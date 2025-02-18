using Newtonsoft.Json.Linq;
using WalletFramework.Core.Functional;
using WalletFramework.Oid4Vc.Oid4Vp.TransactionData.Errors;
using static WalletFramework.Oid4Vc.Oid4Vp.TransactionData.TransactionDataTypeFun;

namespace WalletFramework.Oid4Vc.Oid4Vp.TransactionData;

public readonly struct TransactionDataType
{
    private string Value { get; }

    private TransactionDataType(string value)
    {
        Value = value;
    }

    public override string ToString() => Value;

    public static implicit operator string(TransactionDataType type) => type.Value;

    public static Validation<TransactionDataType> FromJToken(JToken jToken)
    {
        var type = jToken.ToString();
        
        if (string.IsNullOrWhiteSpace(type))
        {
            return new InvalidTransactionDataError("The transaction data type is null or empty");
        }

        if (GetSupportedTypes.Contains(type))
        {
            return new TransactionDataType(type);
        }
        else
        {
            return new InvalidTransactionDataError($"The transaction data type {type} is not supported");
        }
    }
}

public static class TransactionDataTypeFun
{
    public static IEnumerable<string> GetSupportedTypes =>
        new List<string>
        {
            "payment_data"
        };
}
