using Hangfire;
using NexusVault.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.Jobs
{
    public class HangfireIngestionJobScheduler : IIngestionJobScheduler
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public HangfireIngestionJobScheduler(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        public void EnqueueProcessDocumentVersion(Guid documentVersionId, string correlationId)
        {
            _backgroundJobClient.Enqueue<ProcessDocumentVersionJob>(
                job => job.ExecuteAsync(documentVersionId, correlationId, CancellationToken.None));
        }
    }
}
