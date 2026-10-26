using System.Globalization;
using System.Security;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using StoryManagement.Plugins.Base;

namespace StoryManagement.Plugins.Helpers
{
    /// <summary>All FetchXML queries live here, so the plugins stay short and readable.</summary>
    internal static class FetchXmlHelper
    {
        /// <summary>
        /// Task 1: true when a Story with the same name, estimated hours and days already exists.
        /// </summary>
        public static bool StoryExists(IOrganizationService service, ITracingService tracing,
            string name, decimal estHours, int days)
        {
            // SecurityElement.Escape makes characters like & < > ' " safe inside XML.
            // InvariantCulture makes decimals print as 10.5 on every server, never 10,5.
            string fetchXml = string.Format(CultureInfo.InvariantCulture,
                @"<fetch top='1'>
                    <entity name='{0}'>
                      <attribute name='{1}' />
                      <filter type='and'>
                        <condition attribute='{2}' operator='eq' value='{3}' />
                        <condition attribute='{4}' operator='eq' value='{5}' />
                        <condition attribute='{6}' operator='eq' value='{7}' />
                      </filter>
                    </entity>
                  </fetch>",
                StoryFields.LogicalName, StoryFields.Id,
                StoryFields.Name, SecurityElement.Escape(name),
                StoryFields.EstHours, estHours,
                StoryFields.DaysRequired, days);

            tracing.Trace("Duplicate check FetchXML: {0}", fetchXml);

            EntityCollection result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Never assume a record exists: always check the count
            return result.Entities.Count > 0;
        }
    }
}