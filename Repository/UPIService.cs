using System;
using System.Web;

public class UPIService
{
    public string BuildUPIUri(string upiId, string name, string merchantCode,decimal amount, string description)
    {
        // Construct the UPI URI using the specified parameters
        // Customize the URI format according to the UPI service or payment gateway requirements

        // string uri = $"upi://pay?pa={upiId}&pn=MerchantName&mc=MerchantCode&tid=TransactionId&tr={amount}&tn={description}&am={amount}&cu=INR";

        string encodedUPIId = upiId;
        string encodedName = HttpUtility.UrlEncode(name);
        string encodedMerchantCode = HttpUtility.UrlEncode(merchantCode);
        // string encodedTransactionId = HttpUtility.UrlEncode(transactionId);
        string encodedDescription = HttpUtility.UrlEncode(description);
        string encodedtr = HttpUtility.UrlEncode("description");
        string encodedtn = HttpUtility.UrlEncode("description");

        string uri = $"upi://pay?pa={encodedUPIId}&pn={encodedName}&mc={encodedMerchantCode}&tr={encodedtr}&tn={encodedtn}&am={amount}&cu=INR";

        return uri;
    }
}
