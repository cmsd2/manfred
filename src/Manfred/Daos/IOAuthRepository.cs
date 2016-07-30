using Manfred.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Manfred.Daos
{
    public interface IOAuthRepository
    {
        Task<Oauth> GetOauthAsync(string oauthId);
        
        Task CreateOauthAsync(Oauth oauth);

        Task RemoveOauthAsync(string oauthId);
    }
}