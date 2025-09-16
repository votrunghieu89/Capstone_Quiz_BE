using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Capstone.Security
{
    public class Token
    {
        public readonly IConfiguration _configuration;
        public Token(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string generateAccessToken(int accountId, string role, string email)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("AccountId", accountId.ToString()),
                new Claim("Role", role),
                new Claim("Email", email)
            };
            var token = new JwtSecurityToken(
                 issuer: _configuration["Jwt:Issuer"],
                 audience: _configuration["Jwt:Audience"],
                 claims: claims,
                 expires: DateTime.Now.AddHours(6),
                 signingCredentials: credentials
             );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string generateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            return Convert.ToBase64String(randomNumber);
        }
    }
}
