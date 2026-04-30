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
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [
            MediatorDiagnosticIds.MissingHandler,
            MediatorDiagnosticIds.InvalidHandlerSignature,
            MediatorDiagnosticIds.InaccessibleRegistrationType,
            MediatorDiagnosticIds.MissingModuleMarker
        ];

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
                case MediatorDiagnosticIds.MissingHandler:
                    await MissingHandlerCodeFix.RegisterAsync(context, diagnostic).ConfigureAwait(false);
                    break;
                case MediatorDiagnosticIds.InvalidHandlerSignature:
                    await InvalidHandlerSignatureCodeFix.RegisterAsync(context, diagnostic).ConfigureAwait(false);
                    break;
                case MediatorDiagnosticIds.InaccessibleRegistrationType:
                    await InaccessibleRegistrationTypeCodeFix.RegisterAsync(context, diagnostic).ConfigureAwait(false);
                    break;
                case MediatorDiagnosticIds.MissingModuleMarker:
                    await MissingModuleMarkerCodeFix.RegisterAsync(context, diagnostic).ConfigureAwait(false);
                    break;
            }
        }
    }
}
