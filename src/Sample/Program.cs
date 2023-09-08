using System.Text.Json;
using AssemblyAiSk.Plugins;
using AssemblyAiSk.Sample;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.SemanticFunctions;

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

var findFilePlugin = kernel.ImportSkill(
    new FindFilePlugin(kernel),
    "FindFilePlugin"
);
var transcriptPlugin = kernel.ImportSkill(
    new TranscriptPlugin(apiKey: config["AssemblyAI:ApiKey"]
                                 ?? throw new Exception("AssemblyAI:ApiKey not configured."))
    {
        AllowFileSystemAccess = true
    },
    "TranscriptPlugin"
);
var variables = new ContextVariables
{
    ["audioUrl"] = "https://storage.googleapis.com/aai-docs-samples/espn.m4a",
    ["createTranscriptParameters"] = """
{
    "language_code": "en_us"
}
"""
};

var result = await kernel.Skills
    .GetFunction("TranscriptPlugin", "Transcribe")
    .InvokeAsync(variables);
Console.WriteLine(result.Result);
return;
var planner = new SequentialPlanner(kernel);

const string prompt = "Transcribe the espn.m4a in my downloads folder.";
var plan = await planner.CreatePlanAsync(prompt);

Console.WriteLine("Plan:\n");
Console.WriteLine(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));

var transcript = (await kernel.RunAsync(plan)).Result;

Console.WriteLine("Transcript:");
Console.WriteLine(transcript.Trim());

var promptConfig = new PromptTemplateConfig
{
    Completion =
    {
        MaxTokens = 1000,
        Temperature = 0.2,
        TopP = 0.5,
    }
};

var qaPromptTemplate = new PromptTemplate(
    """
Here's a transcript:
---Begin Text---
{{$INPUT}}
---End Text---
Answer these questions about the transcript:
- Which quarterbacks were mentioned?
- Which teams were mentioned?
""",
    promptConfig,
    kernel
);

var qaFunctionConfig = new SemanticFunctionConfig(promptConfig, qaPromptTemplate);

var qaFunction = kernel.RegisterSemanticFunction(
    "AskQuestions",
    "AskQuestions",
    qaFunctionConfig
);

var myOutput = await kernel.RunAsync(transcript, qaFunction);

Console.WriteLine(myOutput);