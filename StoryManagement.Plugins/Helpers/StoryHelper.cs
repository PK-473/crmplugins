using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using StoryManagement.Plugins.Base;

namespace StoryManagement.Plugins.Helpers
{
    /// <summary>The three status numbers, looked up once per plugin run.</summary>
    internal sealed class StatusValues
    {
        public int Open { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
    }

    /// <summary>Small shared functions used by several plugins.</summary>
    internal static class StoryHelper
    {
        /// <summary>
        /// Finds a choice option's number from its label (e.g. "Open").
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
                string.Format("The option '{0}' was not found on column '{1}'.", label, columnName));
        }

        /// <summary>
        /// The Story and Story Item tables share one global choice, so the numbers are
        /// the same on both. We read them from the Story Item column.
        /// </summary>
        public static StatusValues GetStatusValues(IOrganizationService service)
        {
            return new StatusValues
            {
                Open = GetOptionValue(service, StoryItemFields.LogicalName, StoryItemFields.CurrentStatus, StoryStatus.Open),
                InProgress = GetOptionValue(service, StoryItemFields.LogicalName, StoryItemFields.CurrentStatus, StoryStatus.InProgress),
                Completed = GetOptionValue(service, StoryItemFields.LogicalName, StoryItemFields.CurrentStatus, StoryStatus.Completed)
            };
        }

        /// <summary>The number in a choice column, or null when the column is empty.</summary>
        public static int? GetStatus(Entity record, string column)
        {
            OptionSetValue value = record.GetAttributeValue<OptionSetValue>(column);
            return value == null ? (int?)null : value.Value;
        }

        /// <summary>
        /// Retrieves ALL Story Items of a Story with QueryExpression.
        /// It pages through the results, because one call returns at most 5000 rows.
        /// </summary>
        public static List<Entity> GetStoryItems(IOrganizationService service, Guid storyId, params string[] columns)
        {
            var query = new QueryExpression(StoryItemFields.LogicalName)
            {
                ColumnSet = new ColumnSet(columns),
                PageInfo = new PagingInfo { Count = 500, PageNumber = 1 }
            };
            query.Criteria.AddCondition(StoryItemFields.StoryLookup, ConditionOperator.Equal, storyId);

            var all = new List<Entity>();
            while (true)
            {
                EntityCollection page = service.RetrieveMultiple(query);
                all.AddRange(page.Entities);

                if (!page.MoreRecords) { break; }
                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = page.PagingCookie;
            }
            return all;
        }

        /// <summary>Cuts text to a maximum length (the Story Item Name allows 500 characters).</summary>
        public static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) { return text; }
            return text.Substring(0, maxLength);
        }
    }
}
