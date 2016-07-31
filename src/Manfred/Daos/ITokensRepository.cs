using System.Threading.Tasks;
using HipChat.Net;
using IdentityModel.Client;
using Manfred.Models;
using Newtonsoft.Json.Linq;

namespace Manfred.Daos
{
    public interface ITokensRepository
    {
        Task<JObject> GetHipchatCapabilities(string url);

        Task<TokenResponse> GetTokenAsync(Oauth oauth);

        Task<IToken> Renew(string oauthId);

        Task Clear(string oauthId);

        Task<HipChatClient> GetHipChatClient(string groupId, string roomId = null);
    }
}