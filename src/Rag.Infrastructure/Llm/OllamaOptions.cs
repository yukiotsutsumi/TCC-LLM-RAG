namespace Rag.Infrastructure.Llm
{
    public class OllamaOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string GenerationModel { get; set; } = "qwen2.5:3b";
        public string EmbeddingModel { get; set; } = "mxbai-embed-large";
        public int NumCtx { get; set; } = 2048;
        public double Temperature { get; set; } = 0.2;
    }
}
