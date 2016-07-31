using System;
using System.Runtime;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Manfred.Daos;
using Manfred.Models;
using HipChat.Net.Models.Response;

namespace Manfred.Controllers  {
    [Route("api/[controller]")]
    public class OAuthController : Controller
    {
        public Settings Settings {get; set;}
        public IOAuthRepository OAuth {get; set;}

        private ILogger logger;

        public OAuthController(ILoggerFactory loggerFactory, IOptions<Settings> settings, IOAuthRepository oauthRepo)
        {
            logger = loggerFactory.CreateLogger<InstallationsController>();
            Settings = settings.Value;
            OAuth = oauthRepo;
        }

        [HttpGet("{oauthId}")]
        public async Task<IActionResult> Show(string oauthId)
        {
            logger.LogInformation($"show OAuthId={oauthId}");

            var oauth = await OAuth.GetOauthAsync(oauthId);

            if(oauth != null)
            {
                return Ok(oauth);
            }
            else
            {
                return NotFound();
            }
        }
    }
}