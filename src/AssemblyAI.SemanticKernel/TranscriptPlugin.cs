using System;

namespace AssemblyAI.SemanticKernel
{
    [Obsolete("Use AssemblyAIPlugin instead.")]
    public class TranscriptPlugin : AssemblyAIPlugin
    {
        public new const string PluginName = nameof(TranscriptPlugin);

        public bool AllowFileSystemAccess
        {
            get => Options.AllowFileSystemAccess;
            set => Options.AllowFileSystemAccess = value;
        }

        public TranscriptPlugin(string apiKey) : base(apiKey)
        {
        }

        public TranscriptPlugin(string apiKey, bool allowFileSystemAccess) : base(apiKey, allowFileSystemAccess)
        {
        }
    }
}