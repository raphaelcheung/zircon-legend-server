using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Library;
using Server.Envir;

namespace Server.WebApi.Auth
{
    /// <summary>
    /// JWT token helper for authentication
    /// </summary>
    public class JwtHelper
    {
        private readonly SymmetricSecurityKey _key;
        private readonly string _issuer = "ZirconLegendServer";
        private readonly string _audience = "ZirconWebUI";

        public JwtHelper()
        {
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Config.WebApiJwtSecret));
        }

        /// <summary>
        /// Generate a JWT token for the specified account
        /// </summary>
        public string GenerateToken(string email, AccountIdentity identity)
        {
            var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, identity.ToString()),
                new Claim("identity", ((int)identity).ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Config.WebApiJwtExpiration),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Generate a refresh token
        /// </summary>
        public string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Validate a JWT token and return the claims principal
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = _key,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                return principal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get email from claims principal
        /// </summary>
        public static string? GetEmail(ClaimsPrincipal? user)
        {
            return user?.FindFirst(ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// Get identity level from claims principal
        /// </summary>
        public static AccountIdentity GetIdentity(ClaimsPrincipal? user)
        {
            var identityClaim = user?.FindFirst("identity")?.Value;
            if (int.TryParse(identityClaim, out int identity))
            {
                return (AccountIdentity)identity;
            }
            return AccountIdentity.Normal;
        }

        /// <summary>
        /// Check if user has minimum required identity level
        /// </summary>
        public static bool HasMinimumIdentity(ClaimsPrincipal? user, AccountIdentity minimum)
        {
            return GetIdentity(user) >= minimum;
        }
    }
}
