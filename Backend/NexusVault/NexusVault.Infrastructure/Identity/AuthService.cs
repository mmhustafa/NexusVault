using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NexusVault.Application.DTOs;
using NexusVault.Application.Interfaces;
using NexusVault.Application.Services;
using NexusVault.Domain.Entities;
using NexusVault.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.Identity
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NexusVaultDbContext _db;
        private readonly ITokenService _tokenService;
        private readonly int _refreshTokenDays;
        private readonly int _accessTokenMinutes;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            NexusVaultDbContext db,
            ITokenService tokenService,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _db = db;
            _tokenService = tokenService;
            _refreshTokenDays = configuration.GetValue("Jwt:RefreshTokenDays", 30);
            _accessTokenMinutes = configuration.GetValue("Jwt:AccessTokenMinutes", 120);
        }

        public async Task<LoginResult> RegisterAsync(string email, string password, CancellationToken ct = default)
        {
            var appUser = new ApplicationUser { UserName = email, Email = email };
            var createResult = await _userManager.CreateAsync(appUser, password);
            if (!createResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));

            var selectionToken = _tokenService.GenerateTenantSelectionToken(appUser.Id, email);

            return new LoginResult(RequiresTenantSelection: true, null, selectionToken, new List<TenantSummary>());
        }

        public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default)
        {
            var appUser = await _userManager.FindByEmailAsync(email);

            if (appUser is null || !await _userManager.CheckPasswordAsync(appUser, password))
                throw new InvalidOperationException("Invalid email or password.");

            var memberships = await _db.TenantUsers
                .Include(tu => tu.Tenant)
                .Where(tu => tu.UserId == appUser.Id)
                .ToListAsync(ct);

            if (memberships.Count == 0)
            {
                var noTenantToken = _tokenService.GenerateTenantSelectionToken(appUser.Id, email);
                return new LoginResult(RequiresTenantSelection: true, null, noTenantToken, new List<TenantSummary>());
            }

            if (memberships.Count == 1)
            {
                var m = memberships[0];
                var tokens = await IssueTokenPairAsync(appUser.Id, m.TenantId, email, m.Role, ct);
                return new LoginResult(RequiresTenantSelection: false, tokens, null, null);
            }

            var selectionToken = _tokenService.GenerateTenantSelectionToken(appUser.Id, email);
            var summaries = memberships.Select(m => new TenantSummary(m.TenantId, m.Tenant!.Name, m.Role)).ToList();
            return new LoginResult(RequiresTenantSelection: true, null, selectionToken, summaries);
        }

        public async Task<TokenPair> SelectTenantAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
        {

            var membership = await _db.TenantUsers.FirstOrDefaultAsync(
                tu => tu.UserId == userId && tu.TenantId == tenantId, ct);

            if (membership is null)
                throw new InvalidOperationException("You are not a member of that workspace.");

            var appUser = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new InvalidOperationException("User not found.");

            return await IssueTokenPairAsync(userId, tenantId, appUser.Email!, membership.Role, ct);
        }

        public async Task<TokenPair> CreateWorkspaceAsync(Guid userId, string workspaceName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workspaceName))
                throw new InvalidOperationException("workspaceName is required.");

            var appUser = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new InvalidOperationException("User not found.");

            var tenant = new Tenant { Id = Guid.NewGuid(), Name = workspaceName, CreatedAt = DateTimeOffset.UtcNow };
            _db.Tenants.Add(tenant);

            _db.TenantUsers.Add(new TenantUser
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TenantId = tenant.Id,
                Email = appUser.Email!,
                Role = TenantRoles.Admin, 
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(ct);

            return await IssueTokenPairAsync(userId, tenant.Id, appUser.Email!, TenantRoles.Admin, ct);
        }

        public async Task<InvitationResult> CreateInvitationAsync(
            Guid tenantId, Guid invitedByUserId, string email, string role,
            CancellationToken ct = default)
        {
            if (role != TenantRoles.Admin && role != TenantRoles.User)
                throw new InvalidOperationException($"Unknown role '{role}'.");

            var rawToken = _tokenService.GenerateOpaqueToken();

            var invitation = new Invitation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Email = email,
                TokenHash = ContentHasher.ComputeSha256(rawToken),
                InvitedByUserId = invitedByUserId,
                Role = role,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
            };

            _db.Invitations.Add(invitation);
            await _db.SaveChangesAsync(ct);

            return new InvitationResult(invitation.Id, rawToken, invitation.ExpiresAt);
        }

        public async Task<TokenPair> AcceptInvitationAsync(Guid userId, string invitationToken, CancellationToken ct = default)
        {
            var tokenHash = ContentHasher.ComputeSha256(invitationToken);
            var invitation = await _db.Invitations.FirstOrDefaultAsync(
                i => i.TokenHash == tokenHash && i.AcceptedAt == null, ct);

            if (invitation is null || invitation.ExpiresAt < DateTimeOffset.UtcNow)
                throw new InvalidOperationException("Invitation is invalid or has expired.");

            var appUser = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new InvalidOperationException("User not found.");

            if (!string.Equals(appUser.Email, invitation.Email, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This invitation was sent to a different email address.");

            var alreadyMember = await _db.TenantUsers.AnyAsync(
                tu => tu.UserId == userId && tu.TenantId == invitation.TenantId, ct);
            if (alreadyMember)
                throw new InvalidOperationException("You are already a member of that workspace.");

            invitation.AcceptedAt = DateTimeOffset.UtcNow;

            _db.TenantUsers.Add(new TenantUser
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TenantId = invitation.TenantId,
                Email = appUser.Email!,
                Role = invitation.Role,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(ct);

            return await IssueTokenPairAsync(userId, invitation.TenantId, appUser.Email!, invitation.Role, ct);
        }

        public async Task<TokenPair> RefreshAsync(string refreshToken, CancellationToken ct = default)
        {
            var tokenHash = ContentHasher.ComputeSha256(refreshToken);
            var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

            if (existing is null || !existing.IsActive)
                throw new InvalidOperationException("Invalid or expired refresh token.");

            var appUser = await _userManager.FindByIdAsync(existing.UserId.ToString())
                ?? throw new InvalidOperationException("User not found.");

            var membership = await _db.TenantUsers.FirstOrDefaultAsync(
                tu => tu.UserId == existing.UserId && tu.TenantId == existing.TenantId, ct)
                ?? throw new InvalidOperationException("Membership no longer exists.");

            var newPair = await IssueTokenPairAsync(existing.UserId, existing.TenantId, appUser.Email!, membership.Role, ct);

            var newTokenHash = ContentHasher.ComputeSha256(newPair.RefreshToken);
            var newEntity = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == newTokenHash, ct);
            existing.RevokedAt = DateTimeOffset.UtcNow;
            existing.ReplacedByTokenId = newEntity?.Id;
            await _db.SaveChangesAsync(ct);

            return newPair;
        }


        private async Task<TokenPair> IssueTokenPairAsync(Guid userId, Guid tenantId, string email, string role, CancellationToken ct)
        {
            var accessToken = _tokenService.GenerateAccessToken(userId, tenantId, email, new[] { role });
            var rawRefreshToken = _tokenService.GenerateOpaqueToken();

            _db.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TenantId = tenantId,
                TokenHash = ContentHasher.ComputeSha256(rawRefreshToken),
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(_refreshTokenDays)
            });
            await _db.SaveChangesAsync(ct);

            return new TokenPair(accessToken, rawRefreshToken, DateTimeOffset.UtcNow.AddMinutes(_accessTokenMinutes));
        }
    }
}
