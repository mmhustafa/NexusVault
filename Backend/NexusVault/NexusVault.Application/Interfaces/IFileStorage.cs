using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.Interfaces
{
    public interface IFileStorage
    {
        /// <summary>Persists the stream and returns a storage path/key that can be
        /// handed to OpenReadAsync later.</summary>
        Task<string> SaveAsync(Guid documentVersionId, string originalFileName, Stream content, CancellationToken ct = default);

        Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default);
    }
}
