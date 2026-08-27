using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
        public Guid? ReplacedByTokenId { get; set; }

        public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
    }
}
