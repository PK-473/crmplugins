namespace StoryManagement.Plugins.Base
{
    /// <summary>
    /// Logical names of the Story table and its columns.
    /// Keeping them in one place means a wrong name is fixed once, not in every plugin.
    /// </summary>
    internal static class StoryFields
    {
        public const string LogicalName = "sm_story";
        public const string Id = "sm_storyid";
        public const string Name = "sm_storyname";                          // primary column (Story Name)
        public const string EstHours = "sm_esthoursrequired";          // Est. Hours Required
        public const string ActualHours = "sm_actualhoursspent";       // Actual Hours Spent
        public const string DaysRequired = "sm_noofdaysrequired";      // No of Days Required
        public const string OpenItems = "sm_openstoryitems";           // Open Story Items
        public const string InProgressItems = "sm_inprogressstoryitems";
        public const string CompletedItems = "sm_completedstoryitems";
        public const string CurrentStatus = "sm_currentstatus";
    }
}