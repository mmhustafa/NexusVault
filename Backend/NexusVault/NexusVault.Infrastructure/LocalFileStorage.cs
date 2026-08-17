using NexusVault.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure
{
    /// <summary>
    /// Local-disk implementation of IFileStorage. Layout:
    ///   {RootPath}/{documentVersionId}/{originalFileName}
    /// Keying by version id (not document id) means every version's file is
    /// physically distinct on disk with no naming collisions, which matters once
    /// Phase 9 has multiple versions per document coexisting.
    ///
    /// This is explicitly a placeholder for local dev / demo purposes -- swap
    /// for an S3/Azure Blob implementation of IFileStorage when deploying for
    /// real, without touching any calling code.
    /// </summary>
    public class LocalFileStorage :IFileStorage
    {
        private readonly string _rootPath;

        public LocalFileStorage(string rootPath)
        {
            _rootPath = rootPath;
            Directory.CreateDirectory(_rootPath);
        }

        public async Task<string> SaveAsync(Guid documentVersionId, string originalFileName, Stream content, CancellationToken ct = default)
        {
            var versionDir = Path.Combine(_rootPath, documentVersionId.ToString());
            Directory.CreateDirectory(versionDir);

            var safeFileName = Path.GetFileName(originalFileName); // strip any path components -- defense against path traversal
            var fullPath = Path.Combine(versionDir, safeFileName);

            content.Position = 0;
            await using var fileStream = File.Create(fullPath);
            await content.CopyToAsync(fileStream, ct);

            return fullPath;
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default)
        {
            Stream stream = File.OpenRead(storagePath);
            return Task.FromResult(stream);
        }
    }
}
