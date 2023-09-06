using System.ComponentModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.SkillDefinition;

namespace AssemblyAiSk.Plugins;

public class TranscriptPlugin
{
    private readonly string _apiKey;
    private readonly IKernel _kernel;
    private readonly ISKFunction _getCommonFolderPathFunction;

    public TranscriptPlugin(string apiKey, IKernel kernel)
    {
        _apiKey = apiKey;
        _kernel = kernel;
        _getCommonFolderPathFunction = GetCommonFolderPathFunction();
    }

    private ISKFunction GetCommonFolderPathFunction()
    {
        var prompt = "What is the path for the common folder  {{$commonFolderName}} " +
                     $"on operating platform {Environment.OSVersion.Platform.ToString()} " +
                     $"with user profile path {Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}?";
        var promptConfig = new PromptTemplateConfig
        {
            Schema = 1,
            Type = "completion",
            Description = "Gets the path for the given common folder.",
            Completion =
            {
                MaxTokens = 500,
                Temperature = 0.0,
                TopP = 0.0,
                PresencePenalty = 0.0,
                FrequencyPenalty = 0.0
            },
            Input =
            {
                Parameters = new List<PromptTemplateConfig.InputParameter>
                {
                    new()
                    {
                        Name = "commonFolderName",
                        Description = "Name of the common folder"
                    }
                }
            }
        };

        var promptTemplate = new PromptTemplate(prompt, promptConfig, _kernel);
        var functionConfig = new SemanticFunctionConfig(promptConfig, promptTemplate);
        return _kernel.RegisterSemanticFunction("TranscriptPluginInternal", "GetCommonFolderPath", functionConfig);
    }

    private async Task<string?> GetCommonFolderPath(string commonFolderName)
    {
        var variables = new ContextVariables
        {
            ["commonFolderName"] = commonFolderName
        };
        var context = await _getCommonFolderPathFunction.InvokeAsync(variables);
        var match = Regex.Match(context.Result, @"([a-zA-Z]?\:?[\/\\].*[\/\\][\S-[""'\.]]*)", RegexOptions.IgnoreCase);
        return match.Success ? match.Captures[0].Value : null;
    }

    [SKFunction, Description("Find files in common folders.")]
    [SKParameter("fileName", "The name of the file")]
    [SKParameter("commonFolderName", "The name of the common folder")]
    public async Task<string> LocateFile(SKContext context)
    {
        var fileName = context.Variables["fileName"];
        var commonFolderName = context.Variables["commonFolderName"];
        var commonFolderPath = commonFolderName?.ToLower() switch
        {
            null => Environment.CurrentDirectory,
            "" => Environment.CurrentDirectory,
            "." => Environment.CurrentDirectory,
            "desktop" => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "videos" => Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            "music" => Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
            "pictures" => Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "documents" => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "user" => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            _ => await GetCommonFolderPath(commonFolderName)
                 ?? throw new Exception("Could not figure out the location of the common folder.")
        };

        if (!Path.Exists(commonFolderPath))
        {
            throw new Exception(
                $"Could not find {commonFolderName} folder, tried the following path: {commonFolderPath}");
        }

        var foundFiles = Directory.GetFiles(commonFolderPath, fileName, SearchOption.AllDirectories);
        if (foundFiles.Length == 0)
        {
            throw new Exception($"Could not find file named {fileName} in {commonFolderPath}.");
        }

        return foundFiles.First();
    }

    [SKFunction]
    [Description("Upload audio or video file to AssemblyAI so it can be transcribed and return the URL of the file.")]
    [SKParameter("path", "The path of the audio or video file")]
    public async Task<string> Upload(SKContext context)
    {
        var path = context.Variables["path"];
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(_apiKey);
        await using var fileStream = File.OpenRead(path);
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var response = await httpClient.PostAsync("https://api.assemblyai.com/v2/upload", fileContent);
        response.EnsureSuccessStatusCode();
        var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return jsonDoc?.RootElement.GetProperty("upload_url").GetString()!;
    }

    [SKFunction, Description("Transcribe an audio or video file to text.")]
    [SKParameter("audioUrl", "The URL of the audio or video file")]
    public async Task<string> Transcribe(SKContext context)
    {
        var audioUrl = context.Variables["audioUrl"];
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(_apiKey);
        var transcript = await CreateTranscriptAsync(audioUrl, httpClient);
        transcript = await WaitForTranscriptToProcess(transcript, httpClient);
        return transcript.Text;
    }

    private static async Task<Transcript> CreateTranscriptAsync(string audioUrl, HttpClient httpClient)
    {
        var data = new { audio_url = audioUrl };
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

        using var response = await httpClient.PostAsync("https://api.assemblyai.com/v2/transcript", content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Transcript>())!;
    }

    private static async Task<Transcript> WaitForTranscriptToProcess(Transcript transcript, HttpClient httpClient)
    {
        var pollingEndpoint = $"https://api.assemblyai.com/v2/transcript/{transcript.Id}";

        while (true)
        {
            var pollingResponse = await httpClient.GetAsync(pollingEndpoint);
            pollingResponse.EnsureSuccessStatusCode();
            transcript = (await pollingResponse.Content.ReadFromJsonAsync<Transcript>())!;
            switch (transcript.Status)
            {
                case "processing":
                case "queued":
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    break;
                case "completed":
                case "error":
                    return transcript;
                default:
                    throw new Exception("This code shouldn't be reachable.");
            }
        }
    }
}

public class Transcript
{
    public string Id { get; set; }
    public string Status { get; set; }
    public string Text { get; set; }

    public string Error { get; set; }
}