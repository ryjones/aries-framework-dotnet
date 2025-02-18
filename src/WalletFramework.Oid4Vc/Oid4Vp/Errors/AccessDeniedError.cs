namespace WalletFramework.Oid4Vc.Oid4Vp.Errors;

public record AccessDeniedError : VpError
{
    public const string Code = "access_denied";
    
    public AccessDeniedError(string message) : base(Code, message)
    {
    }

    public AccessDeniedError(string Message, Exception Exception) 
        : base(Code, Message, Exception)
    {
    }
}
