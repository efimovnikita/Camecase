using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Camecase.Helpers;
using Camecase.Models;
using Newtonsoft.Json;
using ReactiveUI;

namespace Camecase.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string _inputPhrase = "";
        private string _translatedResult = "";
        private string _camelForVarResult = "";
        private string _camelForFuncResult = "";

        public MainWindowViewModel()
        {
            TranslateCommand = ReactiveCommand.CreateFromTask(Translate);
            ClearAllCommand = ReactiveCommand.Create(ClearAll);
            CopyCommand = ReactiveCommand.CreateFromTask<string>(Copy);
        }

        public string InputPhrase
        {
            get => _inputPhrase;
            set => this.RaiseAndSetIfChanged(ref _inputPhrase, value);
        }

        public string TranslatedResult
        {
            get => _translatedResult;
            set
            {
                this.RaiseAndSetIfChanged(ref _translatedResult, value);
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                string result = Regex.Replace(textInfo.ToTitleCase(value), @"\s+", "");
                CamelForFuncResult = result;
                CamelForVarResult = result.FirstCharToLowerCase()!;
            }
        }

        public string CamelForVarResult
        {
            get => _camelForVarResult;
            set => this.RaiseAndSetIfChanged(ref _camelForVarResult, value);
        }

        public string CamelForFuncResult
        {
            get => _camelForFuncResult;
            set => this.RaiseAndSetIfChanged(ref _camelForFuncResult, value);
        }

        public ReactiveCommand<Unit, Unit> TranslateCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ClearAllCommand { get; set; }

        public ReactiveCommand<string, Unit> CopyCommand { get; set; }

        private async Task Translate()
        {
            string token = await GetTokenFromCloud();

            if (String.IsNullOrEmpty(token))
            {
                return;
            }
            
            string translatedText = await GetTranslationFromCloud(token);

            if (String.IsNullOrEmpty(translatedText))
            {
                return;
            }
            
            TranslatedResult = translatedText;
        }

        private async Task<string> GetTranslationFromCloud(string token)
        {
            try
            {
                HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                TranslationRequest request = new()
                    {folderId = "b1get5pjvk3cj932g5bg", texts = InputPhrase, targetLanguageCode = "en"};

                string json = JsonConvert.SerializeObject(request,
                    Formatting.None, new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore});
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                StringContent content = new(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response =
                    await httpClient.PostAsync("https://translate.api.cloud.yandex.net/translate/v2/translate", content);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return "";
                }
            
                string responseContent = await response.Content.ReadAsStringAsync();

                TranslationResponse? translationResponse = JsonConvert.DeserializeObject<TranslationResponse>(responseContent);
                if (translationResponse != null && translationResponse.translations.Length > 0)
                {
                    return translationResponse.translations[0].text;
                }

                return "";
            }
            catch
            {
                return "";
            }
        }

        private static async Task<string> GetTokenFromCloud()
        {
            try
            {
                HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                TokenRequest request = new()
                    { yandexPassportOauthToken = "AQAAAABhuvQPAATuwZzaVNP-2UihpLgtXgeYGIE" };

                string json = JsonConvert.SerializeObject(request);

                StringContent content = new(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response =
                    await httpClient.PostAsync("https://iam.api.cloud.yandex.net/iam/v1/tokens", content);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return "";
                }

                string stringContent = await response.Content.ReadAsStringAsync();

                TokenResponse? tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(stringContent);
                return tokenResponse != null ? tokenResponse.iamtoken : "";
            }
            catch
            {
                return "";
            }
        }

        private void ClearAll()
        {
            InputPhrase = TranslatedResult = CamelForFuncResult = CamelForVarResult = "";
        }

        private static async Task Copy(string parameter)
        {
            await Application.Current.Clipboard.SetTextAsync(parameter);
        }
    }
}