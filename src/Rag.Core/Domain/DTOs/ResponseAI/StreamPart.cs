namespace Rag.Core.Domain.DTOs.ResponseIA
{
    public enum StreamPartKind
    {
        Delta,
        Finished
    }

    public sealed class StreamPart
    {
        public StreamPartKind Kind { get; init; }
        public string? Delta { get; init; }
        public IReadOnlyList<SourceRef>? Sources { get; init; }

        public static StreamPart CreateDelta(string delta) =>
            new() { Kind = StreamPartKind.Delta, Delta = delta };

        public static StreamPart CreateFinished(IReadOnlyList<SourceRef> sources) =>
            new() { Kind = StreamPartKind.Finished, Sources = sources };
    }
}