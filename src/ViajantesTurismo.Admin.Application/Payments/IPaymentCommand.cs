using SharedKernel.Mediator.Transactions;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Declares the advisory locks for a payment command executed in the mediator transaction.</summary>
public interface IPaymentCommand : ITransactionLockDeclaration;
