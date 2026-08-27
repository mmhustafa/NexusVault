using NexusVault.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.Interfaces
{
    public interface IAuthService
    {

        Task<LoginResult> RegisterAsync(string email, string password, CancellationToken ct = default);

        Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default);

        Task<TokenPair> CreateWorkspaceAsync(Guid userId, string workspaceName, CancellationToken ct = default);

        Task<TokenPair> AcceptInvitationAsync(Guid userId, string invitationToken, CancellationToken ct = default);

        Task<TokenPair> SelectTenantAsync(Guid userId, Guid tenantId, CancellationToken ct = default);

        Task<TokenPair> RefreshAsync(string refreshToken, CancellationToken ct = default);

        Task<InvitationResult> CreateInvitationAsync(
            Guid tenantId, Guid invitedByUserId, string email, string role,
            CancellationToken ct = default);
    }
}
