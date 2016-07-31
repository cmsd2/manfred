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
using HipChat.Net;
using HipChat.Net.Http;
using System.Net;

namespace Manfred.Daos
{
    public class TokensRepository : ITokensRepository
    {
        public static readonly List<string> Scopes = new List<string> {
            "send_message",
            "send_notification",
            "view_group",
            "view_messages",
            "view_room"
        };

        private ILogger logger;
        private IInstallationsRepository Installations {get; set;}
        private IOAuthRepository OAuth {get; set;}

        private HttpClient HttpClient {get; set;}
        private Settings Settings {get; set;}

        public TokensRepository(ILoggerFactory loggerFactory, IOptions<Settings> settings, IInstallationsRepository installationsRepo, IOAuthRepository oauthRepo)
        {
            logger = loggerFactory.CreateLogger<TokensRepository>();
            Settings = settings.Value;
            OAuth = oauthRepo;
            Installations = installationsRepo;
            HttpClient = new HttpClient();
        }

        public async Task<JObject> GetHipchatCapabilities(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await this.HttpClient.SendAsync(request);
            logger.LogInformation($"token response code={response.StatusCode}");
            var json = await response.Content.ReadAsStringAsync();
            logger.LogInformation($"token response content={json}");
            await response.EnsureSuccessStatusCodeAsync();
            return JObject.Parse(json);
        }

        public async Task<TokenResponse> GetTokenAsync(Oauth oauth)
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

        public async Task<IToken> Renew(string oauthId)
        {
            logger.LogInformation($"renew OAuthId={oauthId}");

            var oauth = await OAuth.GetOauthAsync(oauthId);

            if(oauth == null)
            {
                throw new NotFoundException();
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

            return installation;
        }

        public async Task Clear(string oauthId)
        {
            logger.LogInformation($"clear OAuthId={oauthId}");

            var oauth = await OAuth.GetOauthAsync(oauthId);

            if(oauth == null)
            {
                throw new NotFoundException();
            }

            oauth.AccessToken = null;
            oauth.ExpiresAt = null;
            oauth.Scopes = null;

            await OAuth.CreateOauthAsync(oauth);

            var installation = await Installations.GetInstallationAsync(oauth.GroupId, oauth.RoomId);

            installation.AccessToken = null;
            installation.ExpiresAt = null;
            installation.Scopes = null;
        }

        public async Task<HipChatClient> GetHipChatClient(string groupId, string roomId = null)
        {
            var installation = await Installations.GetInstallationAsync(groupId, roomId);

            return await GetHipChatClient(installation);
        }

        public async Task<HipChatClient> GetHipChatClient(IToken token)
        {
            if(token.AccessToken == null)
            {
                token = await Renew(token.OauthId);
            }

            if(token.AccessToken == null)
            {
                throw new Exception("failed to renew token");
            }

            return new HipChatClient(new ApiConnection(new Credentials(token.AccessToken)));
        }

        public async Task<TResult> Exec<TResult>(IToken token, Func<HipChatClient,Task<TResult>> action, int attempts = 2)
        {
            int attempt = 1;
            Exception lastError = null;

            if(attempts < 1)
            {
                throw new ArgumentException("attempts must be greater than or equal to 1");
            }

            while(attempt <= attempts)
            {
                HipChatClient hipChatClient = await GetHipChatClient(token);

                try
                {
                    return await action(hipChatClient);
                }
                catch (HttpRequestException e)
                {
                    logger.LogInformation($"HttpRequestException attempt={attempt} error={e.Message}");

                    if(e.Message == "Response status code does not indicate success: 401 (Unauthorized).")
                    {
                        token = await Renew(token.OauthId);
                    }
                    else
                    {
                        lastError = e;
                    }
                }
                catch (SimpleHttpResponseException e)
                {
                    logger.LogInformation($"SimpleHttpResponseException attempt={attempt} error={e.Message}");

                    if(e.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        token = await Renew(token.OauthId);
                    }
                    else
                    {
                        lastError = e;
                    }
                }

                attempt++;
            }

            throw lastError;
        }
    }
}