using WalletFramework.Core.Base64Url;
using WalletFramework.Core.Functional;
using WalletFramework.Core.Json;
using WalletFramework.Oid4Vc.Oid4Vp.TransactionData;

namespace WalletFramework.Oid4Vc.Payment;

public record PaymentTransactionData(TransactionData TransactionData, PaymentData PaymentData, Base64UrlString Encoded)
{
    public static Validation<PaymentTransactionData> FromBase64Url(Base64UrlString base64UrlString)
    {
        var jObjectValidation = base64UrlString.DecodeToJObject();

        var transactionDataValidation =
            from jObject in jObjectValidation
            from transactionData in TransactionData.FromJObject(jObject)
            select transactionData;

        var paymentDataValidation =
            from jObject in jObjectValidation
            from paymentData in PaymentData.FromJObject(jObject)
            select paymentData;

        var result=
            from transactionData in transactionDataValidation
            from paymentData in paymentDataValidation
            select new PaymentTransactionData(transactionData, paymentData, base64UrlString);

        return result;
    }
}
