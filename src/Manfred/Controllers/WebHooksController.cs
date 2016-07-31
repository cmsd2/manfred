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
using HipChat.Net;
using HipChat.Net.Http;

namespace Manfred.Controllers
{
    [Route("api/[controller]")]
    public class WebHooksController : Controller
    {
        private ILogger logger;

        private IWebHookRepository WebHooks {get; set;}

        private IInstallationsRepository Installations {get; set;}

        private ITokensRepository Tokens {get; set;}

        private Settings Settings {get; set;}
        
        public WebHooksController(ILoggerFactory loggerFactory, IWebHookRepository webHooks, IInstallationsRepository installationsRepo, ITokensRepository tokensRepo, IOptions<Settings> settings)
        {
            logger = loggerFactory.CreateLogger<WebHooksController>();
            WebHooks = webHooks;
            Installations = installationsRepo;
            Tokens = tokensRepo;
            Settings = settings.Value;
        }

        public string BuildWebHookLink(WebHook webhook)
        {
            return $"{Settings.Url}/api/webhooks/{webhook.GroupId}/room/{webhook.RoomId}/webhook/{WebUtility.UrlEncode(webhook.WebHookKey)}";
        }
        
        [HttpGet("{groupId}")]
        public async Task<List<WebHook>> GetGroupWebHooks(string groupId)
        {
            return await WebHooks.GetWebHooksAsync(groupId);
        }

        [HttpGet("{groupId}/room/{roomId}")]
        public async Task<List<WebHook>> GetRoomWebHooks(string groupId, string roomId)
        {
            return await WebHooks.GetWebHooksAsync(groupId, roomId);
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
            Validate.NotEmpty(m.GroupId, "Hipchat GroupId");

            var hipChatClient = await Tokens.GetHipChatClient(m.GroupId, m.RoomId);

            if(m.WebHookKey == null)
            {
                m.WebHookKey = Guid.NewGuid().ToString();
            }

            await WebHooks.AddWebHookAsync(m);

            var created = await hipChatClient.Rooms.CreateRoomWebhookAsync(m.RoomId, new CreateWebhook {
                Authentication = WebhookAuthentication.Jwt,
                Key = m.WebHookKey,
                Url = BuildWebHookLink(m),
                Event = WebhookEvent.RoomMessage
            });

            logger.LogInformation($"created webhook Code={created.Code} Response={created.Body}");

            m.HipChatId = created.Model.Id;

            if(created.Model.Links != null)
            {
                m.HipChatLink = created.Model.Links.Self;
            }

            await WebHooks.AddWebHookAsync(m);
            
            return Ok(m);
        }
        
        [HttpDelete("{groupId}/room/{roomId}/webhook/{webhookKey}")]
        public async Task<IActionResult> Delete(string groupId, string roomId, string webhookKey)
        {
            var hipChatClient = await Tokens.GetHipChatClient(groupId, roomId);

            await hipChatClient.Rooms.DeleteRoomWebhookAsync(roomId, webhookKey);
            
            await WebHooks.RemoveWebHookAsync(groupId, roomId, webhookKey);

            return Ok();
        }
        
        [HttpGet("{groupId}/room/{roomId}/webhook/{webhookKey}")]
        public async Task<IActionResult> Get(string groupId, string roomId, string webhookKey)
        {
            var hipChatClient = await Tokens.GetHipChatClient(groupId, roomId);

            var hooks = await WebHooks.GetWebHooksAsync(roomId, webhookKey);

            foreach(WebHook hook in hooks)
            {
                var resp = await hipChatClient.Rooms.GetRoomWebhookAsync(hook.RoomId, hook.WebHookKey);

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