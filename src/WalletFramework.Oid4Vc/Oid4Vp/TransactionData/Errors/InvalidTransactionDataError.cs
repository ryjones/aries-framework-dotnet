using LanguageExt;
using WalletFramework.Oid4Vc.Oid4Vp.Errors;

namespace WalletFramework.Oid4Vc.Oid4Vp.TransactionData.Errors;

public record InvalidTransactionDataError : VpError
{
    public const string Code = "invalid_transaction_data";
    
    public InvalidTransactionDataError(string message) : base(Code, message)
    {
    }

    public InvalidTransactionDataError(
        string Message,
        Option<Exception> Exception) : base(Code, Message, Exception)
    {
    }
}
