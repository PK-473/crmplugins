namespace WowMoment.Plugins.Constants
{
    // Names verified in the Story Management handoff.
    public static class StoryMetricsFields
    {
        public const string StoryLogicalName = "sm_story";
        public const string StoryPrimaryKey = "sm_storyid";

        public const string ItemLogicalName = "sm_storyitem";
        public const string ItemStory = "sm_story";
        public const string ItemHoursRequired = "sm_hoursrequired";
        public const string ItemCurrentStatus = "sm_currentstatus";

        public const string OpenLabel = "Open";
        public const string InProgressLabel = "In-Progress";
        public const string CompletedLabel = "Completed";
    }
}