using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Captures raw stream pipeline discovery data before request-specific binding occurs.
/// </summary>
internal sealed record RawStreamPipelineDescriptor(
    string MetadataName,
    string? OpenGenericMetadataName,
    int Stage,
    int Order,
    PipelineApplicability Applicability,
    bool IsAccessibleToGeneratedMediator,
    string RequestMetadataName,
    string ResponseMetadataName,
    INamedTypeSymbol TypeSymbol,
    ITypeSymbol RequestTypePattern,
    ITypeSymbol ResponseTypePattern,
    ImmutableArray<ITypeParameterSymbol> TypeParameters);
