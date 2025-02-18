using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using LanguageExt;
using WalletFramework.Oid4Vc.Oid4Vp.Extensions;
using Org.BouncyCastle.X509;
using WalletFramework.Core.X509;
using WalletFramework.Oid4Vc.Oid4Vp.Errors;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;
using static WalletFramework.Oid4Vc.Oid4Vp.Models.AuthorizationRequest;
using static Newtonsoft.Json.Linq.JArray;
using static System.Convert;

namespace WalletFramework.Oid4Vc.Oid4Vp.Models;

/// <summary>
///     Represents a request object as defined in the context of OpenID4VP.
/// </summary>
public readonly struct RequestObject
{
    private JwtSecurityToken Value { get; }

    /// <summary>
    ///     The client ID scheme used to obtain and validate metadata of the verifier.
    /// </summary>
    public ClientIdScheme ClientIdScheme => AuthorizationRequest.ClientIdScheme;

    /// <summary>
    ///     The client ID of the verifier.
    /// </summary>
    public string ClientId => AuthorizationRequest.ClientId;

    private AuthorizationRequest AuthorizationRequest { get; }

    /// <summary>
    ///     Creates a new instance of the <see cref="RequestObject" /> class.
    /// </summary>
    public static implicit operator JwtSecurityToken(RequestObject requestObject) => requestObject.Value;

    private RequestObject(JwtSecurityToken token, AuthorizationRequest authorizationRequest)
    {
        AuthorizationRequest = authorizationRequest;
        Value = token;
    }

    /// <summary>
    ///     Creates a new instance of the <see cref="RequestObject" /> class.
    /// </summary>
    public static Validation<AuthorizationRequestCancellation, RequestObject> CreateRequestObject(string requestObjectJson)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        JwtSecurityToken jwt;
        try
        {
            if (requestObjectJson.Split('.').Length == 2)
                requestObjectJson += ".";
        
            jwt = tokenHandler.ReadJwtToken(requestObjectJson);
        }
        catch (Exception e)
        {
            var error = new InvalidRequestError("The request object is not a valid JWT", e);
            return new AuthorizationRequestCancellation(Option<Uri>.None, [error]);
        }
        
        var json = jwt.Payload.SerializeToJson();
        
        return 
            from authRequest in CreateAuthorizationRequest(json)
            select new RequestObject(jwt, authRequest);
    }

    /// <summary>
    ///     Gets the authorization request from the request object.
    /// </summary>
    public AuthorizationRequest ToAuthorizationRequest() => AuthorizationRequest;
}

/// <summary>
///     Extension methods for <see cref="RequestObject" />.
/// </summary>
public static class RequestObjectExtensions
{
    /// <summary>
    ///     Validates the JWT signature.
    /// </summary>
    /// <returns>The validated request object.</returns>
    /// <exception cref="InvalidOperationException">Throws when validation fails</exception>
    public static RequestObject ValidateJwtSignature(this RequestObject requestObject)
    {
        var jwt = (JwtSecurityToken)requestObject;
        var pubKey = requestObject.GetLeafCertificate().GetPublicKey();
        
        return jwt.IsSignatureValid(pubKey)
            ? requestObject
            : throw new InvalidOperationException("Invalid JWT Signature");
    }

    /// <summary>
    ///     Validates the SAN name of the leaf certificate
    /// </summary>
    /// <returns>The validated request object</returns>
    /// <exception cref="InvalidOperationException">Throws when validation fails</exception>
    public static RequestObject ValidateSanName(this RequestObject requestObject)
    {
        var encoded = requestObject.GetLeafCertificate().GetEncoded();
        var x509Certificate = new X509Certificate2(encoded);
        
        return GetSanDnsNames(x509Certificate).Any(sanDnsName => requestObject.ClientId.EndsWith(sanDnsName.Split("*").Last()))
            ? requestObject
            : throw new InvalidOperationException("SAN does not match Client ID");
    }

    private static IEnumerable<string> GetSanDnsNames(X509Certificate2 certificate)
    {
        const string sanOid = "2.5.29.17";
        var sanNames = new List<string>();

        foreach (var extension in certificate.Extensions)
        {
            if (extension.Oid.Value != sanOid)
                continue;

            var sanExtension = (AsnEncodedData)extension;
            var sanData = sanExtension.Format(true);

            foreach (var line in sanData.Split(new[] { "\r\n", "\n", "," }, StringSplitOptions.RemoveEmptyEntries))
            {
                sanNames.Add(line.Split(':', '=').Last().Trim());
            }
        }

        return sanNames;
    }

    /// <summary>
    ///     Validates the trust chain of the leaf certificate
    /// </summary>
    /// <returns>The validated request object</returns>
    /// <exception cref="InvalidOperationException">Throws when validation fails</exception>
    public static RequestObject ValidateTrustChain(this RequestObject requestObject)
    {
        var certificates = requestObject.GetCertificates();
        if (certificates.Count == 1)
        {
            if (certificates.First().IsSelfSigned())
                return requestObject;
            else
                throw new InvalidOperationException("TrustChain must not consist of only one non-self-signed certificate");
        }
        else
        {
            if (certificates.IsTrustChainValid())
                return requestObject;
            else
                throw new InvalidOperationException("Validation of trust chain failed");
        }
    }

    internal static List<X509Certificate> GetCertificates(this RequestObject requestObject)
    {
        var x5C = ((JwtSecurityToken)requestObject).Header.X5c;
        return Parse(x5C).Select(
            certAsJToken =>
            {
                var certBytes = FromBase64String(certAsJToken.ToString());
                return new X509CertificateParser().ReadCertificate(certBytes);
            }).ToList();
    }

    internal static X509Certificate GetLeafCertificate(this RequestObject requestObject) =>
        GetCertificates(requestObject).First();
}
