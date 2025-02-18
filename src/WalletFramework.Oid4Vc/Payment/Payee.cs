using Newtonsoft.Json.Linq;
using WalletFramework.Core.Functional;
using WalletFramework.Core.Functional.Errors;

namespace WalletFramework.Oid4Vc.Payment;

public record Payee
{
    public string Value { get; }

    private Payee(string value) => Value = value;

    public static Validation<Payee> FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new StringIsNullOrWhitespaceError<Payee>();
        }

        return new Payee(json);
    }

    public static Validation<Payee> FromJToken(JToken token)
    {
        var str = token.ToString();
        return FromJson(str);
    }
}
