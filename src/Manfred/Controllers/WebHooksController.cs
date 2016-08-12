using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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
using System.IO;

namespace Manfred.Controllers
{
    [Route("api/[controller]")]
    public class WebHooksController : Controller
    {
        private ILogger logger;

        private IWebHookRepository WebHooks {get; set;}

        private IInstallationsRepository Installations {get; set;}

        private IEventLogsRepository EventLogs {get; set;}

        private ITokensRepository Tokens {get; set;}
        
        private IEventHub EventHub {get; set;}

        private Settings Settings {get; set;}

        private IJwtValidator JwtValidator {get; set;}
        
        public WebHooksController(ILoggerFactory loggerFactory, IWebHookRepository webHooks, IInstallationsRepository installationsRepo, IEventLogsRepository eventLogsRepo, ITokensRepository tokensRepo, IOptions<Settings> settings, IEventHub eventHub, IJwtValidator jwtValidator)
        {
            logger = loggerFactory.CreateLogger<WebHooksController>();
            WebHooks = webHooks;
            Installations = installationsRepo;
            EventLogs = eventLogsRepo;
            Tokens = tokensRepo;
            Settings = settings.Value;
            EventHub = eventHub;
            JwtValidator = jwtValidator;
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

        [HttpPost("{groupId}/room/{roomId}/webhook/{webhookKey}")]
        public async Task<IActionResult> RoomWebHookEvent([FromHeader] string authorization, string groupId, string roomId, string webhookKey)
        {
            logger.LogInformation($"groupId={groupId} room={roomId} webhookKey={webhookKey} auth={authorization}");
            
            await JwtValidator.Validate(authorization);     
            
            if (Request.Body.CanSeek)
            {
                Request.Body.Position = 0;
            }

            var json = new StreamReader(Request.Body).ReadToEnd();

            logger.LogInformation($"groupId={groupId} room={roomId} webhookKey={webhookKey} payload={json}");

            var e = new EventLog {
                GroupId = groupId,
                RoomId = roomId,
                Content = json
            };
            
            await EventLogs.AddEventLog(e);
            
            EventHub.PublishEvent(e).Forget();
            
            var payload = JsonConvert.DeserializeObject<WebhookPayload>(json, new WebhookPayloadConverter());

            return Ok();
        }
           
        [HttpPut]
        public async Task<IActionResult> Create([FromBody] WebHook m)
        {
            Validate.NotEmpty(m.GroupId, "Hipchat GroupId");
            logger.LogInformation($"creating webhook for installation GroupId={m.GroupId} RoomId={m.RoomId}");

            var installation = await Installations.GetInstallationAsync(m.GroupId, m.RoomId);

            if(installation == null)
            {
                return NotFound();
            }

            logger.LogInformation($"found token OauthId={installation.OauthId} ExpiresAt={installation.ExpiresAt}");

            if(m.WebHookKey == null)
            {
                m.WebHookKey = Guid.NewGuid().ToString();
            }

            await WebHooks.AddWebHookAsync(m);

            var created = await Tokens.ExecHipChat(installation, async hipChatClient => {
                return await hipChatClient.Rooms.CreateRoomWebhookAsync(m.RoomId, new CreateWebhook {
                    Authentication = WebhookAuthentication.Jwt,
                    Key = m.WebHookKey,
                    Url = BuildWebHookLink(m),
                    Event = WebhookEvent.RoomMessage
                });
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
            var installation = await Installations.GetInstallationAsync(groupId, roomId);

            await Tokens.ExecHipChat(installation, async hipChatClient => {
                return await hipChatClient.Rooms.DeleteRoomWebhookAsync(roomId, webhookKey);
            });
            
            await WebHooks.RemoveWebHookAsync(groupId, roomId, webhookKey);

            return Ok();
        }
        
        [HttpGet("{groupId}/room/{roomId}/webhook/{webhookKey}")]
        public async Task<IActionResult> GetRoomWebHook(string groupId, string roomId, string webhookKey)
        {
            var installation = await Installations.GetInstallationAsync(groupId, roomId);

            var hooks = await WebHooks.GetWebHooksAsync(groupId, roomId, webhookKey);

            foreach(WebHook hook in hooks)
            {
                var resp = await Tokens.ExecHipChat(installation, async hipChatClient => {
                    return await hipChatClient.Rooms.GetRoomWebhookAsync(hook.RoomId, hook.WebHookKey);
                });

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