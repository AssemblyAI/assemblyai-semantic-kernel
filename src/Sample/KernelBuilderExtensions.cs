using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace AssemblyAI.SemanticKernel.Plugins.Sample;

internal static class KernelBuilderExtensions
{
    internal static KernelBuilder WithCompletionService(this KernelBuilder kernelBuilder, IConfiguration config)
    {
        switch (config["LlmService"]!)
        {
            case "AzureOpenAI":
                if (config["AzureOpenAI:DeploymentType"]! == "text-completion")
                {
                    kernelBuilder.WithAzureTextCompletionService(
                        deploymentName: config["AzureOpenAI:TextCompletionDeploymentName"]!,
                        endpoint: config["AzureOpenAI:Endpoint"]!,
                        apiKey: config["AzureOpenAI:ApiKey"]!
                    );
                }
                else if (config["AzureOpenAI:DeploymentType"]! == "chat-completion")
                {
                    kernelBuilder.WithAzureChatCompletionService(
                        deploymentName: config["AzureOpenAI:ChatCompletionDeploymentName"]!,
                        endpoint: config["AzureOpenAI:Endpoint"]!,
                        apiKey: config["AzureOpenAI:ApiKey"]!
                    );
                }

                break;

            case "OpenAI":
                switch (config["OpenAI:ModelType"]!)
                {
                    case "text-completion":
                        kernelBuilder.WithOpenAITextCompletionService(
                            modelId: config["OpenAI:TextCompletionModelId"]!,
                            apiKey: config["OpenAI:ApiKey"]!,
                            orgId: config["OpenAI:OrgId"]
                        );
                        break;
                    case "chat-completion":
                        kernelBuilder.WithOpenAIChatCompletionService(
                            modelId: config["OpenAI:ChatCompletionModelId"]!,
                            apiKey: config["OpenAI:ApiKey"]!,
                            orgId: config["OpenAI:OrgId"]
                        );
                        break;
                }

                break;

            default:
                throw new ArgumentException($"Invalid service type value: {config["OpenAI:ModelType"]}");
        }

        return kernelBuilder;
    }
}