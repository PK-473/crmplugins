//using System;
//using Microsoft.Xrm.Sdk;
//using Microsoft.Xrm.Sdk.Query;
//using WowMoment.Plugins.Base;
//using WowMoment.Plugins.Common;
//using WowMoment.Plugins.Constants;

//namespace WowMoment.Plugins.Actions
//{
//    /// <summary>
//    /// Task 1: backs the custom action wow_CreateFeedback (bound to Wow Moment).
//    /// Register on the action's message, Post-operation, Synchronous.
//    /// </summary>
//    public class CreateFeedbackPlugin : PluginBase
//    {
//        protected override void Run(IPluginExecutionContext context,
//                                    IOrganizationService service,
//                                    ITracingService tracing)
//        {
//            // ---- Read inputs ----
//            Guid wowMomentId = GetInput<Guid>(context, "WowMomentId");
//            int rating = GetInput<int>(context, "Rating");
//            string comments = GetInput<string>(context, "Comments");
//            tracing.Trace("Inputs: WowMomentId={0}, Rating={1}", wowMomentId, rating);

//            // ---- Validation 1: Wow Moment must exist ----
//            Entity wowMoment = GetWowMoment(service, wowMomentId);
//            if (wowMoment == null)
//                throw new InvalidPluginExecutionException("The selected Wow Moment record does not exist.");

//            // ---- Validation 2: Management Approval must be Approved ----
//            int approvedValue = OptionSetHelper.GetValueByLabel(service,
//                WowMomentFields.LogicalName, WowMomentFields.ManagementApproval, WowMomentFields.ApprovedLabel);

//            var approval = wowMoment.GetAttributeValue<OptionSetValue>(WowMomentFields.ManagementApproval);
//            if (approval == null || approval.Value != approvedValue)
//                throw new InvalidPluginExecutionException(
//                    "Feedback can be created only for an Approved Wow Moment.");

//            // ---- Validation 3: no duplicate feedback ----
//            if (FeedbackExists(service, wowMomentId))
//                throw new InvalidPluginExecutionException(
//                    "Feedback already exists for this Wow Moment.");

//            // ---- Validation 4: rating range ----
//            if (rating < EventFeedbackFields.MinRating || rating > EventFeedbackFields.MaxRating)
//                throw new InvalidPluginExecutionException(string.Format(
//                    "Rating must be between {0} and {1}.",
//                    EventFeedbackFields.MinRating, EventFeedbackFields.MaxRating));

//            // ---- Create the feedback ----
//            int submittedValue = OptionSetHelper.GetValueByLabel(service,
//                EventFeedbackFields.LogicalName, EventFeedbackFields.FeedbackStatus, EventFeedbackFields.SubmittedLabel);

//            string wowName = wowMoment.GetAttributeValue<string>(WowMomentFields.PrimaryName);

//            var feedback = new Entity(EventFeedbackFields.LogicalName);
//            feedback[EventFeedbackFields.Name] = "Feedback - " + wowName;
//            feedback[EventFeedbackFields.WowMoment] = new EntityReference(WowMomentFields.LogicalName, wowMomentId);
//            feedback[EventFeedbackFields.Rating] = rating;
//            feedback[EventFeedbackFields.FeedbackDate] = DateTime.UtcNow.Date;
//            feedback[EventFeedbackFields.FeedbackStatus] = new OptionSetValue(submittedValue);
//            if (!string.IsNullOrWhiteSpace(comments))
//                feedback[EventFeedbackFields.Comments] = comments.Trim();

//            Guid feedbackId = service.Create(feedback);
//            tracing.Trace("Feedback created: {0}", feedbackId);

//            // ---- Output parameters ----
//            context.OutputParameters["FeedbackId"] = feedbackId;
//            context.OutputParameters["Message"] = "Feedback created successfully.";
//        }

//        private static T GetInput<T>(IPluginExecutionContext context, string name)
//        {
//            object value;
//            if (context.InputParameters.TryGetValue(name, out value) && value is T)
//                return (T)value;
//            return default(T);
//        }

//        private static Entity GetWowMoment(IOrganizationService service, Guid id)
//        {
//            if (id == Guid.Empty) return null;

//            var query = new QueryExpression(WowMomentFields.LogicalName)
//            {
//                ColumnSet = new ColumnSet(WowMomentFields.PrimaryName, WowMomentFields.ManagementApproval),
//                TopCount = 1
//            };
//            query.Criteria.AddCondition(WowMomentFields.PrimaryKey, ConditionOperator.Equal, id);

//            var result = service.RetrieveMultiple(query);
//            return result.Entities.Count > 0 ? result.Entities[0] : null;
//        }

//        private static bool FeedbackExists(IOrganizationService service, Guid wowMomentId)
//        {
//            var query = new QueryExpression(EventFeedbackFields.LogicalName)
//            {
//                ColumnSet = new ColumnSet(false),
//                TopCount = 1
//            };
//            query.Criteria.AddCondition(EventFeedbackFields.WowMoment, ConditionOperator.Equal, wowMomentId);

//            return service.RetrieveMultiple(query).Entities.Count > 0;
//        }
//    }
//}


using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using WowMoment.Plugins.Base;
using WowMoment.Plugins.Helpers;

namespace WowMoment.Plugins.Actions
{
    /// <summary>
    /// Task 1: logic behind the custom action wow_CreateFeedback (bound to Wow Moment).
    /// Registered on: the action's message | Primary Entity pkk_wowmoment | Post-operation (40) | Synchronous.
    ///
    /// Input parameters : WowMomentId (EntityReference to Wow Moment), Rating (Integer), Comments (String)
    /// Output parameters: FeedbackId (EntityReference to Event Feedback), Message (String)
    /// </summary>
    public class CreateFeedbackPlugin : PluginBase
    {
        private const int MinRating = 1;
        private const int MaxRating = 5;
        private const int MaxCommentsLength = 1000;   // the Feedback Comments column allows 1000
        private const int MaxTitleLength = 100;       // keep the primary column value short

        protected override void Run(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            // ---------- Read the inputs ----------
            Guid wowMomentId = ReadWowMomentId(context);
            int? rating = ReadInput<int?>(context, "Rating");
            string comments = ReadInput<string>(context, "Comments");
            if (comments != null) { comments = comments.Trim(); }

            // ---------- Validation 1: the Wow Moment must exist ----------
            // Query instead of Retrieve: an empty result is easy to check, no exception to catch
            var query = new QueryExpression(WowMomentFields.LogicalName)
            {
                ColumnSet = new ColumnSet(WowMomentFields.Title, WowMomentFields.Approval, WowMomentFields.Contact),
                TopCount = 1
            };
            query.Criteria.AddCondition(WowMomentFields.Id, ConditionOperator.Equal, wowMomentId);
            EntityCollection found = service.RetrieveMultiple(query);

            if (found.Entities.Count == 0)
            {
                throw new InvalidPluginExecutionException(string.Format(
                    "The Wow Moment record {0} does not exist. Please check the WowMomentId and try again.", wowMomentId));
            }
            Entity wowMoment = found.Entities[0];
            string wowTitle = wowMoment.GetAttributeValue<string>(WowMomentFields.Title);

            // ---------- Validation 2: Management Approval must be Approved ----------
            int approvedValue = OptionHelper.GetOptionValue(service,
                WowMomentFields.LogicalName, WowMomentFields.Approval, "Approved");
            OptionSetValue approval = wowMoment.GetAttributeValue<OptionSetValue>(WowMomentFields.Approval);

            if (approval == null || approval.Value != approvedValue)
            {
                string currentLabel = wowMoment.FormattedValues.Contains(WowMomentFields.Approval)
                    ? wowMoment.FormattedValues[WowMomentFields.Approval] : "not set";
                throw new InvalidPluginExecutionException(string.Format(
                    "Feedback can only be created for an Approved Wow Moment. The current Management Approval is '{0}'.",
                    currentLabel));
            }

            // ---------- Validation 3: no feedback may exist yet for this Wow Moment ----------
            var duplicate = new QueryExpression(FeedbackFields.LogicalName)
            {
                ColumnSet = new ColumnSet(FeedbackFields.Id),
                TopCount = 1
            };
            duplicate.Criteria.AddCondition(FeedbackFields.WowMoment, ConditionOperator.Equal, wowMomentId);
            EntityCollection existing = service.RetrieveMultiple(duplicate);

            if (existing.Entities.Count > 0)
            {
                throw new InvalidPluginExecutionException(string.Format(
                    "Feedback already exists for this Wow Moment (Feedback Id {0}). Only one feedback record is allowed.",
                    existing.Entities[0].Id));
            }

            // ---------- Validation 4: the rating must be in the supported range ----------
            if (!rating.HasValue || rating.Value < MinRating || rating.Value > MaxRating)
            {
                throw new InvalidPluginExecutionException(string.Format(
                    "Rating must be a whole number between {0} and {1}. You entered {2}.",
                    MinRating, MaxRating, rating.HasValue ? rating.Value.ToString() : "no value"));
            }

            if (comments != null && comments.Length > MaxCommentsLength)
            {
                throw new InvalidPluginExecutionException(string.Format(
                    "Comments can be at most {0} characters. You entered {1}.", MaxCommentsLength, comments.Length));
            }

            // ---------- Create the Event Feedback ----------
            // The choice numbers are looked up by label ("4 Star", "Submitted"), never hardcoded
            int ratingOption = OptionHelper.GetOptionValue(service,
                FeedbackFields.LogicalName, FeedbackFields.Rating, rating.Value + " Star");
            int submittedOption = OptionHelper.GetOptionValue(service,
                FeedbackFields.LogicalName, FeedbackFields.Status, "Submitted");

            var feedback = new Entity(FeedbackFields.LogicalName);
            feedback[FeedbackFields.Title] = Truncate("Feedback - " + wowTitle, MaxTitleLength);
            feedback[FeedbackFields.WowMoment] = new EntityReference(WowMomentFields.LogicalName, wowMomentId);
            feedback[FeedbackFields.Rating] = new OptionSetValue(ratingOption);
            feedback[FeedbackFields.Status] = new OptionSetValue(submittedOption);   // Submitted
            feedback[FeedbackFields.FeedbackDate] = DateTime.UtcNow;                 // current date
            if (!string.IsNullOrWhiteSpace(comments))
            {
                feedback[FeedbackFields.Comments] = comments;
            }

            // The guest becomes "Submitted By" when the Wow Moment has a contact
            EntityReference contact = wowMoment.GetAttributeValue<EntityReference>(WowMomentFields.Contact);
            if (contact != null)
            {
                feedback[FeedbackFields.SubmittedBy] = contact;
            }

            Guid feedbackId = service.Create(feedback);
            tracing.Trace("Event Feedback {0} created for Wow Moment {1}.", feedbackId, wowMomentId);

            // ---------- Output parameters ----------
            context.OutputParameters["FeedbackId"] = new EntityReference(FeedbackFields.LogicalName, feedbackId);
            context.OutputParameters["Message"] = "Feedback created successfully.";
        }

        /// <summary>
        /// A bound action already knows its record (Target). The WowMomentId parameter is
        /// required by the specification, so we read it and make sure both agree.
        /// </summary>
        private static Guid ReadWowMomentId(IPluginExecutionContext context)
        {
            EntityReference fromParameter = ReadInput<EntityReference>(context, "WowMomentId");
            EntityReference target = ReadInput<EntityReference>(context, "Target");

            if (fromParameter == null && target == null)
            {
                throw new InvalidPluginExecutionException("WowMomentId is required.");
            }

            if (fromParameter != null && fromParameter.LogicalName != WowMomentFields.LogicalName)
            {
                throw new InvalidPluginExecutionException(
                    "WowMomentId must refer to a Wow Moment record.");
            }

            if (fromParameter != null && target != null && target.Id != fromParameter.Id)
            {
                throw new InvalidPluginExecutionException(
                    "WowMomentId does not match the Wow Moment the action was called on.");
            }

            return fromParameter != null ? fromParameter.Id : target.Id;
        }

        /// <summary>Reads an input parameter, or returns the default when it was not sent.</summary>
        private static T ReadInput<T>(IPluginExecutionContext context, string name)
        {
            if (context.InputParameters.Contains(name) && context.InputParameters[name] is T)
            {
                return (T)context.InputParameters[name];
            }
            return default(T);
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) { return text; }
            return text.Substring(0, maxLength);
        }
    }
}