using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using StoryManagement.Plugins.Base;
using StoryManagement.Plugins.Helpers;

namespace StoryManagement.Plugins.Story
{
    /// <summary>
    /// Task 3 (completion): calculate Actual Hours Spent and create the review email.
    /// Registered on: sm_story | Update | Post-operation (40) | Synchronous
    /// Filtering attribute: sm_currentstatus | Pre Image: StoryPreImage
    /// </summary>
    public class StoryCompletePlugin : PluginBase
    {
        private const string EmailDescription =
            "The Story has been successfully marked as completed. Please review the details and ensure " +
            "everything is in order. If there are any discrepancies or further actions required, " +
            "kindly address them at your earliest convenience.";

        protected override void Run(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var target = context.InputParameters["Target"] as Entity;
            if (target == null || target.LogicalName != StoryFields.LogicalName)
            {
                tracing.Trace("Target is not a Story. Nothing to do.");
                return;
            }

            // Run only when the status CHANGED TO Completed
            int? newStatus = StoryHelper.GetStatus(target, StoryFields.CurrentStatus);
            StatusValues status = StoryHelper.GetStatusValues(service);
            if (newStatus != status.Completed)
            {
                tracing.Trace("New status is not Completed. Nothing to do.");
                return;
            }

            Entity preImage = context.PreEntityImages.Contains(ImageNames.StoryPreImage)
                ? context.PreEntityImages[ImageNames.StoryPreImage] : null;
            int? oldStatus = preImage == null ? (int?)null : StoryHelper.GetStatus(preImage, StoryFields.CurrentStatus);
            if (oldStatus == status.Completed)
            {
                tracing.Trace("Story was already Completed. Nothing to do.");
                return;
            }

            // The review email goes to the assigned user's manager. Check that FIRST:
            // if we cannot send it, stop with a clear message before changing anything.
            Entity story = service.Retrieve(StoryFields.LogicalName, target.Id,
                new ColumnSet(StoryFields.Name, "ownerid"));
            string storyName = story.GetAttributeValue<string>(StoryFields.Name);
            EntityReference owner = story.GetAttributeValue<EntityReference>("ownerid");
            EntityReference manager = ResolveManager(service, tracing, storyName, owner);

            // 1. Actual Hours Spent = sum of Hours Required of all items
            List<Entity> items = StoryHelper.GetStoryItems(service, target.Id, StoryItemFields.HoursRequired);
            decimal actualHours = 0m;
            foreach (Entity item in items)
            {
                actualHours += item.GetAttributeValue<decimal?>(StoryItemFields.HoursRequired).GetValueOrDefault();
            }

            // Only the hours column changes, and it is not a filtering column, so this does not loop
            var update = new Entity(StoryFields.LogicalName, target.Id);
            update[StoryFields.ActualHours] = actualHours;
            service.Update(update);
            tracing.Trace("Actual Hours Spent set to {0}.", actualHours);

            // 2. Review email to the manager
            CreateReviewEmail(service, tracing, storyName, owner, manager, target.Id);
        }

        /// <summary>
        /// Finds the manager of the assigned user. Throws a clear message when there is none,
        /// so the user knows why completion was blocked.
        /// </summary>
        private static EntityReference ResolveManager(IOrganizationService service, ITracingService tracing,
            string storyName, EntityReference owner)
        {
            // The owner can be a team. Only a user has a manager.
            if (owner == null || owner.LogicalName != "systemuser")
            {
                tracing.Trace("Story is not assigned to a user.");
                throw new InvalidPluginExecutionException(string.Format(
                    "The story '{0}' cannot be completed because it is assigned to a team. " +
                    "The review email goes to the manager of the assigned user, so please assign the story to a user first.",
                    storyName));
            }

            Entity user = service.Retrieve("systemuser", owner.Id, new ColumnSet("fullname", "parentsystemuserid"));
            EntityReference manager = user.GetAttributeValue<EntityReference>("parentsystemuserid");
            if (manager == null)
            {
                tracing.Trace("User {0} has no manager.", owner.Id);
                throw new InvalidPluginExecutionException(string.Format(
                    "The story '{0}' cannot be completed because {1} has no manager. " +
                    "The review email cannot be sent. Please ask your administrator to set the Manager on the user record, then try again.",
                    storyName, user.GetAttributeValue<string>("fullname")));
            }

            tracing.Trace("Manager {0} found for user {1}.", manager.Id, owner.Id);
            return manager;
        }

        private static void CreateReviewEmail(IOrganizationService service, ITracingService tracing,
            string storyName, EntityReference owner, EntityReference manager, Guid storyId)
        {
            var email = new Entity("email");
            email["subject"] = string.Format("{0} is completed, please review", storyName);
            email["description"] = EmailDescription;
            email["directioncode"] = true;                 // true = outgoing email

            // Regarding = this Story. This is what makes the email appear in the
            // Story's Timeline (the Story table needs "Activities" enabled).
            email["regardingobjectid"] = new EntityReference(StoryFields.LogicalName, storyId);
            email["from"] = new EntityCollection(new List<Entity> { Party(owner) });
            email["to"] = new EntityCollection(new List<Entity> { Party(manager) });

            try
            {
                Guid emailId = service.Create(email);      // saved as a draft; it is not sent
                tracing.Trace("Review email {0} created for manager {1}.", emailId, manager.Id);
            }
            catch (Exception ex)
            {
                tracing.Trace("Email creation failed: {0}", ex.ToString());
                throw new InvalidPluginExecutionException(
                    "The story could not be completed because the review email to the manager could not be created. " +
                    "Please contact your administrator. Details: " + ex.Message, ex);
            }
        }

        // An email's To/From are "activity party" rows that point at a user
        private static Entity Party(EntityReference user)
        {
            var party = new Entity("activityparty");
            party["partyid"] = user;
            return party;
        }
    }
}
