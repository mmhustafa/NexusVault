using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Domain.Enums
{
    public enum DocumentVersionStatus
    {
        Pending = 0,
        Processing = 1,
        Ready = 2,
        Failed = 3
    }
}
