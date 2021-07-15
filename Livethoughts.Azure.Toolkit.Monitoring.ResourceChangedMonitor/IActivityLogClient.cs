using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogAnalytics.Client
{
    /// <summary>
    /// Interface for LogAnalyticsClient.
    /// </summary>
    public interface IActivityLogClient
    {
        public Task<string> GetLogEntries(string authorizationToken, DateTime? start, DateTime? end, string resourceId = null);
    }
}