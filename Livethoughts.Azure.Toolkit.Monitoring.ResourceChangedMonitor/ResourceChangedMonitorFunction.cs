using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using LogAnalytics.Client;
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
        public async Task Run([TimerTrigger("0/5 * * * * *")]TimerInfo myTimer)
        {
            //log.LogInformation($"ResourceChangedMonitor Timer trigger function executed at: {DateTime.UtcNow}");

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


            //Connect to log Analytics
            LogAnalyticsClient loganalytics = new LogAnalyticsClient(
                workspaceId: Startup.GetEnvironmentVariable("LogAnalyticsWorkspaceId"),
                sharedKey: Startup.GetEnvironmentVariable("LogAnalyticsWorkspaceKey"));


            foreach (var resource in resources)
            {
                DateTime startTime = DateTime.UtcNow.AddDays(-10);
                if (resource.LastSucessfullRunDateTimeUTC.HasValue)
                    startTime = resource.LastSucessfullRunDateTimeUTC.Value;

                DateTime endTime = DateTime.UtcNow;

                //QueryRequest request = new QueryRequest();
                //request.Subscriptions = new List<string>() { resource.s };
                //request.Query = strQuery;


                string requestBody = Startup.GetEnvironmentVariable("ResourceChangesAPIPostBodyFormat")
                    .Replace("{resourceId}", resource.ResourceID)
                    .Replace("{startTime}", startTime.ToString("u", DateTimeFormatInfo.InvariantInfo))
                    .Replace("{endTime}", endTime.ToString("u", DateTimeFormatInfo.InvariantInfo))
                    .Replace("{includePropertyChanges}", Startup.GetEnvironmentVariable("ResourceChangesAPIIncludePropertyChanges"));

                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");


                var response = await _client.PostAsync(Startup.GetEnvironmentVariable("ResourceChangesAPIUrl"), content);

                if(response.IsSuccessStatusCode)
                {
                    GetResourceChangesResponse getResourceChangesResponse = JsonConvert.DeserializeObject<GetResourceChangesResponse>(await response.Content.ReadAsStringAsync());
                    foreach (var change in getResourceChangesResponse.Changes)
                    {
                        var logentry = new LogEntry();
                        logentry.ResourceID = resource.ResourceID;
                        logentry.DateValue = change.afterSnapshot.timestamp.ToString("u", DateTimeFormatInfo.InvariantInfo);
                        logentry.BeforeSnapshotId = change.beforeSnapshot.snapshotId;
                        logentry.AfterSnapshotId = change.afterSnapshot.snapshotId;
                        bool foundvalidChange = false;

                        foreach (var propertyChanged in change.propertyChanges)
                        {
                            switch (propertyChanged.propertyName)
                            {
                                case "properties.numberOfWorkers":
                                    logentry.NumberOfWorkersBeforeValue = propertyChanged.beforeValue;
                                    logentry.NumberOfWorkersAfterValue = propertyChanged.afterValue;
                                    foundvalidChange = true;
                                    break;
                                case "sku.capacity":
                                    logentry.SKUCapacityBeforeValue = propertyChanged.beforeValue;
                                    logentry.SKUCapacityAfterValue = propertyChanged.afterValue;
                                    foundvalidChange = true;
                                    break;
                                case "properties.currentNumberOfWorkers":
                                    logentry.CurrentNumberOfWorkersBeforeValue = propertyChanged.beforeValue;
                                    logentry.CurrentNumberOfWorkersAfterValue = propertyChanged.afterValue;
                                    foundvalidChange = true;
                                    break;
                                case "sku.name":
                                    logentry.SKUNameBeforeValue = propertyChanged.beforeValue;
                                    logentry.SKUNameAfterValue = propertyChanged.afterValue;
                                    foundvalidChange = true;
                                    break;
                                case "sku.size":
                                    logentry.SKUSizeBeforeValue = propertyChanged.beforeValue;
                                    logentry.SKUSizeAfterValue = propertyChanged.afterValue;
                                    foundvalidChange = true;
                                    break;
                                case "properties.currentWorkerSizeId":
                                    logentry.CurrentWorkerSizeIdBeforeValue = propertyChanged.beforeValue;
                                    logentry.CurrentWorkerSizeIdAfterValue = propertyChanged.afterValue;
                                    foundvalidChange = true;
                                    break;
                                case "properties.currentWorkerSize":
                                    logentry.CurrentWorkerSizeBeforeValue = propertyChanged.beforeValue;
                                    logentry.CurrentWorkerSizeAfterValue = propertyChanged.afterValue;
                                    foundvalidChange = true;
                                    break;
                                case "properties.workerSizeId":
                                    logentry.WorkerSizeIdBeforeValue = propertyChanged.beforeValue;
                                    logentry.WorkerSizeIdAfterValue = propertyChanged.afterValue;
                                    foundvalidChange = true;
                                    break;
                                case "properties.workerSize":
                                    logentry.WorkerSizeBeforeValue = propertyChanged.beforeValue;
                                    logentry.WorkerSizeAfterValue = propertyChanged.afterValue;
                                    foundvalidChange = true;
                                    break;
                                default:
                                    break;
                            }
                        }

                        if(foundvalidChange)
                        {
                            //TODO Post to Log Analytics
                            await loganalytics.SendLogEntries<LogEntry>(new List<LogEntry>() { logentry }, "resourceChanges", resource.ResourceID).ConfigureAwait(false);
                        }
                    }

                    if (getResourceChangesResponse.Changes.Count > 0)
                    {
                        resource.LastKnownSaveState = getResourceChangesResponse.Changes.Last<ResourceChange>().afterSnapshot.snapshotId;
                        resource.LastSucessfullRunDateTimeUTC = endTime;
                        await table.ExecuteAsync(TableOperation.Replace(resource));
                    }
                }
                else
                {
                    //log.LogWarning($"Failed to get information for resource: {resource.ResourceID}");
                    //log.LogWarning($"\tResponse status code: {response.StatusCode}");
                    //log.LogWarning($"\tResponse Reson Phrase: {response.ReasonPhrase}");
                }
                

            }

            //log.LogInformation($"ResourceChangedMonitor Timer trigger function Ended at: {DateTime.UtcNow}");

            return;
        }
    }
}
