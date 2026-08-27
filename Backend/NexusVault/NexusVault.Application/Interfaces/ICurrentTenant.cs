using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.Interfaces
{
    public interface ICurrentTenant
    {
        bool IsAuthenticated { get; }
        Guid UserId { get; }
        Guid TenantId { get; }
    }
}
