namespace WowMoment.Plugins.Constants
{
    // VERIFY every name marked VERIFY against your table.
    public static class EventFeedbackFields
    {
        public const string LogicalName = "pkk_eventfeedback";
        public const string Name = "pkk_FeedbackTitle";           
        public const string WowMoment = "pkk_wowmoment";        
        public const string Rating = "pkk_customerrating";            
        public const string Comments = "pkk_feedbackcomments";         
        public const string FeedbackDate = "pkk_feedbackdate";  
        public const string FeedbackStatus = "pkk_eventstatus"; 
        public const string SubmittedLabel = "Submitted";

        // Use the same range your Event Feedback JavaScript enforces.
        public const int MinRating = 1;
        public const int MaxRating = 5;
    }
}