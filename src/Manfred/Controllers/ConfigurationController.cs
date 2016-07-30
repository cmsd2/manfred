using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using HipChat.Net.Http;
using HipChat.Net.Clients;
using Manfred.Models;

namespace Manfred.Controllers
{
    [Route("[controller]")]
    public class ConfigurationController : Controller
    {
        private readonly ILogger logger;

        public ConfigurationController(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<ConfigurationController>();
        }
        
        [HttpGet]
        public IActionResult GetConfig([FromQuery] string signed_request)
        {
            //todo verify signed_request jwt
            
            logger.LogInformation("showing config page");

            return Ok();
        }
    }
}