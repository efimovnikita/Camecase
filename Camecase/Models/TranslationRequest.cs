namespace Camecase.Models
{
    public class TranslationRequest
    {
        public string folderId { get; set; }
        public string texts { get; set; }
        public string targetLanguageCode { get; set; }
    }
}