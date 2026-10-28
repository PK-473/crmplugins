using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using WowMoment.Plugins.Base;
using WowMoment.Plugins.Common;
using WowMoment.Plugins.Constants;

namespace WowMoment.Plugins.Workflows
{
    /// <summary>
    /// Task 2: reusable workflow activity. Reads all Story Items of a Story and returns
    /// total hours and the Open / In-Progress / Completed counts.
    /// </summary>
    public sealed class CalculateStoryMetricsActivity : CodeActivityBase
    {
        [RequiredArgument]
        [Input("Story Reference")]
        [ReferenceTarget(StoryMetricsFields.StoryLogicalName)]
        public InArgument<EntityReference> StoryReference { get; set; }

        [Output("TotalHours")]
        public OutArgument<decimal> TotalHours { get; set; }

        [Output("OpenCount")]
        public OutArgument<int> OpenCount { get; set; }

        [Output("InProgressCount")]
        public OutArgument<int> InProgressCount { get; set; }

        [Output("CompletedCount")]
        public OutArgument<int> CompletedCount { get; set; }

        protected override void Run(CodeActivityContext context,
                                    IOrganizationService service,
                                    ITracingService tracing)
        {
            // ---- Validation 1: reference provided ----
            EntityReference story = StoryReference.Get(context);
            if (story == null || story.Id == Guid.Empty)
                throw new InvalidPluginExecutionException("Story reference must be provided.");

            if (story.LogicalName != StoryMetricsFields.StoryLogicalName)
                throw new InvalidPluginExecutionException("The reference must point to a Story record.");

            // ---- Validation 2: story exists ----
            if (!StoryExists(service, story.Id))
                throw new InvalidPluginExecutionException("The selected Story record does not exist.");

            // ---- Status values from metadata (no hardcoded numbers) ----
            int openValue = GetStatus(service, StoryMetricsFields.OpenLabel);
            int inProgressValue = GetStatus(service, StoryMetricsFields.InProgressLabel);
            int completedValue = GetStatus(service, StoryMetricsFields.CompletedLabel);

            decimal totalHours = 0m;
            int open = 0, inProgress = 0, completed = 0;

            // ---- Retrieve all items (paged, 5000 per page) ----
            var query = new QueryExpression(StoryMetricsFields.ItemLogicalName)
            {
                ColumnSet = new ColumnSet(StoryMetricsFields.ItemHoursRequired, StoryMetricsFields.ItemCurrentStatus),
                PageInfo = new PagingInfo { PageNumber = 1, Count = 5000 }
            };
            query.Criteria.AddCondition(StoryMetricsFields.ItemStory, ConditionOperator.Equal, story.Id);

            EntityCollection page;
            do
            {
                page = service.RetrieveMultiple(query);

                foreach (Entity item in page.Entities)
                {
                    // Null hours count as 0
                    totalHours += item.GetAttributeValue<decimal?>(StoryMetricsFields.ItemHoursRequired) ?? 0m;

                    var status = item.GetAttributeValue<OptionSetValue>(StoryMetricsFields.ItemCurrentStatus);
                    if (status == null) continue;

                    if (status.Value == openValue) open++;
                    else if (status.Value == inProgressValue) inProgress++;
                    else if (status.Value == completedValue) completed++;
                }

                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = page.PagingCookie;
            }
            while (page.MoreRecords);

            tracing.Trace("Story {0}: hours={1}, open={2}, inProgress={3}, completed={4}",
                story.Id, totalHours, open, inProgress, completed);

            // ---- Outputs (all zero when the story has no items) ----
            TotalHours.Set(context, totalHours);
            OpenCount.Set(context, open);
            InProgressCount.Set(context, inProgress);
            CompletedCount.Set(context, completed);
        }

        private static int GetStatus(IOrganizationService service, string label)
        {
            return OptionSetHelper.GetValueByLabel(service,
                StoryMetricsFields.ItemLogicalName, StoryMetricsFields.ItemCurrentStatus, label);
        }

        private static bool StoryExists(IOrganizationService service, Guid storyId)
        {
            var query = new QueryExpression(StoryMetricsFields.StoryLogicalName)
            {
                ColumnSet = new ColumnSet(false),
                TopCount = 1
            };
            query.Criteria.AddCondition(StoryMetricsFields.StoryPrimaryKey, ConditionOperator.Equal, storyId);
            return service.RetrieveMultiple(query).Entities.Count > 0;
        }
    }
}
