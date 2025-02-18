using LanguageExt;
using Newtonsoft.Json.Linq;
using WalletFramework.Core.Functional;
using WalletFramework.Core.Json;

namespace WalletFramework.Oid4Vc.Payment;

public record PaymentData(
    Payee Payee,
    CurrencyAmount CurrencyAmount,
    Option<PaymentSchedule> RecurringSchedule)
{
    public static Validation<PaymentData> FromJObject(JObject jObject)
    {
        var payeeValidation =
            from jToken in jObject.GetByKey("payee")
            from payee in Payee.FromJToken(jToken)
            select payee;

        var currencyAmountValidation =
            from jToken in jObject.GetByKey("currency_amount")
            from currencyJObject in jToken.ToJObject()
            from currencyAmount in CurrencyAmount.FromJObject(currencyJObject)
            select currencyAmount;

        var recurringScheduleValidation =
            from jToken in jObject.GetByKey("recurring_schedule")
            from scheduleJObject in jToken.ToJObject()
            from schedule in PaymentSchedule.FromJObject(scheduleJObject)
            select schedule;
        
        return ValidationFun.Valid(Create)
            .Apply(payeeValidation)
            .Apply(currencyAmountValidation)
            .Apply(recurringScheduleValidation.ToOption());
    }
    
    private static PaymentData Create(
        Payee payee,
        CurrencyAmount currencyAmount,
        Option<PaymentSchedule> schedule) =>
        new(payee, currencyAmount, schedule);
}
