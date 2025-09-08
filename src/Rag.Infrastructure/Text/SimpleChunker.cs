using Rag.Core.Interfaces;

namespace Rag.Infrastructure.Text;

public class SimpleChunker : IChunker
{
    public IEnumerable<(int Index, string Content)> Split(string text, int maxTokens = 400, int overlap = 50)
    {
        var words = text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
        int step = Math.Max(1, maxTokens - overlap);
        for (int start = 0, idx = 0; start < words.Length; start += step, idx++)
        {
            var segment = string.Join(" ", words.Skip(start).Take(maxTokens));
            if (!string.IsNullOrWhiteSpace(segment))
                yield return (idx, segment);
        }
    }
}