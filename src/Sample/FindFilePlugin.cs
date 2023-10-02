using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace AssemblyAI.SemanticKernel.Sample;

public class FindFilePlugin
{
    public const string PluginName = "FindFilePlugin";
    private readonly IKernel _kernel;

    public FindFilePlugin(IKernel kernel)
    {
        _kernel = kernel;
    }

    private async Task<string?> GetCommonFolderPath(string commonFolderName)
    {
        var prompt = $"The path for the common folder '{commonFolderName}' " +
                     $"on operating platform {Environment.OSVersion.Platform.ToString()} " +
                     $"with user profile path '{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}' is ";
        var context = await _kernel.InvokeSemanticFunctionAsync(
            promptTemplate: prompt,
            temperature: 0
        );
        var matches = Regex.Matches(
            context.Result,
            @"([a-zA-Z]?\:?[\/\\][\S-[""'\. ]]*[\/\\][\S-[""'\. ]]*)",
            RegexOptions.IgnoreCase
        );
        return matches.LastOrDefault()?.Value ?? null;
    }


    public const string LocateFileFunctionName = nameof(LocateFile);

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
}