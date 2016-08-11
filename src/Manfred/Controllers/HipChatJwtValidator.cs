using System;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Manfred.Daos;
using Manfred.Models;
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
            jwtHandler = new HipChatJwtSecurityTokenHandler();
        }

        public string SignToken(Installation creds, ClaimsIdentity claims)
        {
            var plainTextSecurityKey = creds.OauthSecret;

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(plainTextSecurityKey));
    
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var securityTokenDescriptor = new SecurityTokenDescriptor()
            {
                Issuer = creds.OauthId,
                Subject = claims,
                SigningCredentials = signingCredentials
            };

            var plainToken = jwtHandler.CreateToken(securityTokenDescriptor);
            var signedAndEncodedToken = jwtHandler.WriteToken(plainToken);

            return signedAndEncodedToken;
        }

        public async Task<ClaimsPrincipal> Validate(string authorization)
        {
            string token;

            if (string.IsNullOrEmpty(authorization))
            {
                throw new ArgumentException(nameof(authorization), "empty or missing token");
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

            if(!jwtHandler.CanReadToken(token))
            {
                throw new ArgumentException(nameof(authorization), $"can't read token {token}");
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