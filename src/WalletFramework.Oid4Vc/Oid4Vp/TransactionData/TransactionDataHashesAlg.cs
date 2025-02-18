using Newtonsoft.Json.Linq;
using WalletFramework.Core.Functional;
using WalletFramework.Oid4Vc.Oid4Vp.TransactionData.Errors;

namespace WalletFramework.Oid4Vc.Oid4Vp.TransactionData;

public readonly struct TransactionDataHashesAlg
{
    private string Value { get; }

    private TransactionDataHashesAlg(string value)
    {
        Value = value;
    }

    public string AsString => Value;

    public static implicit operator string(TransactionDataHashesAlg alg) => alg.AsString;

    public static Validation<TransactionDataHashesAlg> FromJToken(JToken jToken)
    {
        var alg = jToken.ToString();
        
        if (string.IsNullOrWhiteSpace(alg))
        {
            return new InvalidTransactionDataError("The transaction data hash algorithm is null or empty");
        }

        if (TransactionDataHashesAlgFun.GetSupportedTypes.Contains(alg))
        {
            return new TransactionDataHashesAlg(alg);
        }
        else
        {
            return new InvalidTransactionDataError($"The transaction data hash algorithm {alg} is not supported");
        }
    }

    public static TransactionDataHashesAlg CreateSha256Alg() => new("sha-256");
}

public static class TransactionDataHashesAlgFun
{
    public static IEnumerable<string> GetSupportedTypes =>
        new List<string>
        {
            "sha-256"
        };
}
