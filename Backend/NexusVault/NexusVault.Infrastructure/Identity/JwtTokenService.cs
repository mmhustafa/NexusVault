using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NexusVault.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace NexusVault.Infrastructure.Identity
{
    public class JwtTokenService : ITokenService
    {
        private readonly string _issuer;
        private readonly string _audience;
        private readonly SigningCredentials _signingCredentials;
        private readonly int _accessTokenMinutes;

        public JwtTokenService(IConfiguration configuration)
        {
            var secret = configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
            _issuer = configuration["Jwt:Issuer"] ?? "NexusVault";
            _audience = configuration["Jwt:Audience"] ?? "NexusVault";
            _accessTokenMinutes = configuration.GetValue("Jwt:AccessTokenMinutes", 120);

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
            _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }

        public string GenerateAccessToken(Guid userId, Guid tenantId, string email, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new("tenant_id", tenantId.ToString()), // the claim ICurrentTenant reads in the next stage
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
                signingCredentials: _signingCredentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateOpaqueToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-').Replace('/', '_').TrimEnd('='); // base64url
        }

        public string GenerateTenantSelectionToken(Guid userId, string email)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new("token_purpose", "tenant_selection"),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(10),    
                signingCredentials: _signingCredentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
