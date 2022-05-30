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
using Avalonia.Controls;
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
        private bool _showProgressBar;
        private string _status = "Ready";
        private Token? _token;

        public MainWindowViewModel()
        {
            TranslateCommand = ReactiveCommand.CreateFromTask(Translate);
            ClearAllCommand = ReactiveCommand.Create(ClearAll);
            CopyCommand = ReactiveCommand.CreateFromTask<string>(Copy);
            ExitCommand = ReactiveCommand.Create<Window>(Exit);
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

        public bool ShowProgressBar
        {
            get => _showProgressBar;
            set => this.RaiseAndSetIfChanged(ref _showProgressBar, value);
        }

        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public ReactiveCommand<Unit, Unit> TranslateCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ClearAllCommand { get; set; }
        public ReactiveCommand<string, Unit> CopyCommand { get; set; }
        public ReactiveCommand<Window, Unit> ExitCommand { get; set; }

        private async Task Translate()
        {
            if (String.IsNullOrEmpty(InputPhrase))
            {
                return;
            }
            
            ShowProgressBar = true;
            Token? token = await GetTokenFromCloud();

            if (token is null)
            {
                Status = "Failed to get a token";
                ShowProgressBar = false;
                return;
            }
            
            string translatedText = await GetTranslationFromCloud(token.iamtoken);

            if (String.IsNullOrEmpty(translatedText))
            {
                Status = "Couldn't get a text translation";
                ShowProgressBar = false;
                return;
            }
            
            TranslatedResult = translatedText;
            Status = "Success";
            ShowProgressBar = false;
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

        private async Task<Token?> GetTokenFromCloud()
        {
            try
            {
                if (_token is not null && _token.expiresAt > DateTime.Now)
                {
                    return _token;
                }

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
                    return null;
                }

                string stringContent = await response.Content.ReadAsStringAsync();

                _token = JsonConvert.DeserializeObject<Token>(stringContent);
                return _token;
            }
            catch
            {
                return null;
            }
        }

        private void ClearAll()
        {
            InputPhrase = TranslatedResult = CamelForFuncResult = CamelForVarResult = "";
            Status = "Ready";
            ShowProgressBar = false;
        }

        private static async Task Copy(string parameter)
        {
            await Application.Current.Clipboard.SetTextAsync(parameter);
        }

        private static void Exit(Window window)
        {
            window.Close();
        }
    }
}