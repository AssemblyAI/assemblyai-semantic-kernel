using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;

namespace AssemblyAI.SemanticKernel.Sample;

public class FindFilePlugin
{
    private async Task<string?> GetCommonFolderPath(Kernel kernel, string commonFolderName)
    {
        var prompt = $"The path for the common folder '{commonFolderName}' " +
                     $"on operating platform {Environment.OSVersion.Platform.ToString()} " +
                     $"with user profile path '{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}' is ";
        var context = await kernel.InvokePromptAsync(prompt);
        var matches = Regex.Matches(
            context.GetValue<string>()!,
            @"([a-zA-Z]?\:?[\/\\][\S-[""'\. ]]*[\/\\][\S-[""'\. ]]*)",
            RegexOptions.IgnoreCase
        );
        return matches.LastOrDefault()?.Value ?? null;
    }

    [KernelFunction, Description("Find files in common folders.")]
    public async Task<string> LocateFile(
        [Description("The name of the file")] string fileName,
        [Description("The name of the common folder")]
        string? commonFolderName,
        Kernel kernel)
    {
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
            _ => await GetCommonFolderPath(kernel, commonFolderName)
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