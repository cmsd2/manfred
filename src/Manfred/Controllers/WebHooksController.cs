using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc;
using Manfred.Models;
using Manfred.Daos;
using HipChat.Net.Clients;
using HipChat.Net.Helpers;

namespace Manfred.Controllers
{
    [Route("api/[controller]")]
    public class WebHooksController : Controller
    {
        private IWebHookRepository WebHooks {get; set;}

        private IRoomsClient Client {get; set;}
        
        public WebHooksController(IWebHookRepository webHooks, IRoomsClient client)
        {
            WebHooks = webHooks;
            Client = client;
        }
        
        [HttpGet]
        public async Task<List<WebHook>> Index()
        {
            return await WebHooks.GetWebHooksAsync();
        }
           
        [HttpPut("{roomId}/webhook/{webhookKey}")]
        public async Task<IActionResult> Create([FromBody] WebHook m)
        {
            await WebHooks.AddWebHookAsync(m);
            
            return Ok(m);
        }
        
        [HttpDelete("{roomId}/webhook/{webhookKey}")]
        public async Task<IActionResult> Delete(string roomId, string webhookKey)
        {
            await WebHooks.RemoveWebHookAsync(roomId, webhookKey);

            return Ok();
        }
        
        [HttpGet("{roomId}/webhook/{webhookKey}")]
        public async Task<IActionResult> Get(string roomId, string webhookKey)
        {
            var hooks = await WebHooks.GetWebHooksAsync(roomId, webhookKey);

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