namespace WalletFramework.Oid4Vc.Oid4Vp.Errors;

public record InvalidRequestError : VpError
{
    public const string Code = "invalid_request";

    public InvalidRequestError(string message) : base(Code, message)
    {
    }

    public InvalidRequestError(string Message, Exception Exception) 
        : base(Code, Message, Exception)
    {
    }
}
