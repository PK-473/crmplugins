using System.Globalization;
using Microsoft.Xrm.Sdk;
using StoryManagement.Plugins.Base;
using StoryManagement.Plugins.Helpers;

namespace StoryManagement.Plugins.Story
{
    /// <summary>
    /// Task 5: validate changes to Est. Hours Required and No of Days Required,
    /// comparing the old values (Pre Image) with the new ones.
    /// Registered on: sm_story | Update | Pre-operation (20) | Synchronous
    /// Filtering attributes: sm_esthoursrequired, sm_noofdaysrequired, sm_currentstatus
    /// Pre Image: StoryPreImage (Est. Hours Required, No of Days Required, Current Status)
    /// </summary>
    public class StoryUpdateValidationPlugin : PluginBase
    {
        private const int MinDays = 1;
        private const int MaxDays = 90;

        protected override void Run(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            var target = context.InputParameters["Target"] as Entity;
            if (target == null || target.LogicalName != StoryFields.LogicalName)
            {
                tracing.Trace("Target is not a Story. Nothing to do.");
                return;
            }

            // Pre Image = the Story as it was BEFORE this update
            Entity pre = context.PreEntityImages.Contains(ImageNames.StoryPreImage)
                ? context.PreEntityImages[ImageNames.StoryPreImage] : null;
            if (pre == null)
            {
                tracing.Trace("No Pre Image found. Every column in the update is treated as changed.");
            }

            // ---------- Est. Hours Required ----------
            if (target.Contains(StoryFields.EstHours))
            {
                decimal? newHours = target.GetAttributeValue<decimal?>(StoryFields.EstHours);
                decimal? oldHours = pre == null ? (decimal?)null : pre.GetAttributeValue<decimal?>(StoryFields.EstHours);

                // Compare old and new first: run the validation only when the value really changed
                if (newHours != oldHours)
                {
                    if (!newHours.HasValue || newHours.Value <= 0)
                    {
                        throw new InvalidPluginExecutionException(string.Format(CultureInfo.InvariantCulture,
                            "Est. Hours Required must be greater than 0. You entered {0}; the current value ({1}) was not changed. " +
                            "Please enter a number of hours greater than 0.",
                            newHours.HasValue ? newHours.Value.ToString(CultureInfo.InvariantCulture) : "no value",
                            oldHours.HasValue ? oldHours.Value.ToString(CultureInfo.InvariantCulture) : "empty"));
                    }
                    tracing.Trace("Est. Hours changed from {0} to {1}.", oldHours, newHours);
                }
            }

            // ---------- No of Days Required ----------
            if (target.Contains(StoryFields.DaysRequired))
            {
                int? newDays = target.GetAttributeValue<int?>(StoryFields.DaysRequired);
                int? oldDays = pre == null ? (int?)null : pre.GetAttributeValue<int?>(StoryFields.DaysRequired);

                if (newDays != oldDays)
                {
                    if (!newDays.HasValue || newDays.Value < MinDays || newDays.Value > MaxDays)
                    {
                        throw new InvalidPluginExecutionException(string.Format(CultureInfo.InvariantCulture,
                            "No of Days Required must be a whole number between {0} and {1}. You entered {2}; the current value ({3}) was not changed.",
                            MinDays, MaxDays,
                            newDays.HasValue ? newDays.Value.ToString(CultureInfo.InvariantCulture) : "no value",
                            oldDays.HasValue ? oldDays.Value.ToString(CultureInfo.InvariantCulture) : "empty"));
                    }
                    tracing.Trace("No of Days changed from {0} to {1}.", oldDays, newDays);
                }
            }

            // ---------- Current Status (only when it actually changed) ----------
            if (target.Contains(StoryFields.CurrentStatus))
            {
                int? newStatus = StoryHelper.GetStatus(target, StoryFields.CurrentStatus);
                int? oldStatus = pre == null ? (int?)null : StoryHelper.GetStatus(pre, StoryFields.CurrentStatus);

                if (newStatus != oldStatus)
                {
                    if (!newStatus.HasValue)
                    {
                        throw new InvalidPluginExecutionException(
                            "Current Status cannot be empty. Please choose Open, In-Progress or Completed.");
                    }
                    tracing.Trace("Current Status changed from {0} to {1}.", oldStatus, newStatus);
                }
                else
                {
                    tracing.Trace("Current Status was sent but did not change. Status validation skipped.");
                }
            }
        }
    }
}
