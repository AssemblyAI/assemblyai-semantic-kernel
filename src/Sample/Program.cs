using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planners;

namespace AssemblyAI.SemanticKernel.Sample;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var config = BuildConfig(args);

        var kernel = BuildKernel(config);

        await TranscribeFileUsingPluginDirectly(kernel);
        await TranscribeFileUsingPluginFromSemanticFunction(kernel);
        await TranscribeFileUsingPlan(kernel);
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

        kernel.ImportFunctions(
            new TranscriptPlugin(apiKey: apiKey)
            {
                AllowFileSystemAccess = true
            },
            TranscriptPlugin.PluginName
        );

        kernel.ImportFunctions(
            new FindFilePlugin(kernel),
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
        var context = kernel.CreateNewContext();
        context.Variables["INPUT"] = "https://storage.googleapis.com/aai-docs-samples/espn.m4a";
        var result = await kernel.Functions
            .GetFunction(TranscriptPlugin.PluginName, TranscriptPlugin.TranscribeFunctionName)
            .InvokeAsync(context);

        Console.WriteLine(result.GetValue<string>());
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
        var result = await function.InvokeAsync(context);
        Console.WriteLine(result.GetValue<string>());
        Console.WriteLine();
    }

    private static async Task TranscribeFileUsingPlan(IKernel kernel)
    {
        Console.WriteLine("Transcribing file from a plan");
        var planner = new SequentialPlanner(kernel);
        const string prompt = "Find the espn.m4a in my downloads folder and transcribe it.";
        var plan = await planner.CreatePlanAsync(prompt);

        Console.WriteLine("Plan:\n");
        Console.WriteLine(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));

        var transcript = (await kernel.RunAsync(plan)).GetValue<string>();
        Console.WriteLine(transcript);
        Console.WriteLine();
    }
}