using System;
using System.Collections.Generic;
using System.Text;


namespace NexusVault.Domain.Entities
{
    public class Embedding
    {
        public Guid ChunkId { get; set; }       // 1:1 with Chunk, is the PK
        public float[] Vector { get; set; } = Array.Empty<float>();
        public string ModelName { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public int Dimensions { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public Chunk? Chunk { get; set; }
    }
}
