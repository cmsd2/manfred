using System.Collections.Generic;

namespace Manfred.Models
{
    public interface IToken
    {
        string AccessToken {get; set;}
        string ExpiresAt {get; set;}
        List<string> Scopes {get; set;}
    }
}