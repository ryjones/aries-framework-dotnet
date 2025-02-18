using LanguageExt;
using Newtonsoft.Json.Linq;
using WalletFramework.Core.Functional;
using WalletFramework.Core.Json;
using WalletFramework.Core.Json.Errors;

namespace WalletFramework.Oid4Vc.Payment;

public record PaymentSchedule(
    DateTime StartDate,
    Option<DateTime> ExpiryDate,
    int Frequency)
{
    public static Validation<PaymentSchedule> FromJObject(JObject jObject)
    {
        var validateDateTime = new Func<JToken, Validation<DateTime>>(token =>
        {
            var str = token.ToString();
            try
            {
                return DateTime.Parse(str);
            }
            catch (Exception e)
            {
                return new InvalidJsonError("The value is not a DateTime value", e);
            }
        });
        
        var validateFrequency = new Func<JToken, Validation<int>>(token =>
        {
            var str = token.ToString();
            try
            {
                return int.Parse(str);
            }
            catch (Exception e)
            {
                return new InvalidJsonError("The value is not an integer value", e);
            }
        });

        var startDateValidation =
            from token in jObject.GetByKey("start_date")
            from startDate in validateDateTime(token)
            select startDate;
        
        var expiryDataValidation =
            from token in jObject.GetByKey("expiry_date")
            from expiryDate in validateDateTime(token)
            select expiryDate;

        var frequencyValidation =
            from token in jObject.GetByKey("frequency")
            from frequency in validateFrequency(token)
            select frequency;

        return
            from startDate in startDateValidation
            from frequency in frequencyValidation
            let expiryDateOption = expiryDataValidation.ToOption()
            select new PaymentSchedule(startDate, expiryDateOption, frequency);
    }
}
