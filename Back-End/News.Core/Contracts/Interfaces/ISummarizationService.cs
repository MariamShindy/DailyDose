namespace News.Core.Contracts.Interfaces
{
	public interface ISummarizationService
	{
		Task<string> SummarizeTextAsync(string text);
	}
}
