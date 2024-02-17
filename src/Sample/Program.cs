using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning.Handlebars;

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

    private static IConfigurationRoot BuildConfig(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>()
            .AddCommandLine(args)
            .Build();
        return config;
    }

    private static Kernel BuildKernel(IConfiguration config)
    {
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddOpenAIChatCompletion(
                "gpt-3.5-turbo",
                config["OpenAI:ApiKey"] ?? throw new Exception("OpenAI:ApiKey configuration is required.")
            )
            .AddAssemblyAIPlugin(config);
        var kernel = kernelBuilder.Build();

        kernel.ImportPluginFromType<FindFilePlugin>();
        return kernel;
    }

    private static async Task TranscribeFileUsingPluginDirectly(Kernel kernel)
    {
        Console.WriteLine("Transcribing file using plugin directly");
        var result = await kernel.InvokeAsync(
            nameof(AssemblyAIPlugin),
            AssemblyAIPlugin.TranscribeFunctionName,
            new KernelArguments
            {
                ["INPUT"] = "https://storage.googleapis.com/aai-docs-samples/espn.m4a"
            }
        );

        Console.WriteLine(result.GetValue<string>());
        Console.WriteLine();
    }

    private static async Task TranscribeFileUsingPluginFromSemanticFunction(Kernel kernel)
    {
        Console.WriteLine("Transcribing file and summarizing from within a semantic function");
        // This will pass the URL to the `INPUT` variable.
        // If `INPUT` is a URL, it'll use `INPUT` as `audioUrl`, otherwise, it'll use `INPUT` as `filePath`.
        const string prompt = """
                              Here is a transcript:
                              {{AssemblyAIPlugin.Transcribe "https://storage.googleapis.com/aai-docs-samples/espn.m4a"}}
                              ---
                              Summarize the transcript.
                              """;
        var result = await kernel.InvokePromptAsync(prompt);
        Console.WriteLine(result.GetValue<string>());
        Console.WriteLine();
    }

    private static async Task TranscribeFileUsingPlan(Kernel kernel)
    {
        Console.WriteLine("Transcribing file from a plan");
        var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions { AllowLoops = true });
        const string prompt = "Find the espn.m4a in my downloads folder and transcribe it.";
        var plan = await planner.CreatePlanAsync(kernel, prompt);

        Console.WriteLine("Plan:\n");
        Console.WriteLine(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));

        var transcript = await plan.InvokeAsync(kernel);
        Console.WriteLine(transcript);
        Console.WriteLine();
    }
}