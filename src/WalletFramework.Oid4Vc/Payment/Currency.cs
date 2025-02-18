using WalletFramework.Core.Functional;
using WalletFramework.Core.Functional.Errors;

namespace WalletFramework.Oid4Vc.Payment;

public readonly struct Currency
{
    private Currency(string value)
    {
        Value = value;
    }
    
    public string Value { get; }

    public static Validation<Currency> FromString(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return new StringIsNullOrWhitespaceError<Currency>();
        }
        else
        {
            return new Currency(currency);
        }
    }
}
