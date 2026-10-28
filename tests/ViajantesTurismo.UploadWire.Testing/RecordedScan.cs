namespace ViajantesTurismo.UploadWire.Testing;

/// <summary>One recorded scan.</summary>
/// <param name="Content">Bytes the scanner received.</param>
/// <param name="ChunksStarted">Client chunks that had started sending when the scan began.</param>
public sealed record RecordedScan(ReadOnlyMemory<byte> Content, int ChunksStarted);
