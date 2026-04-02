namespace Rag.Core.Domain.DTOs.Ask.Requests;

public class AskRequest
{
    public string Question { get; set; } = "";
    public int K { get; set; } = 5;

    // Histórico da conversa — últimas N trocas enviadas para dar contexto à IA
    // Não persistido — vive apenas na sessão do usuário
    public List<HistoryMessage> History { get; set; } = [];
}

// Role: "user" | "assistant"
public record HistoryMessage(string Role, string Content);
