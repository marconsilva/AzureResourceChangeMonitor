using Livethoughts.Azure.Toolkit.Monitoring.ResourceChangedMonitor;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
[assembly: FunctionsStartup(typeof(Startup))]

namespace Livethoughts.Azure.Toolkit.Monitoring.ResourceChangedMonitor
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();
        }

        public static string GetEnvironmentVariable(string name)
        {
            var environmentVariable = System.Environment.GetEnvironmentVariable(name);

            if(string.IsNullOrEmpty(environmentVariable))
            {
                switch (name.ToLowerInvariant())
                {
                    case "tablestorageaccounttablename":
                        environmentVariable = "ResouceMonitoringResourceList";
                        break;
                    case "resourcechangesapipostbodyformat":
                        environmentVariable = @"{
                                ""resourceId"": ""{resourceId}"",
                                ""interval"": {
                                    ""start"": ""{startTime}"",
                                    ""end"": ""{endTime}""
                                },
                                ""fetchPropertyChanges"": {includePropertyChanges}
                            }";
                        break;
                    case "resourcechangesapiincludepropertychanges":
                        environmentVariable = "true"; 
                        break;
                    case "resourcechangesapiurl":
                        environmentVariable = "https://management.azure.com/providers/Microsoft.ResourceGraph/resourceChanges?api-version=2018-09-01-preview"; 
                        break;
                    default:
                        break;
                }
            }
            return environmentVariable;
        }
    }
}
