namespace Taskr.Bis3;

public class InvoicePayment
{
    public PaymentMeansCode PaymentMeansCode { get; set; }
    public string? BankAccountId { get; set; }
    public string? BankAccountName { get; set; }
    public string? BankAccountRegistrationId { get; set; }
}

public enum PaymentMeansCode
{
    NotSet = 0,
    PaymentInCash = 10,
    CreditTransfer = 30,
    BankTransfer = 31,
    PaymentToBankAccount = 42,
    CardPayment = 48,
    DebitTransfer = 49,
    OnlinePayment = 68
}
