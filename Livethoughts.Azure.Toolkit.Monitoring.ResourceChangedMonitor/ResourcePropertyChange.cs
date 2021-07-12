namespace Livethoughts.Azure.Toolkit.Monitoring.ResourceChangedMonitor
{
    public class ResourcePropertyChange
    {
        public string propertyName { get; set; }
        public string changeCategory { get; set; }
        public string changeType { get; set; }
        public string afterValue { get; set; }
        public string beforeValue { get; set; }
    }
}