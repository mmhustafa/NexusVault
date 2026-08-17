using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NexusVault.Application.Services
{
    public class ContentHasher
    {
        public static async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct = default)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream, ct);
            stream.Position = 0; // caller will likely need to read the stream again (storage, extraction)
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
