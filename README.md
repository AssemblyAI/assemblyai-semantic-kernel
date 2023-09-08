<img src="https://github.com/AssemblyAI/assemblyai-python-sdk/blob/master/assemblyai.png?raw=true" width="500" alt="AssemblyAI logo"/>

---

[![GitHub License](https://img.shields.io/github/license/AssemblyAI/AssemblyAI.SemanticKernel "GitHub License")](https://github.com/AssemblyAI/AssemblyAI.SemanticKernel/blob/main/LICENSE)
[![CI Build](https://github.com/AssemblyAI/AssemblyAI.SemanticKernel/actions/workflows/ci.yml/badge.svg)](https://github.com/AssemblyAI/AssemblyAI.SemanticKernel/actions/workflows/ci.yml)
[![AssemblyAI Twitter](https://img.shields.io/twitter/follow/AssemblyAI?label=%40AssemblyAI&style=social "AssemblyAI Twitter")](https://twitter.com/AssemblyAI)
[![AssemblyAI YouTube](https://img.shields.io/youtube/channel/subscribers/UCtatfZMf-8EkIwASXM4ts0A "AssemblyAI YouTube")](https://www.youtube.com/@AssemblyAI)

# AssemblyAI plugins for Semantic Kernel

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
using Microsoft.SemanticKernel.Orchestration;

// Build your kernel
var kernel = new KernelBuilder().Build();

// Get AssemblyAI API key from env variables, or much better, from .NET configuration
string apiKey = Environment.GetEnvironmentVariable("ASSEMBLYAI_API_KEY")
  ?? throw new Exception("ASSEMBLYAI_API_KEY env variable not configured.");

var transcriptPlugin = kernel.ImportSkill(
    new TranscriptPlugin(apiKey: apiKey),
    "TranscriptPlugin"
);
```

Get the `Transcribe` function from the transcript plugin and invoke it with the context variables.
```csharp
var variables = new ContextVariables
{
    ["audioUrl"] = "https://storage.googleapis.com/aai-docs-samples/espn.m4a"
};

var context = await kernel.Skills
    .GetFunction("TranscriptPlugin", "Transcribe")
    .InvokeAsync(variables);
    
Console.WriteLine(context.Result);
```

The `context.Result` property contains the transcript text if successful.

You can also upload local audio and video file. To do this:
- Set the `TranscriptPlugin.AllowFileSystemAccess` property to `true`
- Configure the path of the file to upload as the `filePath` parameter

```csharp
var transcriptPlugin = kernel.ImportSkill(
    new TranscriptPlugin(apiKey: apiKey)
    {
        AllowFileSystemAccess = true
    },
    "TranscriptPlugin"
);

var variables = new ContextVariables
{
    ["filePath"] = "./espn.m4a"
};

var context = await kernel.Skills
    .GetFunction("TranscriptPlugin", "Transcribe")
    .InvokeAsync(variables);
    
Console.WriteLine(context.Result);
```

If `filePath` and `audioUrl` are specified, the `filePath` will be used to upload the file and `audioUrl` will be overridden.

The code above explicitly invokes the transcript plugin, but it can also be invoked as part of a plan. 
Check out [the Sample project](./src/Sample/Program.cs#L50) which uses a plan to transcribe an audio file in addition to explicit invocation.
