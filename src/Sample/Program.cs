using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning;

namespace AssemblyAI.SemanticKernel.Sample;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var config = BuildConfig(args);

        var kernel = BuildKernel(config);

        await TranscribeFileUsingPluginDirectly(kernel);
        //await TranscribeFileUsingPluginFromSemanticFunction(kernel);
        //await TranscribeFileUsingPlan(kernel);
    }

    private static IKernel BuildKernel(IConfiguration config)
    {
        var loggerFactory = LoggerFactory.Create(builder => { builder.SetMinimumLevel(0); });
        var kernel = new KernelBuilder()
            .WithOpenAIChatCompletionService(
                "gpt-3.5-turbo",
                config["OpenAI:ApiKey"] ?? throw new Exception("OpenAI:ApiKey configuration is required.")
            )
            .WithLoggerFactory(loggerFactory)
            .Build();

        var apiKey = config["AssemblyAI:ApiKey"] ?? throw new Exception("AssemblyAI:ApiKey configuration is required.");

        kernel.ImportSkill(
            new TranscriptPlugin(apiKey: apiKey)
            {
                AllowFileSystemAccess = true
            },
            TranscriptPlugin.PluginName
        );

        kernel.ImportSkill(
            new FindFilePlugin(kernel: kernel),
            FindFilePlugin.PluginName
        );
        return kernel;
    }

    private static IConfigurationRoot BuildConfig(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>()
            .AddCommandLine(args)
            .Build();
        return config;
    }

    private static async Task TranscribeFileUsingPluginDirectly(IKernel kernel)
    {
        Console.WriteLine("Transcribing file using plugin directly");
        var variables = new ContextVariables
        {
            ["audioUrl"] = "https://storage.googleapis.com/aai-docs-samples/espn.m4a",
            // ["filePath"] = "./espn.m4a" // you can also use `filePath` which will upload the file and override `audioUrl`
        };

        var result = await kernel.Skills
            .GetFunction(TranscriptPlugin.PluginName, TranscriptPlugin.TranscribeFunctionName)
            .InvokeAsync(variables);

        Console.WriteLine(result.Result);
        Console.WriteLine();
    }

    private static async Task TranscribeFileUsingPluginFromSemanticFunction(IKernel kernel)
    {
        Console.WriteLine("Transcribing file and summarizing from within a semantic function");
        // This will pass the URL to the `INPUT` variable.
        // If `INPUT` is a URL, it'll use `INPUT` as `audioUrl`, otherwise, it'll use `INPUT` as `filePath`.
        const string prompt = """
                              Here is a transcript:
                              {{TranscriptPlugin.Transcribe "https://storage.googleapis.com/aai-docs-samples/espn.m4a"}}
                              ---
                              Summarize the transcript.
                              """;
        var context = kernel.CreateNewContext();
        var function = kernel.CreateSemanticFunction(prompt);
        await function.InvokeAsync(context);
        Console.WriteLine(context.Result);
        Console.WriteLine();
    }

    private static async Task TranscribeFileUsingPlan(IKernel kernel)
    {
        Console.WriteLine("Transcribing file from a plan");
        var planner = new SequentialPlanner(kernel);

        const string prompt = "Transcribe the espn.m4a in my downloads folder.";
        var plan = await planner.CreatePlanAsync(prompt);

        Console.WriteLine("Plan:\n");
        Console.WriteLine(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));

        var transcript = (await kernel.RunAsync(plan)).Result;
        Console.WriteLine(transcript);
        Console.WriteLine();
    }
}