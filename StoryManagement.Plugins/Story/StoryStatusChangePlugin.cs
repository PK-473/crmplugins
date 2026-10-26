using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using StoryManagement.Plugins.Base;
using StoryManagement.Plugins.Helpers;

namespace StoryManagement.Plugins.Story
{
    /// <summary>
    /// Task 3 (validation): a Story cannot be Completed while any Story Item is Open or In-Progress.
    /// Registered on: sm_story | Update | Pre-operation (20) | Synchronous
    /// Filtering attribute: sm_currentstatus | Pre Image: StoryPreImage
    /// </summary>
    public class StoryStatusChangePlugin : PluginBase
    {
        protected override void Run(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var target = context.InputParameters["Target"] as Entity;
            if (target == null || target.LogicalName != StoryFields.LogicalName)
            {
                tracing.Trace("Target is not a Story. Nothing to do.");
                return;
            }

            // The new status the user chose (null when the status was not part of this update)
            int? newStatus = StoryHelper.GetStatus(target, StoryFields.CurrentStatus);
            if (newStatus == null)
            {
                tracing.Trace("Current Status is not in this update.");
                return;
            }

            StatusValues status = StoryHelper.GetStatusValues(service);
            if (newStatus != status.Completed)
            {
                tracing.Trace("New status is not Completed. Nothing to validate.");
                return;
            }

            // Pre Image = the Story as it was BEFORE this update
            Entity preImage = context.PreEntityImages.Contains(ImageNames.StoryPreImage)
                ? context.PreEntityImages[ImageNames.StoryPreImage] : null;
            int? oldStatus = preImage == null ? (int?)null : StoryHelper.GetStatus(preImage, StoryFields.CurrentStatus);
            if (oldStatus == status.Completed)
            {
                tracing.Trace("Story was already Completed. Not a status change.");
                return;
            }

            // Retrieve the related items and look for any that are not finished
            List<Entity> items = StoryHelper.GetStoryItems(service, target.Id, StoryItemFields.CurrentStatus);
            int open = 0, inProgress = 0;
            foreach (Entity item in items)
            {
                int? value = StoryHelper.GetStatus(item, StoryItemFields.CurrentStatus);
                if (value == status.Open) { open++; }
                else if (value == status.InProgress) { inProgress++; }
            }

            if (open + inProgress > 0)
            {
                string storyName = preImage != null ? preImage.GetAttributeValue<string>(StoryFields.Name) : null;
                throw new InvalidPluginExecutionException(string.Format(
                    "The story '{0}' cannot be marked as Completed because {1} of its Story Items are not completed yet " +
                    "({2} Open, {3} In-Progress). Please complete all Story Items first.",
                    string.IsNullOrWhiteSpace(storyName) ? "this story" : storyName,
                    open + inProgress, open, inProgress));
            }

            tracing.Trace("All {0} Story Items are completed. Completion allowed.", items.Count);
        }
    }
}