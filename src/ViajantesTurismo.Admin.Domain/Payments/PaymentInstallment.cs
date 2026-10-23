using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>An immutable, positive EUR installment with a civil calendar due date.</summary>
public sealed class PaymentInstallment
{
    private PaymentInstallment(Guid id, PaymentMoney amount, DateOnly dueDate)
    {
        Id = id;
        Amount = amount;
        DueDate = dueDate;
    }
    /// <summary>Gets the immutable allocation target identifier.</summary>
    public Guid Id
    {
        get;
    }
    /// <summary>Gets the positive EUR amount.</summary>
    public PaymentMoney Amount
    {
        get;
    }
    /// <summary>Gets the calendar due date, independent of quote-expiry instants.</summary>
    public DateOnly DueDate
    {
        get;
    }
    /// <summary>Creates one validated installment.</summary>
    public static Result<PaymentInstallment> Create(decimal amount, DateOnly dueDate)
    {
        var money = PaymentMoney.Create(amount, Currency.Euro);
        return money.IsFailure || amount <= 0 || dueDate == default
            ? FinancialErrors.Invalid("installment").ConvertError<PaymentInstallment>() : Result.Ok(new PaymentInstallment(Guid.CreateVersion7(), money.Value, dueDate));
    }
    internal static PaymentInstallment Restore(Guid id, PaymentMoney money, DateOnly dueDate) => new(id, money, dueDate);
}
