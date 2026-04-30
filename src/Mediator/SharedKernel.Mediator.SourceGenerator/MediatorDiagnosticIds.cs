namespace SharedKernel.Mediator;

/// <summary>
/// Central diagnostic identifiers used by the mediator generator and related tooling.
/// </summary>
internal static class MediatorDiagnosticIds
{
    public const string MissingHandler = "SKMED001";
    public const string MultipleHandlers = "SKMED002";
    public const string InvalidHandlerSignature = "SKMED003";
    public const string InaccessibleRegistrationType = "SKMED010";
    public const string MissingModuleMarker = "SKMED011";
    public const string DuplicateGeneratedRegistration = "SKMED012";
    public const string UnprovenObjectDispatchCoverage = "SKMED013";
}
