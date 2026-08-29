using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.DTOs
{
    public record UploadDocumentResult(
        Guid DocumentId,
        Guid DocumentVersionId,
        string Status,
        bool WasDuplicate
    );

    public record DocumentVersionStatusResult(
        Guid DocumentVersionId,
        string Status,
        int AttemptCount,
        string? ErrorMessage,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ReadyAt
    );

    public record DocumentVersionSummaryDto(
        Guid VersionId,
        int VersionNumber,
        bool IsCurrent,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ReadyAt
);
}
