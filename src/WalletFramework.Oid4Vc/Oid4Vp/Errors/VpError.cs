using LanguageExt;
using WalletFramework.Core.Functional;
using WalletFramework.Oid4Vc.Errors;

namespace WalletFramework.Oid4Vc.Oid4Vp.Errors;

public abstract record VpError : Error
{
    public string ErrorCode { get; }
    
    protected VpError(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    protected VpError(
        string errorCode,
        string Message,
        Option<Exception> Exception) : base(Message, Exception)
    {
        ErrorCode = errorCode;
    }
}

public static class VpErrorFun
{
    public static ErrorResponse ToResponse(this VpError error)
    {
        return new ErrorResponse(error.ErrorCode, error.Message, null);
    }
}
