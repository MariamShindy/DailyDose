namespace News.Core.Entities.NewsCatcher
{
	public class NewsArticle
    {
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Published_Date { get; set; }
        public string? Published_Date_Precision { get; set; }
        public string? Link { get; set; }
        public string? Domain_Url { get; set; }
        public string? Full_Domain_Url { get; set; }
        public NlpData? Nlp { get; set; } 
        public string? Description { get; set; }
        public string? Content { get; set; }
        public string? Rights { get; set; }
        public int Rank { get; set; }
        public string? Country { get; set; }
        public string? Language { get; set; }
		public List<string> Authors { get; set; } = new List<string>();
		public string? Media { get; set; }
        public bool Is_Opinion { get; set; }
        public string? Twitter_Account { get; set; }
        public string? Id { get; set; }
        public bool Is_Headline { get; set; }
        public int  Word_Count  { get; set; }
        public string? Topic => Nlp?.Theme ?? "Unknown topic";
    }
}
