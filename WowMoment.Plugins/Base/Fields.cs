namespace WowMoment.Plugins.Base
{
    /// <summary>Logical names of the Wow Moment table and its columns.</summary>
    internal static class WowMomentFields
    {
        public const string LogicalName = "pkk_wowmoment";
        public const string Id = "pkk_wowmomentid";
        public const string Title = "pkk_title";                    // primary column
        public const string Approval = "pkk_managementapproval";    // Management Approval (choice)
        public const string Contact = "pkk_contact";                // lookup to Contact
    }

    /// <summary>Logical names of the Event Feedback table and its columns.</summary>
    internal static class FeedbackFields
    {
        public const string LogicalName = "pkk_eventfeedback";
        public const string Id = "pkk_eventfeedbackid";
        public const string Title = "pkk_feedbacktitle";            // primary column
        public const string WowMoment = "pkk_wowmoment";            // lookup to Wow Moment
        public const string Rating = "pkk_customerrating";          // Customer Rating (choice: 1 Star ... 5 Star)
        public const string Comments = "pkk_feedbackcomments";
        public const string Status = "pkk_eventstatus";             // Event Status (choice: Pending, Submitted, Reviewed)
        public const string SubmittedBy = "pkk_submittedby";        // lookup to Contact
        public const string FeedbackDate = "pkk_feedbackdate";
    }
}