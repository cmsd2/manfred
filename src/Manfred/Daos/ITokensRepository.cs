using System;
using System.Threading.Tasks;
using HipChat.Net;
using HipChat.Net.Http;
using IdentityModel.Client;
using Manfred.Models;
using Newtonsoft.Json.Linq;

namespace Manfred.Daos
{
    public interface ITokensRepository
    {
        Task<JObject> GetHipchatCapabilities(string url);

        Task<TokenResponse> GetTokenAsync(Installation oauth);

        Task<IToken> Renew(string oauthId);

        Task Clear(string oauthId);

        Task<HipChatClient> GetHipChatClient(string groupId, string roomId = null);

        Task<HipChatClient> GetHipChatClient(IToken token);

        Task<TResult> Exec<TResult>(IToken token, Func<HipChatClient,Task<TResult>> action, int attempts = 2);
        Task<IResponse<TModel>> ExecHipChat<TModel>(IToken token, Func<HipChatClient,Task<IResponse<TModel>>> action, int attempts = 2);
    }
}