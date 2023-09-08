using System.ComponentModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace AssemblyAiSk.Plugins;

public class TranscriptPlugin
{
    private readonly string _apiKey;
    public bool AllowFileSystemAccess { get; set; }

    public TranscriptPlugin(string apiKey)
    {
        _apiKey = apiKey;
    }

    [SKFunction]
    [Description("Upload audio or video file to AssemblyAI so it can be transcribed and return the URL of the file.")]
    [SKParameter("path", "The path of the audio or video file")]
    public async Task<string> Upload(SKContext context)
    {
        var path = context.Variables["path"];
        return await UploadFileAsync(path).ConfigureAwait(false);
    }

    private async Task<string> UploadFileAsync(string path)
    {
        if (AllowFileSystemAccess == false)
        {
            throw new Exception(
                "You need to allow file system access to upload files. Set TranscriptPlugin.AllowFileSystemAccess to true.");
        }

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
    [SKParameter("audioUrl",
        "The URL of the audio or video file. Optional if createTranscriptParameters is configured.")]
    [SKParameter("createTranscriptParameters",
        "The parameters to create an AssemblyAI transcript as a JSON object. Optional if audioUrl is configured.")]
    public async Task<string> Transcribe(SKContext context)
    {
        context.Variables.TryGetValue("audioUrl", out var audioUrl);
        context.Variables.TryGetValue("createTranscriptParameters", out var createTranscriptParameters);
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(_apiKey);
        var transcript = await CreateTranscriptAsync(audioUrl, createTranscriptParameters, httpClient);
        transcript = await WaitForTranscriptToProcess(transcript, httpClient);
        return transcript.Text;
    }

    private static async Task<Transcript> CreateTranscriptAsync(
        string? audioUrl,
        string? createTranscriptParameters,
        HttpClient httpClient
    )
    {
        string jsonString;
        if (!string.IsNullOrEmpty(createTranscriptParameters))
        {
            var jsonNode = JsonNode.Parse(createTranscriptParameters)!;
            if (!string.IsNullOrEmpty(audioUrl))
            {
                jsonNode["audio_url"] = audioUrl;
            }

            jsonString = jsonNode.ToJsonString();
        }
        else if (!string.IsNullOrEmpty(audioUrl))
        {
            var json = new JsonObject
            {
                ["audio_url"] = audioUrl
            };
            jsonString = json.ToJsonString();
        }
        else
        {
            throw new Exception("audioUrl or createTranscriptParameters has to be passed into the function.");
        }

        var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
        using var response = await httpClient.PostAsync("https://api.assemblyai.com/v2/transcript", content);
        response.EnsureSuccessStatusCode();
        var transcript = (await response.Content.ReadFromJsonAsync<Transcript>())!;
        if (transcript.Status == "error") throw new Exception(transcript.Error);
        return transcript;
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
                    throw new Exception(transcript.Error);
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