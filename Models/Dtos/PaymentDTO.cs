public class PaymentOptionsDTO
{
    public string upiId { get; set; }
    public string directLink { get; set; }
    public string gpayLink { get; set; }
    public string paytmLink { get; set; }
    public string phonePeLink { get; set; }
    public string directAndroidLink { get; set; }
    public string gpayAndroidLink { get; set; }
    public string paytmAndroidLink { get; set; }
    public string phonePeAndroidLink { get; set; }
    public string qrCode { get; set; }
    public AccountDetailDTO accountDetail { get; set; }
}


public class AccountDetailDTO
{
    public string bankAccountHolderName { get; set; } = null!;
    public string bankAccountNumber { get; set; } = null!;
    public string? bankName { get; set; }
    public string Ifsc { get; set; } = null!;
    public string? branchName { get; set; }
}