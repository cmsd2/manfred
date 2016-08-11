using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Manfred.Controllers
{
    public class HipChatJwtSecurityTokenHandler : JwtSecurityTokenHandler
    {
        protected override void ValidateAudience(IEnumerable<string> audiences, JwtSecurityToken token, TokenValidationParameters validationParams)
        {
            // allow missing aud claim
            if(!audiences.GetEnumerator().MoveNext())
            {
                return;
            }
            
            base.ValidateAudience(audiences, token, validationParams);
        }
    }
}