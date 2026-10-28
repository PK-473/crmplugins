using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace WowMoment.Plugins.Helpers
{
    internal static class OptionHelper
    {
        /// <summary>
        /// Finds a choice option's number from its label (for example "Approved" or "4 Star").
        /// Reading it from metadata means no option number is hardcoded.
        /// </summary>
        public static int GetOptionValue(IOrganizationService service, string tableName,
            string columnName, string label)
        {
            var request = new RetrieveAttributeRequest
            {
                EntityLogicalName = tableName,
                LogicalName = columnName,
                RetrieveAsIfPublished = true
            };
            var response = (RetrieveAttributeResponse)service.Execute(request);

            var picklist = response.AttributeMetadata as PicklistAttributeMetadata;
            if (picklist == null)
            {
                throw new InvalidPluginExecutionException(
                    string.Format("Column '{0}' on '{1}' is not a choice column.", columnName, tableName));
            }

            foreach (OptionMetadata option in picklist.OptionSet.Options)
            {
                // Check every language label, not only the current user's
                bool match = option.Label != null && option.Label.LocalizedLabels.Any(
                    l => string.Equals(l.Label, label, StringComparison.OrdinalIgnoreCase));
                if (match && option.Value.HasValue)
                {
                    return option.Value.Value;
                }
            }

            throw new InvalidPluginExecutionException(
                string.Format("The option '{0}' was not found on column '{1}'. Please contact your administrator.",
                    label, columnName));
        }
    }
}