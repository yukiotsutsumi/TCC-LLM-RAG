namespace Rag.Core.Domain.DTOs.Ask.Requests;

public class AskRequest
{
    public string Question { get; set; } = "";
    public int K { get; set; } = 5;

    public List<HistoryMessage> History { get; set; } = [];
}

public record HistoryMessage(string Role, string Content);
