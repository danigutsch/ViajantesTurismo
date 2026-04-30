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
    private const string InaccessibleRegistrationTypeDiagnosticId = "SKMED010";
    private const string MissingModuleMarkerDiagnosticId = "SKMED011";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [
            MissingHandlerDiagnosticId,
            InvalidHandlerSignatureDiagnosticId,
            InaccessibleRegistrationTypeDiagnosticId,
            MissingModuleMarkerDiagnosticId
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
                case MissingHandlerDiagnosticId:
                    await MissingHandlerCodeFix.RegisterAsync(context, diagnostic).ConfigureAwait(false);
                    break;
                case InvalidHandlerSignatureDiagnosticId:
                    await InvalidHandlerSignatureCodeFix.RegisterAsync(context, diagnostic).ConfigureAwait(false);
                    break;
                case InaccessibleRegistrationTypeDiagnosticId:
                    await InaccessibleRegistrationTypeCodeFix.RegisterAsync(context, diagnostic).ConfigureAwait(false);
                    break;
                case MissingModuleMarkerDiagnosticId:
                    await MissingModuleMarkerCodeFix.RegisterAsync(context, diagnostic).ConfigureAwait(false);
                    break;
            }
        }
    }
}
