namespace News.Core.Dtos.NewsCatcher
{
    public class TranslationRequest
    {
        public string Text { get; set; }
        public string SourceLang { get; set; }
        public string TargetLang { get; set; }
    }
}
