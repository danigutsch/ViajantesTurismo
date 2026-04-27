using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace SharedKernel.Mediator.CodeFixes;

/// <summary>
/// Provides safe code fixes for currently implemented mediator diagnostics.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SharedKernelMediatorCodeFixProvider))]
public sealed class SharedKernelMediatorCodeFixProvider : CodeFixProvider
{
    private const string MissingHandlerDiagnosticId = "SKMED001";
    private const string InvalidHandlerSignatureDiagnosticId = "SKMED003";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [MissingHandlerDiagnosticId, InvalidHandlerSignatureDiagnosticId];

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            switch (diagnostic.Id)
            {
                case MissingHandlerDiagnosticId:
                    await MissingHandlerCodeFix.RegisterAsync(context, diagnostic).ConfigureAwait(false);
                    break;
                case InvalidHandlerSignatureDiagnosticId:
                    await InvalidHandlerSignatureCodeFix.RegisterAsync(context, diagnostic).ConfigureAwait(false);
                    break;
            }
        }
    }
}
