using System;
using System.Collections.Generic;
using System.Text;

namespace Livethoughts.Azure.Toolkit.Monitoring.ResourceChangedMonitor
{
    public class LogEntry
    {
        public string Category { get; set; }
        public string ResourceID { get; set; }
        public string DateValue { get; set; }
        public string BeforeSnapshotId { get; set; }
        public string AfterSnapshotId { get; set; }
        public string NumberOfWorkersBeforeValue { get; set; }
        public string NumberOfWorkersAfterValue { get; set; }
        public string SKUCapacityBeforeValue { get; set; }
        public string SKUCapacityAfterValue { get; set; }
        public string CurrentNumberOfWorkersBeforeValue { get; set; }
        public string CurrentNumberOfWorkersAfterValue { get; set; }
        public string SKUNameBeforeValue { get; set; }
        public string SKUNameAfterValue { get; set; }
        public string SKUSizeBeforeValue { get; set; }
        public string SKUSizeAfterValue { get; set; }
        public string CurrentWorkerSizeIdBeforeValue { get; set; }
        public string CurrentWorkerSizeIdAfterValue { get; set; }
        public string CurrentWorkerSizeBeforeValue { get; set; }
        public string CurrentWorkerSizeAfterValue { get; set; }
        public string WorkerSizeIdBeforeValue { get; set; }
        public string WorkerSizeIdAfterValue { get; set; }
        public string WorkerSizeBeforeValue { get; set; }
        public string WorkerSizeAfterValue { get; set; }
    }
}
