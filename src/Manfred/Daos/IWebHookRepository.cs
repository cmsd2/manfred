using Manfred.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Manfred.Daos
{
    public interface IWebHookRepository
    {
        Task<List<WebHook>> GetWebHooksAsync(string roomId = null, string webhookKey = null);
        
        Task AddWebHookAsync(WebHook webhook);
        
        Task RemoveWebHookAsync(string roomId, string webhookKey);
    }
}