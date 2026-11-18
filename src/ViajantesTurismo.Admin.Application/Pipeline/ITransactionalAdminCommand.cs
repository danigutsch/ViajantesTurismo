using System.Diagnostics.CodeAnalysis;

namespace ViajantesTurismo.Admin.Application.Pipeline;

/// <summary>Marks an Admin command whose transaction is owned by the mediator pipeline.</summary>
[SuppressMessage(
    "Design",
    "CA1040:Avoid empty interfaces",
    Justification = "The mediator generator uses the interface as a compile-time pipeline constraint.")]
public interface ITransactionalAdminCommand;
