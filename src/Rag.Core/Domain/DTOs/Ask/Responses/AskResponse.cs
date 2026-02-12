using Rag.Core.Domain.DTOs.ResponseIA;

namespace Rag.Core.Domain.DTOs.Ask.Responses
{
    public class AskResponse 
    { 
        public string Answer { get; set; } = ""; 
        public List<SourceRef> Sources { get; set; } = []; 
        public int TookMs { get; set; } 
    }
}
