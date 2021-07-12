using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace Livethoughts.Azure.Toolkit.Monitoring.ResourceChangedMonitor
{
    public class ResourceChangedMonitorFunction
    {
        private readonly HttpClient _client;

        public ResourceChangedMonitorFunction(System.Net.Http.IHttpClientFactory httpClientFactory)
        {
            this._client = httpClientFactory.CreateClient();
        }

        [FunctionName("ResourceChangedMonitor")]
        public async Task<IActionResult> Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"ResourceChangedMonitor Timer trigger function executed at: {DateTime.UtcNow}");

            var creds = new StorageCredentials(Startup.GetEnvironmentVariable("TableStorageAccountName"), Startup.GetEnvironmentVariable("TableStorageAccountKey"));
            var account = new CloudStorageAccount(creds, useHttps: true);

            // Retrieve the role assignments table
            var client = account.CreateCloudTableClient();
            var table = client.GetTableReference(Startup.GetEnvironmentVariable("TableStorageAccountTableName"));
            var resources = table.ExecuteQuery(new TableQuery<ResourceConfiguration>()).ToList();

            //Retrieve ResourceGraph Authentication Credencials
            AuthenticationContext authContext = new AuthenticationContext("https://login.microsoftonline.com/" + Startup.GetEnvironmentVariable("TenantId"));
            AuthenticationResult authResult = await authContext.AcquireTokenAsync("https://management.core.windows.net", new ClientCredential(Startup.GetEnvironmentVariable("ClientId"), Startup.GetEnvironmentVariable("ClientSecret")));
            string accessToken = authResult.AccessToken;

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            //ServiceClientCredentials serviceClientCreds = new TokenCredentials(authResult.AccessToken);

            //ResourceGraphClient argClient = new ResourceGraphClient(serviceClientCreds);


            foreach (var resource in resources)
            {
                DateTime startTime = DateTime.UtcNow.AddDays(-10);
                if (resource.LastSucessfullRunDateTimeUTC.HasValue)
                    startTime = resource.LastSucessfullRunDateTimeUTC.Value;

                DateTime endTime = DateTime.UtcNow;

                //QueryRequest request = new QueryRequest();
                //request.Subscriptions = new List<string>() { resource.s };
                //request.Query = strQuery;


                var content = new StringContent(JsonConvert.SerializeObject(
                        string.Format(Startup.GetEnvironmentVariable("ResourceChangesAPIPostBodyFormat"), resource.ResourceID, startTime, endTime,
                            Startup.GetEnvironmentVariable("ResourceChangesAPIIncludePropertyChanges")))
                    , Encoding.UTF8, "application/json");


                var response = await _client.PostAsync(Startup.GetEnvironmentVariable("ResourceChangesAPIUrl"), content);

                if(response.IsSuccessStatusCode)
                {
                    GetResourceChangesResponse getResourceChangesResponse = JsonConvert.DeserializeObject<GetResourceChangesResponse>(await response.Content.ReadAsStringAsync());
                    foreach (var change in getResourceChangesResponse.Changes)
                    {
                        switch(change.changeType)
                        {
                            //TODO Send Change events to log analytics
                            default:
                                break;
                        }
                    }

                }
                else
                {
                    log.LogWarning($"Failed to get information for resource: {resource.ResourceID}");
                    log.LogWarning($"\tResponse status code: {response.StatusCode}");
                    log.LogWarning($"\tResponse Reson Phrase: {response.ReasonPhrase}");
                }
                

            }

            log.LogInformation($"ResourceChangedMonitor Timer trigger function Ended at: {DateTime.UtcNow}");

            return new OkObjectResult("Job Ended Sucessfully");
        }
    }
}
