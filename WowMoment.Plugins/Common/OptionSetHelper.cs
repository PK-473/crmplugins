using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace WowMoment.Plugins.Common
{
    public static class OptionSetHelper
    {
        /// <summary>
        /// Returns the option value for a label (e.g. "Approved" -> 1).
        /// Works for local and global choices. No hardcoded numbers.
        /// </summary>
        public static int GetValueByLabel(IOrganizationService service,
                                          string entityLogicalName,
                                          string attributeLogicalName,
                                          string label)
        {
            var request = new RetrieveAttributeRequest
            {
                EntityLogicalName = entityLogicalName,
                LogicalName = attributeLogicalName,
                RetrieveAsIfPublished = true
            };
            var response = (RetrieveAttributeResponse)service.Execute(request);

            var metadata = response.AttributeMetadata as EnumAttributeMetadata;
            if (metadata == null || metadata.OptionSet == null)
                throw new InvalidPluginExecutionException(
                    "Column '" + attributeLogicalName + "' on '" + entityLogicalName + "' is not a choice column.");

            foreach (var option in metadata.OptionSet.Options)
            {
                var text = option.Label != null && option.Label.UserLocalizedLabel != null
                    ? option.Label.UserLocalizedLabel.Label : null;

                if (string.Equals(text, label, StringComparison.OrdinalIgnoreCase) && option.Value.HasValue)
                    return option.Value.Value;
            }

            throw new InvalidPluginExecutionException(
                "Choice label '" + label + "' was not found on '" + attributeLogicalName + "'.");
        }
    }
}
