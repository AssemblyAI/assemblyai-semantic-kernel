<img src="https://github.com/AssemblyAI/assemblyai-python-sdk/blob/master/assemblyai.png?raw=true" width="500" alt="AssemblyAI logo"/>

---

[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/AssemblyAI.SemanticKernel)](https://www.nuget.org/packages/AssemblyAI.SemanticKernel/)
[![CI Build](https://github.com/AssemblyAI/assemblyai-semantic-kernel/actions/workflows/ci.yml/badge.svg)](https://github.com/AssemblyAI/assemblyai-semantic-kernel/actions/workflows/ci.yml)
[![GitHub License](https://img.shields.io/github/license/AssemblyAI/assemblyai-semantic-kernel "GitHub License")](https://github.com/AssemblyAI/assemblyai-semantic-kernel/blob/main/LICENSE)
[![AssemblyAI Twitter](https://img.shields.io/twitter/follow/AssemblyAI?label=%40AssemblyAI&style=social)](https://twitter.com/AssemblyAI)
[![AssemblyAI YouTube](https://img.shields.io/youtube/channel/subscribers/UCtatfZMf-8EkIwASXM4ts0A)](https://www.youtube.com/@AssemblyAI)
[![Discord](https://img.shields.io/discord/875120158014853141?logo=discord&label=Discord&link=https%3A%2F%2Fdiscord.com%2Fchannels%2F875120158014853141&style=social)
](https://discord.gg/5aQNZyq3)

# AssemblyAI integration for Semantic Kernel

Transcribe audio using AssemblyAI with Semantic Kernel plugins.

## Get started

Add the [AssemblyAI.SemanticKernel NuGet package](https://www.nuget.org/packages/AssemblyAI.SemanticKernel) to your project.

```bash
dotnet add package AssemblyAI.SemanticKernel
```

Next, register the `TranscriptPlugin` into your kernel:

```csharp
using AssemblyAI.SemanticKernel;
using Microsoft.SemanticKernel;

// Build your kernel
var kernel = new KernelBuilder().Build();

// Get AssemblyAI API key from env variables, or much better, from .NET configuration
string apiKey = Environment.GetEnvironmentVariable("ASSEMBLYAI_API_KEY")
  ?? throw new Exception("ASSEMBLYAI_API_KEY env variable not configured.");

kernel.ImportFunctions(
    new TranscriptPlugin(apiKey: apiKey),
    TranscriptPlugin.PluginName
);
```

## Usage

Get the `Transcribe` function from the transcript plugin and invoke it with the context variables.
```csharp
var function = kernel.Functions
    .GetFunction(TranscriptPlugin.PluginName, TranscriptPlugin.TranscribeFunctionName);
var context = kernel.CreateNewContext();
context.Variables["INPUT"] = "https://storage.googleapis.com/aai-docs-samples/espn.m4a";
var result = await function.InvokeAsync(context);
Console.WriteLine(result.GetValue<string());
```

You can get the transcript using `result.GetValue<string>()`.

You can also upload local audio and video file. To do this:
- Set the `TranscriptPlugin.AllowFileSystemAccess` property to `true`.
- Configure the `INPUT` variable with a local file path.

```csharp
kernel.ImportFunctions(
    new TranscriptPlugin(apiKey: apiKey)
    {
        AllowFileSystemAccess = true
    },
    TranscriptPlugin.PluginName
);
var function = kernel.Functions
    .GetFunction(TranscriptPlugin.PluginName, TranscriptPlugin.TranscribeFunctionName);
var context = kernel.CreateNewContext();
context.Variables["INPUT"] = "./espn.m4a";
var result = await function.InvokeAsync(context);
Console.WriteLine(result.GetValue<string>());
```

You can also invoke the function from within a semantic function like this.

```csharp
var prompt = """
             Here is a transcript:
             {{TranscriptPlugin.Transcribe "https://storage.googleapis.com/aai-docs-samples/espn.m4a"}}
             ---
             Summarize the transcript.
             """;
var context = kernel.CreateNewContext();
var function = kernel.CreateSemanticFunction(prompt);
var result = await function.InvokeAsync(context);
Console.WriteLine(result.GetValue<string>());
```

All the code above explicitly invokes the transcript plugin, but it can also be invoked as part of a plan. 
Check out [the Sample project](./src/Sample/Program.cs#L96)) which uses a plan to transcribe an audio file in addition to explicit invocation.

## Notes

- The AssemblyAI integration only supports Semantic Kernel with .NET at this moment. 
If there's demand, we will extend support to other platforms, so let us know!
- Semantic Kernel itself is still in pre-release, and changes frequently, so we'll keep our integration in pre-release until SK is GA'd.
- Feel free to [file an issue](https://github.com/AssemblyAI/assemblyai-semantic-kernel/issues) in case of bugs or feature requests.
