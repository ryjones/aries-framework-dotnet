using Newtonsoft.Json.Linq;
using WalletFramework.Core.Base64Url;
using WalletFramework.Core.Functional;
using WalletFramework.Oid4Vc.Payment;

namespace WalletFramework.Oid4Vc.Oid4Vp.TransactionData;

public record TransactionDataArray(Base64UrlString[] EncodedTransactionDataStrings)
{
    public static Validation<TransactionDataArray> FromJObject(JArray jArray)
    {
        var arrayValidation = jArray.TraverseAll(jToken =>
        {
            return Base64UrlString.FromString(jToken.ToString());
        });

        return
            from base64UrlStrings in arrayValidation
            select new TransactionDataArray(base64UrlStrings.ToArray());
    }
}

public static class TransactionDataArrayFun
{
    public static Validation<IEnumerable<PaymentTransactionData>> Decode(this TransactionDataArray array) =>
        array.EncodedTransactionDataStrings.TraverseAll(PaymentTransactionData.FromBase64Url);
}
