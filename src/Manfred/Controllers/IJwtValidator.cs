using System.Security.Claims;
using System.Threading.Tasks;

namespace Manfred.Controllers
{
    public interface IJwtValidator
    {
        Task<ClaimsPrincipal> Validate(string authorization);
    }
}