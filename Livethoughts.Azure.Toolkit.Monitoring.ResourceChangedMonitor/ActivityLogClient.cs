using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogAnalytics.Client.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace LogAnalytics.Client
{
    /// <summary>
    /// Client to send logs to Azure Log Analytics.
    /// </summary>
    public class ActivityLogClient : IActivityLogClient
    {
        private readonly HttpClient httpClient;

        private string SubscriptionId { get; }

        private string RequestBaseUrl { get; }

        private ActivityLogClient(HttpClient client, string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId), "workspaceId cannot be null or empty");
            }

            this.SubscriptionId = subscriptionId;
            this.RequestBaseUrl = $"https://management.azure.com/subscriptions/{this.SubscriptionId}/providers/Microsoft.Insights/eventtypes/management/values?api-version={Consts.ActivityLogApiVersion}";

            this.httpClient = client;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogAnalyticsClient"/> class.
        /// </summary>
        /// <param name="client">The HttpClient.</param>
        /// <param name="options">LogAnalyticsClient options.</param>
        public ActivityLogClient(HttpClient client, IOptions<ActivityLogClientOptions> options)
            : this(client, options.Value.SubscriptionId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogAnalyticsClient"/> class.
        /// </summary>
        /// <param name="subscriptionId">Azure Log Analytics Workspace ID</param>
        /// <param name="sharedKey">Azure Log Analytics Workspace Shared Key</param>
        /// <param name="endPointOverride">The Azure Cloud to use.</param>
        public ActivityLogClient(string subscriptionId)
            : this(new HttpClient(), subscriptionId)
        {
        }

        public async Task<string> GetLogEntries(string authorizationToken, DateTime? start, DateTime? end, string resourceId = null)
        {
            var dateTimeNow = DateTime.UtcNow.ToString("r", System.Globalization.CultureInfo.InvariantCulture);

            bool isFirstFilterParameter = true;
            string filterParameterString = string.Empty;
            if(start != null && start.HasValue)
            {
                filterParameterString = $"&$filter=eventTimestamp ge '{WebUtility.UrlEncode(start.Value.ToString("u", System.Globalization.CultureInfo.InvariantCulture))}'";
                isFirstFilterParameter = false;
            }
            if (end != null && end.HasValue)
            {
                if (isFirstFilterParameter)
                {
                    filterParameterString = $"&$filter=eventTimestamp lt '{WebUtility.UrlEncode(end.Value.ToString("u", System.Globalization.CultureInfo.InvariantCulture))}'";
                }
                else
                {
                    filterParameterString = string.Concat(filterParameterString, $" and eventTimestamp lt '{WebUtility.UrlEncode(end.Value.ToString("u", System.Globalization.CultureInfo.InvariantCulture))}'");
                }
            }
            if (!string.IsNullOrEmpty(resourceId))
            {
                if (isFirstFilterParameter)
                {
                    filterParameterString = $"&$filter=resourceId eq '{WebUtility.UrlEncode(resourceId)}'";
                }
                else
                {
                    filterParameterString = string.Concat(filterParameterString, $" and resourceId eq {WebUtility.UrlEncode(resourceId)}");
                }
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, string.Concat(this.RequestBaseUrl, filterParameterString));
            request.Headers.Clear();
            request.Headers.Add("Authorization", $"Bearer {authorizationToken}");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("x-ms-date", dateTimeNow);
            if (!string.IsNullOrWhiteSpace(resourceId))
            {
                // The Resource ID in Azure for a given resource to connect the logs with.
                request.Headers.Add("x-ms-AzureResourceId", resourceId);
            }

            var response = await this.httpClient.SendAsync(request).ConfigureAwait(false);

            // Bubble up exceptions if there are any, don't swallow them here. This lets consumers handle it better.
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}