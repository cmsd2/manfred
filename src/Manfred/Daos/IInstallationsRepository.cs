using Manfred.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Manfred.Daos
{
    public interface IInstallationsRepository
    {
        Task<Installation> GetInstallationAsync(string groupId, string roomId = null);
        
        Task CreateInstallationAsync(Installation installation);

        Task RemoveInstallationAsync(string groupId, string roomId = null);
    }
}