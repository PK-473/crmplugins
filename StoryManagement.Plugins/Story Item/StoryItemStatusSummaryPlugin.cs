using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using StoryManagement.Plugins.Base;
using StoryManagement.Plugins.Helpers;

namespace StoryManagement.Plugins.StoryItem
{
    /// <summary>
    /// Task 4: when a Story Item's status changes, recount the items on the parent Story.
    /// Registered on: sm_storyitem | Update | Post-operation (40) | Synchronous
    /// Filtering attribute: sm_currentstatus
    /// </summary>
    public class StoryItemStatusSummaryPlugin : PluginBase
    {
        protected override void Run(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var target = context.InputParameters["Target"] as Entity;
            if (target == null || target.LogicalName != StoryItemFields.LogicalName)
            {
                tracing.Trace("Target is not a Story Item. Nothing to do.");
                return;
            }

            // Target only holds the changed columns, so read the parent Story from the item itself
            Entity item = service.Retrieve(StoryItemFields.LogicalName, target.Id,
                new ColumnSet(StoryItemFields.StoryLookup));
            EntityReference story = item.GetAttributeValue<EntityReference>(StoryItemFields.StoryLookup);
            if (story == null)
            {
                tracing.Trace("Story Item {0} has no parent Story. Nothing to update.", target.Id);
                return;
            }

            // Retrieve every item of that Story and count them by status
            StatusValues status = StoryHelper.GetStatusValues(service);
            List<Entity> items = StoryHelper.GetStoryItems(service, story.Id, StoryItemFields.CurrentStatus);

            int open = 0, inProgress = 0, completed = 0;
            foreach (Entity i in items)
            {
                int? value = StoryHelper.GetStatus(i, StoryItemFields.CurrentStatus);
                if (value == status.Open) { open++; }
                else if (value == status.InProgress) { inProgress++; }
                else if (value == status.Completed) { completed++; }
            }

            // Update ONLY the three count columns. They are not in the filtering
            // columns of the Story plugins, so this update will not start them again.
            var update = new Entity(StoryFields.LogicalName, story.Id);
            update[StoryFields.OpenItems] = open;
            update[StoryFields.InProgressItems] = inProgress;
            update[StoryFields.CompletedItems] = completed;
            service.Update(update);

            tracing.Trace("Story {0} counts: Open={1}, In-Progress={2}, Completed={3}.", story.Id, open, inProgress, completed);
        }
    }
}