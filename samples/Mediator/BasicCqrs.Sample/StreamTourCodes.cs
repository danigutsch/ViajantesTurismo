using SharedKernel.Mediator;

namespace BasicCqrs.Sample;

/// <summary>
/// Requests a stream of sample tour codes.
/// </summary>
public sealed record StreamTourCodes(int Count) : IStreamQuery<string>;
