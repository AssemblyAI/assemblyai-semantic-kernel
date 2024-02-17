namespace AssemblyAI.SemanticKernel
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Transcript
    {
        public string Id { get; set; } = null;
        public string Status { get; set; } = null;
        public string Text { get; set; }

        public string Error { get; set; }
    }
}