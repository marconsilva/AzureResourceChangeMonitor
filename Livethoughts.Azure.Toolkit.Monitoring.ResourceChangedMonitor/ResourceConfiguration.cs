using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Livethoughts.Azure.Toolkit.Monitoring.ResourceChangedMonitor
{
    public class ResourceConfiguration : TableEntity
    {
        public string SubscriptionID { get; set; }
        public string ResourceID { get; set; }

        public DateTime? LastSucessfullRunDateTimeUTC { get; set; }

        public string LastKnownSaveState { get; set; }

        public ResourceConfiguration()
        {

        }

        public ResourceConfiguration(string subscriptionID, string resourceId, DateTime? lastSucessfullRunDateTimeUTC, string lastKnownSaveState)
        {
            PartitionKey = SubscriptionID = subscriptionID;
            RowKey = ResourceID = resourceId;
            LastSucessfullRunDateTimeUTC = lastSucessfullRunDateTimeUTC;
            LastKnownSaveState = lastKnownSaveState;
        }
    }
}
