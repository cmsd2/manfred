using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Manfred.Models;
using Manfred.Daos;
using HipChat.Net.Clients;
using HipChat.Net.Helpers;
using HipChat.Net.Models.Request;
using HipChat.Net.Models.Response;

namespace Manfred.Controllers
{
    [Route("api/[controller]")]
    public class WebHooksController : Controller
    {
        private ILogger logger;

        private IWebHookRepository WebHooks {get; set;}

        private IRoomsClient Client {get; set;}

        private Settings Settings {get; set;}
        
        public WebHooksController(ILoggerFactory loggerFactory, IWebHookRepository webHooks, IRoomsClient client, IOptions<Settings> settings)
        {
            logger = loggerFactory.CreateLogger<WebHooksController>();
            WebHooks = webHooks;
            Client = client;
            Settings = settings.Value;
        }

        public string BuildWebHookLink(WebHook webhook)
        {
            return $"{Settings.Url}/api/webhooks/{webhook.RoomId}/webhook/{WebUtility.UrlEncode(webhook.WebHookKey)}";
        }
        
        [HttpGet]
        public async Task<List<WebHook>> Index()
        {
            return await WebHooks.GetWebHooksAsync();
        }

        [HttpPost("{roomId}/webhook/{webhookKey}")]
        public Task<IActionResult> Event(string roomId, string webhookKey, [FromBody] string webhookPayload)
        {
            var payload = JsonConvert.DeserializeObject<WebhookPayload>(webhookPayload, new WebhookPayloadConverter());
            logger.LogInformation($"room={roomId} webhookKey={webhookKey} payload={payload}");
            return Task.FromResult<IActionResult>(Ok());
        }
           
        [HttpPut]
        public async Task<IActionResult> Create([FromBody] WebHook m)
        {
            if(m.WebHookKey == null)
            {
                m.WebHookKey = Guid.NewGuid().ToString();
            }

            await WebHooks.AddWebHookAsync(m);

            var created = await Client.CreateRoomWebhookAsync(m.RoomId, new CreateWebhook {
                Authentication = WebhookAuthentication.Jwt,
                Key = m.WebHookKey,
                Url = BuildWebHookLink(m),
                Event = WebhookEvent.RoomMessage
            });

            m.HipChatId = created.Model.Id;

            if(created.Model.Links != null)
            {
                m.HipChatLink = created.Model.Links.Self;
            }

            await WebHooks.AddWebHookAsync(m);
            
            return Ok(m);
        }
        
        [HttpDelete("{roomId}/webhook/{webhookKey}")]
        public async Task<IActionResult> Delete(string roomId, string webhookKey)
        {
            await Client.DeleteRoomWebhookAsync(roomId, webhookKey);
            
            await WebHooks.RemoveWebHookAsync(roomId, webhookKey);

            return Ok();
        }
        
        [HttpGet("{roomId}/webhook/{webhookKey}")]
        public async Task<IActionResult> Get(string roomId, string webhookKey)
        {
            var hooks = await WebHooks.GetWebHooksAsync(roomId, webhookKey);

            foreach(WebHook hook in hooks)
            {
                var resp = await Client.GetRoomWebhookAsync(hook.RoomId, hook.WebHookKey);

                logger.LogInformation($"roomId={roomId} webhookKey={webhookKey} state={hook} webhook={resp.Model}");
            }

            if(hooks.Count == 0)
            {
                return NotFound();
            }
            else if(hooks.Count == 1)
            {
                return Ok(hooks[0]);
            }
            else
            {
                return Ok(hooks);
            }
        }
    }
}