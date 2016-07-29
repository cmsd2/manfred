using Manfred.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Manfred.Daos
{
    public interface IMembershipRepository
    {
        Task<List<string>> GetMembershipsAsync();
        
        Task AddMembershipAsync(string roomId);
        
        Task RemoveMembershipAsync(string roomId);

        Task<bool> IsMemberAsync(string roomId);
    }
}