namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Contains a mediated finalized document artifact returned by the Admin API.</summary>
public sealed record DocumentArtifactResponse(ReadOnlyMemory<byte> Content, string FileName);
