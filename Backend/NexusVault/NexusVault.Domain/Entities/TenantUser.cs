using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Domain.Entities
{
    public class TenantUser
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = TenantRoles.User;
        public DateTimeOffset CreatedAt { get; set; }
        public Tenant? Tenant { get; set; }
    }
}
