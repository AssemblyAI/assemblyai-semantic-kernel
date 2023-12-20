using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace AssemblyAI.SemanticKernel
{
    public class TranscriptPlugin
    {
        public const string PluginName = nameof(TranscriptPlugin);
        private readonly string _apiKey;
        public bool AllowFileSystemAccess { get; set; }

        public TranscriptPlugin(string apiKey)
        {
            _apiKey = apiKey;
        }

        public const string TranscribeFunctionName = nameof(Transcribe);

        [KernelFunction, Description("Transcribe an audio or video file to text.")]
        public async Task<string> Transcribe(
            [Description("The public URL or the local path of the audio or video file to transcribe.")]
            string input
        )
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new Exception("The INPUT parameter is required.");
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_apiKey);
                string audioUrl;
                if (TryGetPath(input, out var filePath))
                {
                    if (AllowFileSystemAccess == false)
                    {
                        throw new Exception(
                            "You need to allow file system access to upload files. Set TranscriptPlugin.AllowFileSystemAccess to true."
                        );
                    }

                    audioUrl = await UploadFileAsync(filePath, httpClient);
                }
                else
                {
                    audioUrl = input;
                }

                var transcript = await CreateTranscriptAsync(audioUrl, httpClient);
                transcript = await WaitForTranscriptToProcess(transcript, httpClient);
                return transcript.Text ?? throw new Exception("Transcript text is null. This should not happen.");
            }
        }

        private static bool TryGetPath(string input, out string filePath)
        {
            if (Uri.TryCreate(input, UriKind.Absolute, out var inputUrl))
            {
                if (inputUrl.IsFile)
                {
                    filePath = inputUrl.LocalPath;
                    return true;
                }

                filePath = null;
                return false;
            }

            filePath = input;
            return true;
        }

        private static async Task<string> UploadFileAsync(string path, HttpClient httpClient)
        {
            using (var fileStream = File.OpenRead(path))
            using (var fileContent = new StreamContent(fileStream))
            {
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                using (var response = await httpClient.PostAsync("https://api.assemblyai.com/v2/upload", fileContent))
                {
                    response.EnsureSuccessStatusCode();
                    var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
                    return jsonDoc?.RootElement.GetProperty("upload_url").GetString();
                }
            }
        }

        private static async Task<Transcript> CreateTranscriptAsync(string audioUrl, HttpClient httpClient)
        {
            var jsonString = JsonSerializer.Serialize(new
            {
                audio_url = audioUrl
            });

            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            using (var response = await httpClient.PostAsync("https://api.assemblyai.com/v2/transcript", content))
            {
                response.EnsureSuccessStatusCode();
                var transcript = await response.Content.ReadFromJsonAsync<Transcript>();
                if (transcript.Status == "error") throw new Exception(transcript.Error);
                return transcript;
            }
        }

        private static async Task<Transcript> WaitForTranscriptToProcess(Transcript transcript, HttpClient httpClient)
        {
            var pollingEndpoint = $"https://api.assemblyai.com/v2/transcript/{transcript.Id}";

            while (true)
            {
                var pollingResponse = await httpClient.GetAsync(pollingEndpoint);
                pollingResponse.EnsureSuccessStatusCode();
                transcript = (await pollingResponse.Content.ReadFromJsonAsync<Transcript>());
                switch (transcript.Status)
                {
                    case "processing":
                    case "queued":
                        await Task.Delay(TimeSpan.FromSeconds(3));
                        break;
                    case "completed":
                        return transcript;
                    case "error":
                        throw new Exception(transcript.Error);
                    default:
                        throw new Exception("This code shouldn't be reachable.");
                }
            }
        }
    }

    public class Transcript
    {
        public string Id { get; set; } = null;
        public string Status { get; set; } = null;
        public string Text { get; set; }

        public string Error { get; set; }
    }
}