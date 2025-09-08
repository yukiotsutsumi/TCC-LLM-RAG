namespace Rag.Infrastructure.Llm
{
    public class OllamaOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string GenerationModel { get; set; } = "qwen3:4b";
        public string EmbeddingModel { get; set; } = "bge-m3";
        public int NumCtx { get; set; } = 2048;
        public double Temperature { get; set; } = 0.2;
    }
}
