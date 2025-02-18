using OneOf;
using WalletFramework.Core.Functional;
using WalletFramework.Oid4Vc.Oid4Vp.Errors;

namespace WalletFramework.Oid4Vc.Oid4Vp.Models;

public struct AuthorizationRequestUri
{
    public OneOf<AuthorizationRequestByReference, AuthorizationRequestByValue> Value { get; }
        
    private AuthorizationRequestUri(OneOf<AuthorizationRequestByReference, AuthorizationRequestByValue> value) => Value = value;
    
    public static Validation<AuthorizationRequestUri> FromUri(Uri uri)
    {
        var byValueValidation = AuthorizationRequestByValue.CreateAuthorizationRequestByValue(uri).Match(
            value => new AuthorizationRequestUri(value),
            () => new InvalidRequestError("Authorization Request URI could not be parsed")
                .ToInvalid<AuthorizationRequestUri>());
        
        return AuthorizationRequestByReference.CreateAuthorizationRequestByReference(uri).Match(
            byReference => new AuthorizationRequestUri(byReference),
            () => byValueValidation);
    }
}
