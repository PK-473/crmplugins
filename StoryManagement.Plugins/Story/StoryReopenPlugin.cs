using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using StoryManagement.Plugins.Base;
using StoryManagement.Plugins.Helpers;

namespace StoryManagement.Plugins.Story
{
    /// <summary>
    /// Task 3 (reopen): when a Completed Story goes back to Open or In-Progress,
    /// set every Story Item to the same status.
    /// Registered on: sm_story | Update | Post-operation (40) | ASYNCHRONOUS
    /// Filtering attribute: sm_currentstatus | Pre Image: StoryPreImage
    /// </summary>
    public class StoryReopenPlugin : PluginBase
    {
        protected override void Run(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var target = context.InputParameters["Target"] as Entity;
            if (target == null || target.LogicalName != StoryFields.LogicalName)
            {
                tracing.Trace("Target is not a Story. Nothing to do.");
                return;
            }

            int? newStatus = StoryHelper.GetStatus(target, StoryFields.CurrentStatus);
            StatusValues status = StoryHelper.GetStatusValues(service);

            // Only Open or In-Progress count as "reopened"
            if (newStatus != status.Open && newStatus != status.InProgress)
            {
                tracing.Trace("New status is not Open or In-Progress. Nothing to do.");
                return;
            }

            // It must have been Completed before (this needs the Pre Image)
            Entity preImage = context.PreEntityImages.Contains(ImageNames.StoryPreImage)
                ? context.PreEntityImages[ImageNames.StoryPreImage] : null;
            int? oldStatus = preImage == null ? (int?)null : StoryHelper.GetStatus(preImage, StoryFields.CurrentStatus);
            if (oldStatus != status.Completed)
            {
                tracing.Trace("Story was not Completed before. Not a reopen.");
                return;
            }

            // Build one update per item that does not already have the new status
            List<Entity> items = StoryHelper.GetStoryItems(service, target.Id, StoryItemFields.CurrentStatus);
            var updates = new List<Entity>();
            foreach (Entity item in items)
            {
                if (StoryHelper.GetStatus(item, StoryItemFields.CurrentStatus) == newStatus) { continue; }

                var change = new Entity(StoryItemFields.LogicalName, item.Id);
                change[StoryItemFields.CurrentStatus] = new OptionSetValue(newStatus.Value);
                updates.Add(change);
            }

            ExecuteMultipleHelper.UpdateRecords(service, tracing, updates, "Reopen Story Items");
            tracing.Trace("{0} Story Items set to the new status.", updates.Count);
        }
    }
}