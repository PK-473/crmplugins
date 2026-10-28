using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using StoryManagement.Plugins.Base;
using StoryManagement.Plugins.Helpers;
using System.Activities;
using System.Collections.Generic;

namespace StoryManagement.Plugins.Activities
{
    /// <summary>
    /// Calculates Story metrics from Story Items and updates the Story.
    /// </summary>
    public class CalculateStoryMetrics : WorkflowBase
    {
        [Input("Story")]
        [ReferenceTarget(StoryFields.LogicalName)]
        [RequiredArgument]
        public InArgument<EntityReference> Story { get; set; }

        [Output("TotalHours")]
        public OutArgument<decimal> TotalHours { get; set; }

        [Output("OpenCount")]
        public OutArgument<int> OpenCount { get; set; }

        [Output("InProgressCount")]
        public OutArgument<int> InProgressCount { get; set; }

        [Output("CompletedCount")]
        public OutArgument<int> CompletedCount { get; set; }

        protected override void Run(
            CodeActivityContext context,
            IWorkflowContext workflowContext,
            IOrganizationService service,
            ITracingService tracing)
        {
            EntityReference storyRef =
                Story.Get(context);

            if (storyRef == null)
            {
                throw new InvalidPluginExecutionException(
                    "Story is required.");
            }

            tracing.Trace(
                "Calculating metrics for Story {0}",
                storyRef.Id);

            StatusValues statusValues =
                StoryHelper.GetStatusValues(service);

            List<Entity> storyItems =
                StoryHelper.GetStoryItems(
                    service,
                    storyRef.Id,
                    StoryItemFields.HoursRequired,
                    StoryItemFields.CurrentStatus);

            decimal totalHours = 0;
            int openCount = 0;
            int inProgressCount = 0;
            int completedCount = 0;

            foreach (Entity storyItem in storyItems)
            {
                decimal hours =
                    storyItem.Contains(StoryItemFields.HoursRequired)
                        ? storyItem.GetAttributeValue<decimal>(
                            StoryItemFields.HoursRequired)
                        : 0;

                totalHours += hours;

                OptionSetValue status =
                    storyItem.GetAttributeValue<OptionSetValue>(
                        StoryItemFields.CurrentStatus);

                if (status == null)
                {
                    continue;
                }

                if (status.Value == statusValues.Open)
                {
                    openCount++;
                }
                else if (status.Value == statusValues.InProgress)
                {
                    inProgressCount++;
                }
                else if (status.Value == statusValues.Completed)
                {
                    completedCount++;
                }
            }

            tracing.Trace(
                "Hours={0}, Open={1}, InProgress={2}, Completed={3}",
                totalHours,
                openCount,
                inProgressCount,
                completedCount);

            // Update Story record directly
            Entity storyToUpdate =
                new Entity(
                    StoryFields.LogicalName,
                    storyRef.Id);

            storyToUpdate[StoryFields.EstHours] =
                totalHours;

            storyToUpdate[StoryFields.OpenItems] =
                openCount;

            storyToUpdate[StoryFields.InProgressItems] =
                inProgressCount;

            storyToUpdate[StoryFields.CompletedItems] =
                completedCount;

            service.Update(storyToUpdate);

            tracing.Trace(
                "Story metrics updated successfully.");

            // Return outputs as well (kept for task compliance)
            TotalHours.Set(
                context,
                totalHours);

            OpenCount.Set(
                context,
                openCount);

            InProgressCount.Set(
                context,
                inProgressCount);

            CompletedCount.Set(
                context,
                completedCount);
        }
    }
}
