using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace StoryManagement.Plugins.Helpers
{
    /// <summary>
    /// Sends many requests to Dataverse in ONE call (ExecuteMultipleRequest).
    /// Used by Task 2 (create), Task 3C (update) and Task 6 (delete).
    /// </summary>
    internal static class ExecuteMultipleHelper
    {
        // Dataverse allows at most 1000 requests in one ExecuteMultipleRequest
        private const int BatchSize = 1000;

        /// <summary>
        /// Runs all requests. If any request fails, throws, so the whole
        /// operation (and its transaction) is rolled back.
        /// </summary>
        public static void Execute(IOrganizationService service, ITracingService tracing,
            IList<OrganizationRequest> requests, string operationName)
        {
            if (requests == null || requests.Count == 0)
            {
                tracing.Trace("{0}: no requests to send.", operationName);
                return;
            }

            for (int start = 0; start < requests.Count; start += BatchSize)
            {
                var batch = new ExecuteMultipleRequest
                {
                    Settings = new ExecuteMultipleSettings
                    {
                        ContinueOnError = false,   // stop at the first failure
                        ReturnResponses = true     // we need the responses to read faults
                    },
                    Requests = new OrganizationRequestCollection()
                };

                foreach (OrganizationRequest request in requests.Skip(start).Take(BatchSize))
                {
                    batch.Requests.Add(request);
                }

                var response = (ExecuteMultipleResponse)service.Execute(batch);

                if (response.IsFaulted)
                {
                    // Find the first response that carries an error
                    ExecuteMultipleResponseItem failed = response.Responses.FirstOrDefault(r => r.Fault != null);
                    string reason = failed != null ? failed.Fault.Message : "Unknown error.";

                    tracing.Trace("{0} failed at request {1}: {2}",
                        operationName, failed != null ? failed.RequestIndex : -1, reason);
                    throw new InvalidPluginExecutionException(operationName + " failed: " + reason);
                }
            }

            tracing.Trace("{0}: {1} request(s) completed.", operationName, requests.Count);
        }

        /// <summary>Convenience: builds UpdateRequests and runs them.</summary>
        public static void UpdateRecords(IOrganizationService service, ITracingService tracing,
            IEnumerable<Entity> records, string operationName)
        {
            List<OrganizationRequest> requests = records
                .Select(r => (OrganizationRequest)new UpdateRequest { Target = r })
                .ToList();
            Execute(service, tracing, requests, operationName);
        }

        /// <summary>Convenience: builds DeleteRequests and runs them.</summary>
        public static void DeleteRecords(IOrganizationService service, ITracingService tracing,
            IEnumerable<EntityReference> records, string operationName)
        {
            List<OrganizationRequest> requests = records
                .Select(r => (OrganizationRequest)new DeleteRequest { Target = r })
                .ToList();
            Execute(service, tracing, requests, operationName);
        }

        /// <summary>Convenience: builds CreateRequests and runs them.</summary>
        public static void CreateRecords(IOrganizationService service, ITracingService tracing,
            IEnumerable<Entity> records, string operationName)
        {
            List<OrganizationRequest> requests = records
                .Select(r => (OrganizationRequest)new CreateRequest { Target = r })
                .ToList();
            Execute(service, tracing, requests, operationName);
        }
    }
}