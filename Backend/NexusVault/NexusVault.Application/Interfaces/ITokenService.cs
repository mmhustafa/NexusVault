using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(Guid userId, Guid tenantId, string email, IEnumerable<string> roles);

        string GenerateTenantSelectionToken(Guid userId, string email);

        //used for both refresh tokens and invitation tokens
        string GenerateOpaqueToken();
    }
}
