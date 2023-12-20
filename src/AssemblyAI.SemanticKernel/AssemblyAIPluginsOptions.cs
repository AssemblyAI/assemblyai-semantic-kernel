namespace AssemblyAI.SemanticKernel
{
    /// <summary>
    /// Options to configure the AssemblyAI plugin with.
    /// </summary>
    public class AssemblyAIPluginsOptions
    {
        /// <summary>
        /// The AssemblyAI API key. Find your API key at https://www.assemblyai.com/app/account
        /// </summary>
        public string ApiKey { get; set; }
        
        /// <summary>
        /// If true, you can transcribe audio files from disk.
        /// The file be uploaded to AssemblyAI's server to transcribe and deleted when transcription is completed.
        /// If false, an exception will be thrown when trying to transcribe files from disk.
        /// </summary>
        public bool AllowFileSystemAccess { get; set; }
    }
}