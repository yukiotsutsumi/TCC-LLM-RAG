namespace Rag.Core.Domain.DTOs
{
    public class AskRequest 
    { 
        public string Question { get; set; } = ""; 
        public int K { get; set; } = 6; 
        public int MaxContextTokens { get; set; } = 3000; 
    }
}
