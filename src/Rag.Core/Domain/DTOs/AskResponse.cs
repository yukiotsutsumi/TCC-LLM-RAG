namespace Rag.Core.Domain.DTOs
{
    public class AskResponse 
    { 
        public string Answer { get; set; } = ""; 
        public List<SourceRef> Sources { get; set; } = []; 
        public int TookMs { get; set; } 
    }
}
