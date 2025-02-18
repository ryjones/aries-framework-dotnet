using Newtonsoft.Json.Linq;
using WalletFramework.Core.Functional;
using WalletFramework.Oid4Vc.Oid4Vp.TransactionData.Errors;

namespace WalletFramework.Oid4Vc.Oid4Vp.TransactionData;

public readonly struct TransactionDataCredentialId
{
    private string Value { get; }

    private TransactionDataCredentialId(string value)
    {
        Value = value;
    }

    public string AsString => Value;

    public static implicit operator string(TransactionDataCredentialId id) => id.AsString;

    public static Validation<TransactionDataCredentialId> FromJToken(JToken jToken)
    {
        var id = jToken.ToString();
        
        if (string.IsNullOrWhiteSpace(id))
        {
            return new InvalidTransactionDataError("CredentialId in transaction data cannot be empty")
                .ToInvalid<TransactionDataCredentialId>();
        }

        return new TransactionDataCredentialId(id);
    }
}    
