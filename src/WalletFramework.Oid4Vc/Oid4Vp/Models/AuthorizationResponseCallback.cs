using Newtonsoft.Json;

namespace WalletFramework.Oid4Vc.Oid4Vp.Models;

internal record AuthorizationResponseCallback
{
    [JsonProperty("redirect_uri")]
    public Uri RedirectUri { get; }
        
    public static implicit operator Uri (AuthorizationResponseCallback? response) => response.RedirectUri;
        
    public static implicit operator AuthorizationResponseCallback (Uri redirectUri) => new (redirectUri);
    
    [JsonConstructor]
    private AuthorizationResponseCallback(Uri redirectUri)
    {
        RedirectUri = redirectUri;
    }
}

internal static class AuthorizationResponseCallbackFun
{
    public static Uri ToUri(this AuthorizationResponseCallback callback) => callback.RedirectUri;
}
