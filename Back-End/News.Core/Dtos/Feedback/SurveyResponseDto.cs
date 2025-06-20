namespace News.Core.Dtos.Feedback
{
    public class SurveyResponseDto
    {
        public string? SourceDiscovery { get; set; }
        public string? VisitFrequency { get; set; }
        public string? IsLoadingSpeedSatisfactory { get; set; }
        public string? NavigationEaseRating { get; set; }

        public string? ApplicationUserId { get; set; }
        public string? ApplicationUserName { get; set; }
        public string? ApplicationUserEmail { get; set; }
    }
}
