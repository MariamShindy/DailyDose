namespace News.Core.Contracts.Interfaces
{
    public interface ITranslationService
    {
		Task<TranslationResponse> TranslateTextAsync(TranslationRequest request);

	}
}
