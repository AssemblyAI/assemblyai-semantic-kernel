using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AssemblyAI.Transcripts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace AssemblyAI.SemanticKernel
{
    public class AssemblyAIPlugin
    {
        internal AssemblyAIPluginOptions Options { get; }

        private string ApiKey => Options.ApiKey;

        private bool AllowFileSystemAccess => Options.AllowFileSystemAccess;

        public AssemblyAIPlugin(string apiKey)
        {
            Options = new AssemblyAIPluginOptions
            {
                ApiKey = apiKey
            };
        }

        public AssemblyAIPlugin(string apiKey, bool allowFileSystemAccess)
        {
            Options = new AssemblyAIPluginOptions
            {
                ApiKey = apiKey,
                AllowFileSystemAccess = allowFileSystemAccess
            };
        }

        [ActivatorUtilitiesConstructor]
        public AssemblyAIPlugin(IOptions<AssemblyAIPluginOptions> options)
        {
            Options = options.Value;
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
                var client = new AssemblyAIClient(new ClientOptions
                {
                    ApiKey = ApiKey,
                    HttpClient = httpClient,
                    UserAgent = new UserAgent
                    {
                        ["integration"] = new UserAgentItem(
                            "AssemblyAI.SemanticKernel", 
                            typeof(AssemblyAIPlugin).Assembly.GetName().Version.ToString()
                        )
                    }
                });
                
                string audioUrl;
                if (TryGetPath(input, out var filePath))
                {
                    if (AllowFileSystemAccess == false)
                    {
                        throw new Exception(
                            "You need to allow file system access to upload files. Set AssemblyAI:Plugin:AllowFileSystemAccess to true."
                        );
                    }

                    audioUrl = await UploadFileAsync(filePath, client);
                }
                else
                {
                    audioUrl = input;
                }

                var transcript = await TranscribeAsync(audioUrl, client);
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

        private static async Task<string> UploadFileAsync(string path, AssemblyAIClient client)
        {
            using (var fileStream = File.OpenRead(path))
            {
                var response = await client.Files.UploadAsync(fileStream).ConfigureAwait(false);
                return response.UploadUrl;
            }
        }

        private static async Task<Transcript> TranscribeAsync(string audioUrl, AssemblyAIClient client)
        {
            var transcriptParams = new TranscriptParams
            {
                AudioUrl = audioUrl
            };

            var transcript = await client.Transcripts.TranscribeAsync(transcriptParams).ConfigureAwait(false);
            transcript.EnsureStatusCompleted();
            return transcript;
        } 
    }
}