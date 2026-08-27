using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.DTOs
{
    public record TenantSummary(Guid TenantId, string TenantName, string Role);
    public record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);

    /// Result of a login attempt. Exactly one of two shapes, depending on how
    /// many tenants the user belongs to:
    ///   - RequiresTenantSelection == false: Tokens is populated, ready to use immediately.
    ///   - RequiresTenantSelection == true: TenantSelectionToken + AvailableTenants
    ///     are populated, Tokens is null -- caller must call SelectTenantAsync next.
    public record LoginResult(
        bool RequiresTenantSelection,
        TokenPair? Tokens,
        string? TenantSelectionToken,
        IReadOnlyList<TenantSummary>? AvailableTenants
    );

    public record InvitationResult(Guid InvitationId, string RawToken, DateTimeOffset ExpiresAt);

    public record RegisterRequest(string Email, string Password);
    public record LoginRequest(string Email, string Password);
    public record SelectTenantRequest(Guid TenantId);
    public record CreateWorkspaceRequest(string WorkspaceName);
    public record AcceptInvitationRequest(string InvitationToken);
    public record RefreshRequest(string RefreshToken);
    public record CreateInvitationRequest(string Email, string? Role);

}
