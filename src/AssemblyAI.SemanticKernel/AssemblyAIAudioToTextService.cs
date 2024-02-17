using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AssemblyAI.Transcripts;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AudioToText;
using Microsoft.SemanticKernel.Contents;

namespace AssemblyAI.SemanticKernel
{
    /// <summary>
    /// AssemblyAI speech-to-text service.
    /// </summary>
    public class AssemblyAIAudioToTextService : IAudioToTextService
    {
        private readonly AssemblyAIClient _client;

        /// <summary>
        /// Attributes are not used by AssemblyAIAudioToTextService.
        /// </summary>
        public IReadOnlyDictionary<string, object> Attributes => new Dictionary<string, object>();

        /// <summary>
        /// Creates an instance of the <see cref="AssemblyAIAudioToTextService"/> with an AssemblyAI API key.
        /// </summary>
        /// <param name="apiKey">OpenAI API Key</param>
        public AssemblyAIAudioToTextService(string apiKey)
        {
            _client = new AssemblyAIClient(apiKey);
        }

        /// <inheritdoc/>
        public async Task<TextContent> GetTextContentAsync(
            AudioContent content,
            PromptExecutionSettings executionSettings = null,
            Kernel kernel = null,
            CancellationToken cancellationToken = default)
        {
            UploadedFile uploadedFile;
            using (var stream = content.Data.ToStream())
            {
                uploadedFile = await _client.Files.Upload(stream).ConfigureAwait(false);
            }

            AssemblyAI.Transcript transcript;
            if (executionSettings != null && executionSettings is AssemblyAIAudioToTextExecutionSettings aaiSettings)
            {
                if (aaiSettings.TranscriptParameters == null)
                {
                    throw new Exception(
                        "AssemblyAIAudioToTextExecutionSettings.TranscriptParameters is required when passing execution settings.");
                }

                transcript = await _client.Transcripts.Create(
                    uploadedFile.UploadUrl,
                    aaiSettings.TranscriptParameters
                ).ConfigureAwait(false);
            }
            else
            {
                transcript = await _client.Transcripts.Create(new CreateTranscriptParameters
                {
                    AudioUrl = uploadedFile.UploadUrl
                }).ConfigureAwait(false);
            }

            return new TextContent(
                text: transcript.Text,
                modelId: null,
                innerContent: transcript,
                encoding: Encoding.UTF8,
                metadata: null
            );
        }
    }
}