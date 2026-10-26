using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using StoryManagement.Plugins.Base;
using StoryManagement.Plugins.Helpers;
 
namespace StoryManagement.Plugins.Story
{
    /// <summary>
    /// Task 2: after a Story is created, create one Story Item per day.
    /// Registered on: sm_story | Create | Post-operation (40) | Synchronous.
    /// </summary>
    public class StoryCreateItemsPlugin : PluginBase
    {
        private const string OpenLabel = "Open";
        private const int ItemNameMaxLength = 450;   // column allows 500, leave room for " - Item 90"
 
        protected override void Run(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            if (context.PrimaryEntityName != StoryFields.LogicalName || context.MessageName != "Create")
            {
                tracing.Trace("Not a Story Create. Nothing to do.");
                return;
            }
 
            // Post-operation: the Story exists now, so its Id is available
            Guid storyId = context.PrimaryEntityId;
 
            // 1. Retrieve the values we need from the saved Story
            Entity story = service.Retrieve(StoryFields.LogicalName, storyId,
                new ColumnSet(StoryFields.Name, StoryFields.EstHours, StoryFields.DaysRequired, "ownerid"));
 
            string storyName = story.GetAttributeValue<string>(StoryFields.Name);
            decimal estHours = story.GetAttributeValue<decimal?>(StoryFields.EstHours).GetValueOrDefault();
            int days = story.GetAttributeValue<int?>(StoryFields.DaysRequired).GetValueOrDefault();
            EntityReference owner = story.GetAttributeValue<EntityReference>("ownerid");
 
            if (days <= 0)
            {
                tracing.Trace("No of Days Required is empty or 0. No items created.");
                return;
            }
 
            // 2. Find the number for the "Open" option (not hardcoded)
            int openStatus = StoryHelper.GetOptionValue(service,
                StoryItemFields.LogicalName, StoryItemFields.CurrentStatus, OpenLabel);
 
            // 3. Hours per item. Round DOWN to 2 decimals, and give the leftover
            //    to the last item so the items always add up to the story's estimate.
            decimal hoursPerItem = Math.Floor(estHours / days * 100m) / 100m;
            decimal allocated = 0m;
 
            var items = new List<Entity>();
            for (int i = 1; i <= days; i++)
            {
                decimal hours = (i == days) ? estHours - allocated : hoursPerItem;
                allocated += hours;
 
                var item = new Entity(StoryItemFields.LogicalName);
                item[StoryItemFields.StoryItemName] = string.Format("{0} - Item {1}",
                    StoryHelper.Truncate(storyName, ItemNameMaxLength), i);
                item[StoryItemFields.HoursRequired] = hours;
                item[StoryItemFields.CurrentStatus] = new OptionSetValue(openStatus);
                item[StoryItemFields.StoryLookup] = new EntityReference(StoryFields.LogicalName, storyId);
                if (owner != null)
                {
                    item["ownerid"] = owner;   // same owner as the Story
                }
                items.Add(item);
            }
 
            // 4. Create all items in one batch. A failure throws, so the Story
            //    creation rolls back too (no Story without its items).
            ExecuteMultipleHelper.CreateRecords(service, tracing, items, "Create Story Items");
 
            tracing.Trace("{0} Story Items created for story {1}.", items.Count, storyId);
        }
    }
}
 