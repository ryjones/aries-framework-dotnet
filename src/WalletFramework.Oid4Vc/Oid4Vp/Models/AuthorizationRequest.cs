using System.Security.Cryptography.X509Certificates;
using LanguageExt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WalletFramework.Core.Functional;
using WalletFramework.Core.Json;
using WalletFramework.Oid4Vc.Oid4Vp.Errors;
using WalletFramework.Oid4Vc.Oid4Vp.PresentationExchange.Models;
using WalletFramework.Oid4Vc.Oid4Vp.TransactionData;
using WalletFramework.Oid4Vc.Payment;
using static WalletFramework.Oid4Vc.Oid4Vp.Models.ClientIdScheme;

namespace WalletFramework.Oid4Vc.Oid4Vp.Models;

/// <summary>
///     Represents the Request of a Verifier to a Holder within the OpenId4VP specification.
/// </summary>
public record AuthorizationRequest
{
    public const string DirectPost = "direct_post";
    public const string DirectPostJwt = "direct_post.jwt";

    public static readonly string[] SupportedClientIdSchemes =
        [RedirectUriScheme, VerifierAttestationScheme, X509SanDnsScheme];

    private const string VpToken = "vp_token";

    /// <summary>
    ///     Gets the client id scheme.
    /// </summary>
    [JsonProperty("client_id_scheme")]
    public ClientIdScheme ClientIdScheme { get; }

    /// <summary>
    ///     Gets the presentation definition. Contains the claims that the Verifier wants to receive.
    /// </summary>
    [JsonProperty("presentation_definition")]
    public PresentationDefinition PresentationDefinition { get; }

    /// <summary>
    ///     Gets the client id. The Identifier of the Verifier.
    /// </summary>
    [JsonProperty("client_id")]
    public string ClientId { get; }

    /// <summary>
    ///     Gets the nonce. Random string for session binding.
    /// </summary>
    [JsonProperty("nonce")]
    public string Nonce { get; }

    /// <summary>
    ///     Gets the response mode. Determines where to send the Authorization Response to.
    /// </summary>
    [JsonProperty("response_uri")]
    public string ResponseUri { get; }
    
    [JsonProperty("response_mode")]
    public string ResponseMode { get; }

    /// <summary>
    ///     Gets the client metadata. Contains the Verifier metadata.
    /// </summary>
    [JsonProperty("client_metadata")]
    public ClientMetadata? ClientMetadata { get; init; }

    /// <summary>
    ///     Gets the client metadata uri. Can be used to retrieve the verifier metadata.
    /// </summary>
    [JsonProperty("client_metadata_uri")]
    public string? ClientMetadataUri { get; }

    /// <summary>
    ///     The scope of the request.
    /// </summary>
    [JsonProperty("scope")]
    public string? Scope { get; }

    /// <summary>
    ///     Gets the state.
    /// </summary>
    [JsonProperty("state")]
    public string? State { get; }

    [JsonIgnore]
    public Option<List<PaymentTransactionData>> TransactionData { get; init; } = 
        Option<List<PaymentTransactionData>>.None;

    /// <summary>
    ///     The X509 certificate of the verifier, this property is only set when ClientIDScheme is X509SanDNS.
    /// </summary>
    [JsonIgnore]
    public X509Certificate2? X509Certificate { get; init; }

    /// <summary>
    ///     The trust chain of the verifier, this property is only set when ClientIDScheme is X509SanDNS.
    /// </summary>
    [JsonIgnore]
    public X509Chain? X509TrustChain { get; init; }

    [JsonConstructor]
    private AuthorizationRequest(
        ClientIdScheme clientIdScheme,
        PresentationDefinition presentationDefinition,
        string clientId,
        string nonce,
        string responseUri,
        string responseMode,
        ClientMetadata? clientMetadata,
        string? clientMetadataUri,
        string? scope,
        string? state)
    {
        if (SupportedClientIdSchemes.Exists(supportedClientIdScheme => clientId.StartsWith($"{supportedClientIdScheme}:")))
        {
            ClientIdScheme = clientId.Split(':')[0];
            ClientId = clientId.Split(':')[1];
        }
        else
        {
            ClientId = clientId;
            ClientIdScheme = clientIdScheme;    
        }
        
        ClientMetadata = clientMetadata;
        ClientMetadataUri = clientMetadataUri;
        Nonce = nonce;
        PresentationDefinition = presentationDefinition;
        ResponseUri = responseUri;
        ResponseMode = responseMode;
        Scope = scope;
        State = state;
    }

    /// <summary>
    ///     Creates a new instance of the <see cref="AuthorizationRequest" /> class.
    /// </summary>
    /// <param name="authorizationRequestJson">The json representation of the authorization request.</param>
    /// <returns>A new instance of the <see cref="AuthorizationRequest" /> class.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the request does not match the HAIP.</exception>
    public static Validation<AuthorizationRequestCancellation, AuthorizationRequest> CreateAuthorizationRequest(
        string authorizationRequestJson)
    {
        JObject jObject;
        try
        {
            jObject = JObject.Parse(authorizationRequestJson);
        }
        catch (Exception e)
        {
            var responseUriOption = AuthorizationRequestExtensions.GetResponseUriMaybe(authorizationRequestJson);
            var error = new InvalidRequestError("The authorization request could not be parsed", e);
            return new AuthorizationRequestCancellation(responseUriOption, [error]);
        }

        try
        {
            return CreateAuthorizationRequest(jObject);
        }
        catch (Exception e)
        {
            var responseUriOption = AuthorizationRequestExtensions.GetResponseUriMaybe(authorizationRequestJson);
            var error = new InvalidRequestError("The authorization request could not be parsed", e);
            return new AuthorizationRequestCancellation(responseUriOption, [error]);
        }
    }

    private static Validation<AuthorizationRequestCancellation, AuthorizationRequest> CreateAuthorizationRequest(
        JObject authRequestJObject)
    {
        if (IsHaipConform(authRequestJObject))
        {
            var authRequestValidation = 
                authRequestJObject.ToObject<AuthorizationRequest>()
                ?? new InvalidRequestError("Could not parse the Authorization Request")
                    .ToInvalid<AuthorizationRequest>();
            
            var transactionDataPropertyFoundValidation =
                from jToken in authRequestJObject.GetByKey("transaction_data")
                from jObject in jToken.ToJArray()
                select jObject;
            
            var responseUriOption = AuthorizationRequestExtensions.GetResponseUriMaybe(authRequestJObject);

            return transactionDataPropertyFoundValidation.Match(
                jArray =>
                {
                    var transactionDataValidation = 
                        from transactionDataArray in TransactionDataArray.FromJObject(jArray)
                        from transactionDataEnum in transactionDataArray.Decode()
                        from authRequest in authRequestValidation
                        select authRequest with
                        {
                            TransactionData = transactionDataEnum.ToList()
                        };
            
                    return transactionDataValidation.Value.MapFail(error =>
                    {
                        var vpError = error as VpError ?? new InvalidRequestError("Could not parse the Authorization Request");
                        return new AuthorizationRequestCancellation(responseUriOption, [vpError]);
                    });
                },
                _ => authRequestValidation.Value.MapFail(error =>
                {
                    var vpError = error as VpError ?? new InvalidRequestError("Could not parse the Authorization Request");
                    return new AuthorizationRequestCancellation(responseUriOption, [vpError]);
                }));
        }
        else
        {
            var responseUriOption = AuthorizationRequestExtensions.GetResponseUriMaybe(authRequestJObject.ToString());
            var error = new InvalidRequestError("The authorization request does not match the HAIP");
            return new AuthorizationRequestCancellation(responseUriOption, [error]);
        }
    }

    private static bool IsHaipConform(JObject authorizationRequestJson)
    {
        var responseType = authorizationRequestJson["response_type"]!.ToString();
        var responseUri = authorizationRequestJson["response_uri"]!.ToString();
        var responseMode = authorizationRequestJson["response_mode"]!.ToString();
        var redirectUri = authorizationRequestJson["redirect_uri"];
        var authorizationRequestClientId = authorizationRequestJson["client_id"]!.ToString();

        string clientId;
        string clientIdScheme;
        if (SupportedClientIdSchemes.Exists(supportedClientIdScheme => authorizationRequestClientId.StartsWith($"{supportedClientIdScheme}:")))
        {
            clientIdScheme = authorizationRequestClientId.Split(':')[0]; 
            clientId = authorizationRequestClientId.Split(':')[1];
        }
        else
        {
            clientIdScheme = authorizationRequestJson["client_id_scheme"]!.ToString();
            clientId = authorizationRequestClientId;    
        }

        return
            responseType == VpToken
            && responseMode == DirectPost || responseMode == DirectPostJwt
            && !string.IsNullOrEmpty(responseUri)
            && redirectUri is null
            && (clientIdScheme is X509SanDnsScheme or VerifierAttestationScheme
                || (clientIdScheme is RedirectUriScheme && clientId == responseUri));
    }
}

internal static class AuthorizationRequestExtensions
{
    internal static AuthorizationRequest WithX509(
        this AuthorizationRequest authorizationRequest,
        RequestObject requestObject)
    {
        var encodedCertificate = requestObject.GetLeafCertificate().GetEncoded();

        var certificates =
            requestObject
                .GetCertificates()
                .Select(x => x.GetEncoded())
                .Select(x => new X509Certificate2(x));

        var trustChain = new X509Chain();
        foreach (var element in certificates)
        {
            trustChain.ChainPolicy.ExtraStore.Add(element);
        }

        return authorizationRequest with
        {
            X509Certificate = new X509Certificate2(encodedCertificate),
            X509TrustChain = trustChain
        };
    }
        
    internal static AuthorizationRequest WithClientMetadata(
        this AuthorizationRequest authorizationRequest,
        Option<ClientMetadata> clientMetadata) 
        => authorizationRequest with { ClientMetadata = clientMetadata.ToNullable() };
    
    internal static Option<Uri> GetResponseUriMaybe(string authRequestJson)
    {
        try
        {
            var jObject = JObject.Parse(authRequestJson);
            return GetResponseUriMaybe(jObject);
        }
        catch (Exception)
        {
            return Option<Uri>.None;
        }
    }
    
    internal static Option<Uri> GetResponseUriMaybe(JObject authRequestJObject)
    {
        try
        {
            var responseUri = authRequestJObject["response_uri"]!.ToString();
            return new Uri(responseUri);
        }
        catch (Exception)
        {
            return Option<Uri>.None;
        }
    }
}

public static class AuthorizationRequestFun
{
    public static Option<Uri> GetResponseUriMaybe(this AuthorizationRequest authRequest)
    {
        try
        {
            return new Uri(authRequest.ResponseUri);
        }
        catch (Exception)
        {
            return Option<Uri>.None;
        }
    }
}
