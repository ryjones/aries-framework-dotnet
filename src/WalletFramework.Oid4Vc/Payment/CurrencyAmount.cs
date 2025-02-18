using Newtonsoft.Json.Linq;
using WalletFramework.Core.Functional;
using WalletFramework.Core.Json;
using WalletFramework.Core.Json.Errors;

namespace WalletFramework.Oid4Vc.Payment;

public record CurrencyAmount(Currency Currency, decimal Value)
{
    public static Validation<CurrencyAmount> FromJObject(JObject jObject)
    {
        var currencyValidation =
            from jToken in jObject.GetByKey("currency")
            from currency in Currency.FromString(jToken.ToString())
            select currency;

        var validateValue = new Func<JToken, Validation<decimal>>(token =>
        {
            var str = token.ToString();
            try
            {
                return decimal.Parse(str);
            }
            catch (Exception e)
            {
                return new InvalidJsonError("The value is not a decimal value", e);
            }
        });

        var valueValidation =
            from jToken in jObject.GetByKey("value")
            from value in validateValue(jToken)
            select value;

        return
            from currency in currencyValidation
            from value in valueValidation
            select new CurrencyAmount(currency, value);
    }
}
