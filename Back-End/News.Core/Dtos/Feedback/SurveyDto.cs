namespace News.Core.Dtos.Feedback;

public class SurveyDto
{
    public string? SourceDiscovery { get; set; } // For "How did you find out about our news website?"
    public string? VisitFrequency { get; set; }  // For "How often do you visit news websites?"
    public string? IsLoadingSpeedSatisfactory { get; set; } // For "Is the website loading speed satisfactory?"
    public string? NavigationEaseRating { get; set; } // For "How easy is it to navigate our website?" 

}