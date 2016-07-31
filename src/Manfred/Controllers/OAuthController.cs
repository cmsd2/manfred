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
        public static readonly List<string> Scopes = new List<string> {
            "send_message",
            "send_notification",
            "view_group",
            "view_messages",
            "view_room"
        };
        public Settings Settings {get; set;}
        public IOAuthRepository OAuth {get; set;}

        public IInstallationsRepository Installations {get; set;}

        public HttpClient HttpClient {get; set;}

        private ILogger logger;

        public OAuthController(ILoggerFactory loggerFactory, IOptions<Settings> settings, IOAuthRepository oauthRepo, IInstallationsRepository installationsRepo)
        {
            logger = loggerFactory.CreateLogger<InstallationsController>();
            Settings = settings.Value;
            OAuth = oauthRepo;
            Installations = installationsRepo;
            HttpClient = new HttpClient();
        }

        async Task<JObject> GetHipchatCapabilities(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await this.HttpClient.SendAsync(request);
            logger.LogInformation($"token response code={response.StatusCode}");
            var json = await response.Content.ReadAsStringAsync();
            logger.LogInformation($"token response content={json}");
            response.EnsureSuccessStatusCode();
            return JObject.Parse(json);
        }

        async Task<TokenResponse> GetTokenAsync(Oauth oauth)
        {
            JObject caps = await GetHipchatCapabilities(oauth.CapabilitiesUrl);

            var tokenClient = new TokenClient(
                (string)caps["capabilities"]["oauth2Provider"]["tokenUrl"],
                oauth.OauthId,
                oauth.OauthSecret);
 
            var token = await tokenClient.RequestClientCredentialsAsync(String.Join(" ", Scopes));

            if(token.IsHttpError)
            {
                logger.LogInformation($"token code={token.HttpErrorStatusCode} httperr={token.HttpErrorReason}");
            }
            else if(token.IsError)
            {
                logger.LogInformation($"token err={token.Error}");
            }
            else
            {
                logger.LogInformation($"token access_token={token.AccessToken} expires_in={token.ExpiresIn}");
            }

            return token;
        }

        [HttpGet("{oauthId}")]
        public async Task<IActionResult> Show(string oauthId)
        {
            logger.LogInformation($"show OAuthId={oauthId}");

            var oauth = await OAuth.GetOauthAsync(oauthId);

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
            logger.LogInformation($"renew OAuthId={oauthId}");

            var oauth = await OAuth.GetOauthAsync(oauthId);

            if(oauth == null)
            {
                return NotFound();
            }

            var token = await GetTokenAsync(oauth);

            if(token.IsError)
            {
                throw new Exception($"failed: code={token.HttpErrorStatusCode} msg={token.HttpErrorReason} err={token.Error}");
            }

            oauth.AccessToken = token.AccessToken;
            oauth.ExpiresAt = DateTimeUTils.ToIsoString(DateTime.UtcNow.AddSeconds(token.ExpiresIn));
            oauth.Scopes = Scopes;

            await OAuth.CreateOauthAsync(oauth);

            var installation = await Installations.GetInstallationAsync(oauth.GroupId, oauth.RoomId);

            installation.AccessToken = oauth.AccessToken;
            installation.ExpiresAt = oauth.ExpiresAt;
            installation.Scopes = oauth.Scopes;

            await Installations.CreateInstallationAsync(installation);

            return Ok();
        }

        [HttpPost("{oauthId}/clear")]
        public async Task<IActionResult> Clear(string oauthId)
        {
            logger.LogInformation($"clear OAuthId={oauthId}");

            var oauth = await OAuth.GetOauthAsync(oauthId);

            if(oauth == null)
            {
                return NotFound();
            }

            oauth.AccessToken = null;
            oauth.ExpiresAt = null;
            oauth.Scopes = null;

            await OAuth.CreateOauthAsync(oauth);

            var installation = await Installations.GetInstallationAsync(oauth.GroupId, oauth.RoomId);

            installation.AccessToken = null;
            installation.ExpiresAt = null;
            installation.Scopes = null;

            return Ok();
        }
    }
}