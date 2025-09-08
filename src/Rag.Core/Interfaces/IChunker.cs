namespace Rag.Core.Interfaces
{
    public interface IChunker
    {
        IEnumerable<(int Index, string Content)> Split(string text, int maxTokens = 400, int overlap = 50);
    }
}
