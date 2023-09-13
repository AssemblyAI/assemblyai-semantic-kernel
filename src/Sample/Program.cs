using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning;
using AssemblyAI.SemanticKernel;
using AssemblyAI.SemanticKernel.Sample;
using Microsoft.Extensions.Logging;

var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .AddCommandLine(args)
    .Build();

using var loggerFactory = LoggerFactory.Create(builder => { builder.SetMinimumLevel(0); });
var kernel = new KernelBuilder()
    .WithCompletionService(config)
    .WithLoggerFactory(loggerFactory)
    .Build();

var apiKey = config["AssemblyAI:ApiKey"] ?? throw new Exception("AssemblyAI:ApiKey not configured.");

var transcriptPlugin = kernel.ImportSkill(
    new TranscriptPlugin(apiKey: apiKey)
    {
        AllowFileSystemAccess = true
    },
    "TranscriptPlugin"
);

await TranscribeFileUsingPlugin(kernel);

async Task TranscribeFileUsingPlugin(IKernel kernel)
{
    var variables = new ContextVariables
    {
        ["audioUrl"] = "https://storage.googleapis.com/aai-docs-samples/espn.m4a",
    };

    var result = await kernel.Skills
        .GetFunction("TranscriptPlugin", "Transcribe")
        .InvokeAsync(variables);
    Console.WriteLine(result.Result);
}

var findFilePlugin = kernel.ImportSkill(
    new FindFilePlugin(kernel: kernel),
    "FindFilePlugin"
);

await TranscribeFileUsingPlan(kernel);

async Task TranscribeFileUsingPlan(IKernel kernel)
{
    var planner = new SequentialPlanner(kernel);

    const string prompt = "Transcribe the espn.m4a in my downloads folder.";
    var plan = await planner.CreatePlanAsync(prompt);

    Console.WriteLine("Plan:\n");
    Console.WriteLine(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));

    var transcript = (await kernel.RunAsync(plan)).Result;
    Console.WriteLine("Transcript:");
    Console.WriteLine(transcript);
}