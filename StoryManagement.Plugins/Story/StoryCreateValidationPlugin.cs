using System;
using StoryManagement.Plugins.Base;
using StoryManagement.Plugins.Helpers;
using System.Globalization;
using Microsoft.Xrm.Sdk;

namespace StoryManagement.Plugins.Story
{
    /// <summary>
    /// Task 1: Validate and initialize a Story during Create.
    /// Registered on: sm_story | Create | Pre-operation (20) | Synchronous.
    /// </summary>
    public class StoryCreateValidationPlugin : PluginBase
    {
        private const int MinDays = 1;
        private const int MaxDays = 90;

        protected override void Run(IPluginExecutionContext context, IOrganizationService service, ITracingService tracing)
        {
            // Target = the record that is about to be created
            var target = context.InputParameters["Target"] as Entity;
            if (target == null || target.LogicalName != StoryFields.LogicalName)
            {
                tracing.Trace("Target is not a Story. Nothing to do.");
                return;
            }

            // Read the values the user entered (null when the column was not sent)
            string name = target.GetAttributeValue<string>(StoryFields.Name);
            decimal? estHours = target.GetAttributeValue<decimal?>(StoryFields.EstHours);
            int? days = target.GetAttributeValue<int?>(StoryFields.DaysRequired);

            // ---------- 1. Validation ----------
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidPluginExecutionException(
                    "Story Name is required. Please enter a name for the story.");
            }
            name = name.Trim();

            if (!estHours.HasValue || estHours.Value <= 0)
            {
                throw new InvalidPluginExecutionException(
                    "Est. Hours Required must be greater than 0. Please enter the estimated hours for this story.");
            }

            if (!days.HasValue || days.Value < MinDays || days.Value > MaxDays)
            {
                throw new InvalidPluginExecutionException(
                    string.Format("No of Days Required must be a whole number between {0} and {1}. Please correct the number of days.",
                        MinDays, MaxDays));
            }

            // ---------- 2. Duplicate check (FetchXML) ----------
            if (FetchXmlHelper.StoryExists(service, tracing, name, estHours.Value, days.Value))
            {
                throw new InvalidPluginExecutionException(
                    string.Format(CultureInfo.InvariantCulture,
                        "A story named '{0}' with Est. Hours Required {1} and No of Days Required {2} already exists. " +
                        "Change the story name, the hours, or the days to create a new story.",
                        name, estHours.Value, days.Value));
            }

            // ---------- 3. Initialize values ----------
            // We are in Pre-operation, so changing Target is enough:
            // the platform saves these values with the record. No Update call is needed.
            target[StoryFields.Name] = name;
            target[StoryFields.OpenItems] = days.Value;
            target[StoryFields.InProgressItems] = 0;
            target[StoryFields.CompletedItems] = 0;
            target[StoryFields.ActualHours] = 0m;

            tracing.Trace("Story validated and initialized. Open items set to {0}.", days.Value);
        }
    }
}
