using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using StoryManagement.Plugins.Base;
using StoryManagement.Plugins.Helpers;

namespace StoryManagement.Plugins.Story
{
    /// <summary>
    /// Task 6: when a Story is deleted, delete its Story Items first.
    /// Registered on: sm_story | Delete | Pre-operation (20) | Synchronous
    /// Pre Image: StoryDeleteImage (Story Name)
    /// </summary>
    public class StoryDeletePlugin : PluginBase
    {
        protected override void Run(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            // On Delete, Target is only a reference (table + id), not a full record
            var story = context.InputParameters["Target"] as EntityReference;
            if (story == null || story.LogicalName != StoryFields.LogicalName)
            {
                tracing.Trace("Target is not a Story. Nothing to do.");
                return;
            }

            // The Pre Image gives us the Story Name for clear messages
            Entity pre = context.PreEntityImages.Contains(ImageNames.StoryDeleteImage)
                ? context.PreEntityImages[ImageNames.StoryDeleteImage] : null;
            string storyName = pre == null ? null : pre.GetAttributeValue<string>(StoryFields.Name);
            if (string.IsNullOrWhiteSpace(storyName)) { storyName = "this story"; }

            // Retrieve the related items (only their ids are needed)
            List<Entity> items = StoryHelper.GetStoryItems(service, story.Id);

            // Never assume records exist: with no items, the delete simply continues
            if (items.Count == 0)
            {
                tracing.Trace("Story {0} has no Story Items. Delete continues.", story.Id);
                return;
            }

            List<EntityReference> toDelete = items.Select(i => i.ToEntityReference()).ToList();

            try
            {
                ExecuteMultipleHelper.DeleteRecords(service, tracing, toDelete, "Delete Story Items");
            }
            catch (InvalidPluginExecutionException ex)
            {
                // Make the message say what happened and what the user can do
                throw new InvalidPluginExecutionException(string.Format(
                    "Unable to delete the related Story Items of '{0}', so the story was not deleted. " +
                    "Please try again, or contact your administrator. Details: {1}",
                    storyName, ex.Message), ex);
            }

            tracing.Trace("{0} Story Items deleted for story {1}.", toDelete.Count, story.Id);
        }
    }
}