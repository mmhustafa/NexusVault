using Microsoft.AspNetCore.Http;
using NexusVault.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace NexusVault.Infrastructure.Identity
{
    public class CurrentTenant : ICurrentTenant
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentTenant(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

        public Guid UserId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                return Guid.TryParse(value, out var id)
                    ? id
                    : throw new InvalidOperationException("No authenticated user id available on the current request.");
            }
        }

        public Guid TenantId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.User.FindFirst("tenant_id")?.Value;
                return Guid.TryParse(value, out var id)
                    ? id
                    : throw new InvalidOperationException(
                        "No tenant context available on the current request -- this endpoint requires a fully-scoped access token, not a tenant-selection token.");
            }
        }
    }
}
