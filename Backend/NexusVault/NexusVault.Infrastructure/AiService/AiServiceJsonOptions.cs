using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NexusVault.Infrastructure.AiService
{
    public static class AiServiceJsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }
}
