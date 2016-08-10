using System;
using System.Runtime;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Manfred.Daos;
using Manfred.Models;
using Manfred.ViewModels;
using HipChat.Net.Models.Response;
using IdentityModel;
using IdentityModel.Client;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Manfred;

namespace Manfred.Controllers  {
    [Route("api/[controller]")]
    public class OAuthController : Controller
    {
        public Settings Settings {get; set;}

        public IInstallationsRepository Installations {get; set;}

        public ITokensRepository Tokens {get; set;}

        public HttpClient HttpClient {get; set;}

        private ILogger logger;

        public OAuthController(ILoggerFactory loggerFactory, IOptions<Settings> settings, IInstallationsRepository installationsRepo, ITokensRepository tokensRepo)
        {
            logger = loggerFactory.CreateLogger<InstallationsController>();
            Settings = settings.Value;
            Tokens = tokensRepo;
            Installations = installationsRepo;
            HttpClient = new HttpClient();
        }

        
        [HttpGet("{oauthId}")]
        public async Task<IActionResult> Show(string oauthId)
        {
            logger.LogInformation($"show OAuthId={oauthId}");

            var oauth = await Installations.GetInstallationByOauthIdAsync(oauthId);

            if(oauth != null)
            {
                return Ok(new OAuthView(oauth));
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("{oauthId}/renew")]
        public async Task<IActionResult> Renew(string oauthId)
        {
            try {
                await Tokens.Renew(oauthId);
            } catch (NotFoundException) {
                // todo could we get an AggregateException instead?
                return NotFound();
            }

            return Ok();
        }

        [HttpPost("{oauthId}/clear")]
        public async Task<IActionResult> Clear(string oauthId)
        {
            try {
                await Tokens.Clear(oauthId);
            } catch (NotFoundException) {
                return NotFound();
            }

            return Ok();
        }
    }
}