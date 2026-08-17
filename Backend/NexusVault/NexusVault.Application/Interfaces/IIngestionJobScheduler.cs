using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.Interfaces
{

    public interface IIngestionJobScheduler
    {
        void EnqueueProcessDocumentVersion(Guid documentVersionId, string correlationId);
    }
}

