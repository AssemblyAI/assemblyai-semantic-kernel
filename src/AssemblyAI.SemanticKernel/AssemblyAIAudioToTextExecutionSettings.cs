using Microsoft.SemanticKernel;

namespace AssemblyAI.SemanticKernel
{
    /// <summary>
    /// Configure AssemblyAI transcript parameters
    /// </summary>
    public class AssemblyAIAudioToTextExecutionSettings : PromptExecutionSettings
    {
        /// <summary>
        /// Parameters to transcribe audio using AssemblyAI.
        /// </summary>
        public CreateTranscriptOptionalParameters TranscriptParameters { get; set; }
    }
}