using System;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Manfred.Daos;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Manfred.Controllers
{
    public class HipChatJwtValidator : IJwtValidator
    {
        private ILogger logger;

        public Settings Settings {get; set;}

        public IInstallationsRepository Installations {get; set;}

        private JwtSecurityTokenHandler jwtHandler;

        public HipChatJwtValidator(ILoggerFactory loggerFactory, IOptions<Settings> settings, IInstallationsRepository installations)
        {
            logger = loggerFactory.CreateLogger<HipChatJwtValidator>();
            Settings = settings.Value;

            Installations = installations;
            jwtHandler = new JwtSecurityTokenHandler();
        }

        public async Task<ClaimsPrincipal> Validate(string authorization)
        {
            string token;

            if (string.IsNullOrEmpty(authorization))
            {
                throw new Exception("empty or missing token");
            }

            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authorization.Substring("Bearer ".Length).Trim();
            }
            else if (authorization.StartsWith("JWT ", StringComparison.OrdinalIgnoreCase))
            {
                token = authorization.Substring("JWT ".Length).Trim();

            }
            else
            {
                token = authorization;
            }

            if(jwtHandler.CanReadToken(token))
            {
                throw new Exception($"can't read token {token}");
            }

            JwtSecurityToken jwt = jwtHandler.ReadJwtToken(token);

            var oauthId = jwt.Issuer;
            var installation = await Installations.GetInstallationByOauthIdAsync(oauthId);
            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(installation.OauthSecret));
            var validationParams = new TokenValidationParameters 
            {
                ValidIssuer = oauthId,
                IssuerSigningKey = signingKey
            };

            SecurityToken validatedToken;
            return jwtHandler.ValidateToken(token, validationParams, out validatedToken);
        }
    }
}