namespace Camecase.Models
{
    public class TranslationResponse
    {
        public Translation[] translations { get; set; }
    }

    public class Translation
    {
        public string text { get; set; }
    }
}