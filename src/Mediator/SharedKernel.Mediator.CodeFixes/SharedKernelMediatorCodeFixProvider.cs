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
    private const string MissingArgumentDiagnosticId = "CS7036";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [
            MissingArgumentDiagnosticId,
            MediatorDiagnosticIds.MissingHandler,
            MediatorDiagnosticIds.InvalidHandlerSignature,
            MediatorDiagnosticIds.MissingCancellationToken,
            MediatorDiagnosticIds.MissingCancellationForwarding,
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
                case MissingArgumentDiagnosticId:
                case MediatorDiagnosticIds.MissingHandler:
                    if (string.Equals(diagnostic.Id, MissingArgumentDiagnosticId, StringComparison.Ordinal))
                    {
                        await MissingCancellationForwardingCodeFix.RegisterAsync(context, diagnostic).ConfigureAwait(false);
                        break;
                    }

                    await MissingHandlerCodeFix.RegisterAsync(context, diagnostic).ConfigureAwait(false);
                    break;
                case MediatorDiagnosticIds.InvalidHandlerSignature:
                    await InvalidHandlerSignatureCodeFix.RegisterAsync(context, diagnostic).ConfigureAwait(false);
                    break;
                case MediatorDiagnosticIds.MissingCancellationToken:
                    await MissingCancellationTokenCodeFix.RegisterAsync(context, diagnostic).ConfigureAwait(false);
                    break;
                case MediatorDiagnosticIds.MissingCancellationForwarding:
                    await MissingCancellationForwardingCodeFix.RegisterAsync(context, diagnostic).ConfigureAwait(false);
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
