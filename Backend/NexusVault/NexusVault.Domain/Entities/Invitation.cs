using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Domain.Entities
{
    public class Invitation
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string TokenHash { get; set; } = string.Empty;
        public Guid InvitedByUserId { get; set; }
        public string Role { get; set; } = TenantRoles.User;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? AcceptedAt { get; set; }

        public Tenant? Tenant { get; set; }
    }
}
