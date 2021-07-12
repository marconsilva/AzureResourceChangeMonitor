using System.Collections.Generic;

namespace Livethoughts.Azure.Toolkit.Monitoring.ResourceChangedMonitor
{
    public class ResourceChange
    {
        public string changeId { get; set; }
        public ShapshotDefinition beforeSnapshot { get; set; }
        public ShapshotDefinition afterSnapshot { get; set; }
        public string changeType { get; set; }
        public List<ResourcePropertyChange> propertyChanges { get; set; }
    }
}