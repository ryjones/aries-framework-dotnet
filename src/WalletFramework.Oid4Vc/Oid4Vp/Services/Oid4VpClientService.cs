using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Extensions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using SD_JWT.Models;
using WalletFramework.Core.Credentials;
using WalletFramework.Oid4Vc.ClientAttestation;
using WalletFramework.Core.Credentials.Abstractions;
using WalletFramework.Core.Functional;
using WalletFramework.MdocLib;
using WalletFramework.MdocLib.Device;
using WalletFramework.MdocLib.Device.Abstractions;
using WalletFramework.MdocLib.Device.Response;
using WalletFramework.MdocLib.Elements;
using WalletFramework.MdocLib.Security;
using WalletFramework.MdocLib.Security.Cose;
using WalletFramework.MdocVc;
using WalletFramework.Oid4Vc.Errors;
using WalletFramework.Oid4Vc.Oid4Vci.Abstractions;
using WalletFramework.Oid4Vc.Oid4Vci.AuthFlow.Abstractions;
using WalletFramework.Oid4Vc.Oid4Vci.AuthFlow.Models;
using WalletFramework.Oid4Vc.Oid4Vci.CredConfiguration.Models;
using WalletFramework.Oid4Vc.Oid4Vci.Extensions;
using WalletFramework.Oid4Vc.Oid4Vci.Implementations;
using WalletFramework.Oid4Vc.Oid4Vp.AuthResponse.Encryption;
using WalletFramework.Oid4Vc.Oid4Vp.Errors;
using WalletFramework.Oid4Vc.Oid4Vp.Models;
using WalletFramework.Oid4Vc.Oid4Vp.PresentationExchange.Models;
using WalletFramework.Oid4Vc.Oid4Vp.PresentationExchange.Services;
using WalletFramework.Oid4Vc.Oid4Vp.TransactionData;
using WalletFramework.SdJwtVc.Models;
using WalletFramework.SdJwtVc.Models.Records;
using WalletFramework.SdJwtVc.Services.SdJwtVcHolderService;
using static Newtonsoft.Json.JsonConvert;
using static WalletFramework.MdocLib.Security.Cose.ProtectedHeaders;
using Format = WalletFramework.Oid4Vc.Oid4Vci.CredConfiguration.Models.Format;

namespace WalletFramework.Oid4Vc.Oid4Vp.Services;

/// <inheritdoc />
public class Oid4VpClientService : IOid4VpClientService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Oid4VpClientService" /> class.
    /// </summary>
    /// <param name="agentProvider">The agent provider</param>
    /// <param name="authorizationRequestService">The authorization request service.</param>
    /// <param name="httpClientFactory">The http client factory to create http clients.</param>
    /// <param name="sdJwtVcHolderService">The service responsible for SD-JWT related operations.</param>
    /// <param name="pexService">The Presentation Exchange service.</param>
    /// <param name="mdocAuthenticationService">The mdoc authentication service.</param>
    /// <param name="oid4VpHaipClient">The service responsible for OpenId4VP related operations.</param>
    /// <param name="logger">The ILogger.</param>
    /// <param name="authFlowSessionStorage">The Auth Flow Session Storage.</param>
    /// <param name="oid4VpRecordService">The service responsible for OidPresentationRecord related operations.</param>
    /// <param name="mDocStorage">The service responsible for mDOc storage operations.</param> 
    public Oid4VpClientService(
        IAgentProvider agentProvider,
        IAuthorizationRequestService authorizationRequestService,
        IHttpClientFactory httpClientFactory,
        ILogger<Oid4VpClientService> logger,
        IMdocAuthenticationService mdocAuthenticationService,
        IOid4VpHaipClient oid4VpHaipClient,
        IOid4VpRecordService oid4VpRecordService,
        IMdocStorage mDocStorage,
        IPexService pexService,
        IAuthFlowSessionStorage authFlowSessionStorage,
        ISdJwtVcHolderService sdJwtVcHolderService)
    {
        _agentProvider = agentProvider;
        _authorizationRequestService = authorizationRequestService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _mdocAuthenticationService = mdocAuthenticationService;
        _oid4VpHaipClient = oid4VpHaipClient;
        _oid4VpRecordService = oid4VpRecordService;
        _mDocStorage = mDocStorage;
        _pexService = pexService;
        _authFlowSessionStorage = authFlowSessionStorage;
        _sdJwtVcHolderService = sdJwtVcHolderService;
    }

    private readonly IAgentProvider _agentProvider;
    private readonly IAuthorizationRequestService _authorizationRequestService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<Oid4VpClientService> _logger;
    private readonly IMdocAuthenticationService _mdocAuthenticationService;
    private readonly IOid4VpHaipClient _oid4VpHaipClient;
    private readonly IOid4VpRecordService _oid4VpRecordService;
    private readonly IMdocStorage _mDocStorage;
    private readonly IPexService _pexService;
    private readonly IAuthFlowSessionStorage _authFlowSessionStorage;
    private readonly ISdJwtVcHolderService _sdJwtVcHolderService;

    public async Task<Validation<AuthorizationRequestCancellation, AuthorizationRequestCandidates>> ProcessAuthorizationRequestUri(
        AuthorizationRequestUri requestUri)
    {
        var authorizationRequestValidation = await _authorizationRequestService.GetAuthorizationRequest(requestUri);
        var result = authorizationRequestValidation.Map(async authRequest =>
        {
            var candidates = await _pexService.FindCredentialCandidates(
                authRequest.PresentationDefinition.InputDescriptors,
                authRequest.ClientMetadata?.Formats);

            var candidatesList = candidates.ToList();

            var candidatesOption = candidatesList.Count == 0
                ? Option<List<PresentationCandidates>>.None
                : candidatesList;

            return new AuthorizationRequestCandidates(authRequest, candidatesOption);
        });

        return await result.Traverse(candidates => candidates);
    }

    public async Task<Option<Uri>> AcceptAuthorizationRequest(
        AuthorizationRequest authorizationRequest,
        IEnumerable<SelectedCredential> selectedCredentials,
        CombinedWalletAttestation? clientAttestation = null)
    {
        var credentials = selectedCredentials.ToList();
        
        var inputDescriptors = authorizationRequest
            .PresentationDefinition
            .InputDescriptors;
        
        var mdocNonce = Option<Nonce>.None;

        var presentationMapTasks = credentials.Select(async credential =>
        {
            var inputDescriptor = inputDescriptors.Single(descriptor => 
                descriptor.Id == credential.InputDescriptorId);

            var claims =
                from field in inputDescriptor.Constraints.Fields
                from path in field.Path.Select(path => path.TrimStart('$', '.'))
                select path;

            Format format;
            ICredential presentedCredential;

            string presentation;
            switch (credential.Credential)
            {
                case SdJwtRecord sdJwt:
                    format = FormatFun.CreateSdJwtFormat();

                    var transactionDataHashesOption = authorizationRequest.TransactionData.OnSome(
                        list => list.Select(data => data.Hash().AsString));
                    
                    presentation = await _sdJwtVcHolderService.CreatePresentation(
                        sdJwt,
                        claims.ToArray(),
                        transactionDataHashesOption,
                        authorizationRequest.ClientId,
                        authorizationRequest.Nonce);

                    presentedCredential = sdJwt;
                    break;
                case MdocRecord mdocRecord:
                    format = FormatFun.CreateMdocFormat();
                    
                    var toDisclose = claims.Select(claim =>
                    {
                        // TODO: This is needed because in mdoc the requested attributes look like this: $['Namespace']['ElementId']. Refactor this more clean
                        var keys = claim.Split(["['", "']"], StringSplitOptions.RemoveEmptyEntries);
                        
                        var nameSpace = NameSpace.ValidNameSpace(keys[0]).UnwrapOrThrow();
                        var elementId = ElementIdentifier
                            .ValidElementIdentifier(keys[1])
                            .UnwrapOrThrow();

                        return (NameSpace: nameSpace, ElementId: elementId);
                    })
                    .GroupBy(nameSpaceAndElementId => nameSpaceAndElementId.NameSpace, tuple => tuple.ElementId)
                    .ToDictionary(group => group.Key, group => group.ToList());
                    
                    var mdoc = mdocRecord.Mdoc.SelectivelyDisclose(toDisclose);

                    var handover = authorizationRequest.ToVpHandover();
                    mdocNonce = handover.MdocGeneratedNonce;
                    var sessionTranscript = handover.ToSessionTranscript();
                    var authenticatedMdoc = await _mdocAuthenticationService.Authenticate(
                        mdoc, sessionTranscript, mdocRecord.KeyId);
                    
                    presentation = new Document(authenticatedMdoc).BuildDeviceResponse().EncodeToBase64Url();

                    presentedCredential = mdocRecord;
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(credential.Credential));
            }

            return (InputDescriptorId: inputDescriptor.Id, Presentation: presentation, Format: format, PresentedCredential: presentedCredential);
        });

        var presentationMaps = new List<(string InputDescriptorId, string Presentation, Format Format, ICredential PresentedCredential)>();
        foreach (var task in presentationMapTasks)
        {
            var presentationMap = await task;
            presentationMaps.Add(presentationMap);
        }

        var authorizationResponse = await _oid4VpHaipClient.CreateAuthorizationResponseAsync(
            authorizationRequest,
            presentationMaps.Select(tuple => (tuple.InputDescriptorId, tuple.Presentation, tuple.Format)).ToArray()
        );

        var content = authorizationRequest.ResponseMode switch
        {
            AuthorizationRequest.DirectPost => authorizationResponse.ToFormUrl(),
            AuthorizationRequest.DirectPostJwt => authorizationResponse.Encrypt(authorizationRequest, mdocNonce).ToFormUrl(),
            _ => throw new ArgumentOutOfRangeException(nameof(authorizationRequest.ResponseMode))
        };

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(authorizationRequest.ResponseUri),
            Method = HttpMethod.Post,
            Content = content
        };
        
        // TODO: Introduce timeout
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Clear();
        if (clientAttestation != null)
            httpClient.AddClientAttestationPopHeader(clientAttestation);
        
        // ToDo: when to delete these records?
        var context = await _agentProvider.GetContextAsync();
        foreach (var credential in credentials)
        {
            switch (credential.Credential)
            {
                case SdJwtRecord { OneTimeUse: true } sdJwtRecord:
                    await _sdJwtVcHolderService.DeleteAsync(context, sdJwtRecord.GetId());
                    break;
                case MdocRecord { OneTimeUse: true } mDocRecord:
                    await _mDocStorage.Delete(mDocRecord);
                    break;
            }
        }
        
        var responseMessage = await httpClient.SendAsync(message);
        if (!responseMessage.IsSuccessStatusCode)
        {
            var str = await responseMessage.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Authorization Response failed with message {str}");
        }
        
        var presentedCredentials = presentationMaps.Select(presentationMap =>
        {
            PresentedCredentialSet result;
            
            switch (presentationMap.PresentedCredential)
            {
                case SdJwtRecord sdJwtRecord:
                    var issuanceSdJwtDoc = sdJwtRecord.ToSdJwtDoc();
                    var presentation = new SdJwtDoc(presentationMap.Presentation);
                    
                    var nonDisclosed =
                        from disclosure in issuanceSdJwtDoc.Disclosures
                        let base64Encoded = disclosure.Base64UrlEncoded
                        where presentation.Disclosures.All(itm => itm.Base64UrlEncoded != base64Encoded)
                        select disclosure;
                    
                    var presentedClaims = 
                        from claim in sdJwtRecord.Claims
                        where !nonDisclosed.Any(nd => claim.Key.StartsWith(nd.Path ?? string.Empty))
                        select new
                        {
                            key = claim.Key, 
                            value = new PresentedClaim { Value = claim.Value }  
                        };
        
                    result = new PresentedCredentialSet
                    {
                        SdJwtCredentialType = Vct.ValidVct(sdJwtRecord.Vct).UnwrapOrThrow(),
                        CredentialSetId = CredentialSetId.ValidCredentialSetId(sdJwtRecord.CredentialSetId).UnwrapOrThrow(),
                        PresentedClaims = presentedClaims.ToDictionary(itm => itm.key, itm => itm.value)
                    };
                    break;
                case MdocRecord mdocRecord:
                    var claims = mdocRecord.Mdoc.IssuerSigned.IssuerNameSpaces.Value.SelectMany(pair => pair.Value);

                    result = new PresentedCredentialSet
                    {
                        MDocCredentialType = mdocRecord.DocType,
                        CredentialSetId = mdocRecord.GetCredentialSetId(),
                        PresentedClaims = claims.ToDictionary(
                            item => item.ElementId.ToString(),
                            item => new PresentedClaim { Value = item.Element.ToString() }
                        )
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(presentationMap.PresentedCredential));
            }

            return result;
        });

        var oidPresentationRecord = new OidPresentationRecord
        {
            Id = Guid.NewGuid().ToString(),
            ClientId = authorizationRequest.ClientId,
            ClientMetadata = authorizationRequest.ClientMetadata,
            Name = authorizationRequest.PresentationDefinition.Name,
            PresentedCredentialSets = presentedCredentials.ToList()
        };
        
        await _oid4VpRecordService.StoreAsync(context, oidPresentationRecord);

        var redirectUriJson = await responseMessage.Content.ReadAsStringAsync();

        try
        {
            Uri callback = DeserializeObject<AuthorizationResponseCallback>(redirectUriJson);
            return callback;
        }
        catch (Exception e)
        {
            _logger.LogWarning(
                e, 
                "Could not parse Redirect URI received from: {ResponseUri}", authorizationRequest.ResponseUri);
            
            return null;
        }
    }

    public async Task<Option<Uri>> AbortAuthorizationRequest(AuthorizationRequestCancellation cancellation)
    {
        var callbackTaskOption = cancellation.ResponseUri.OnSome(
            async uri =>
            {
                var error = cancellation.Errors.First();
                
                var message = new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = error.ToResponse().ToFormUrlContent()
                };
        
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Clear();
                
                using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(3)); 
                var responseMessage = await httpClient.SendAsync(message, cancellationSource.Token);
                if (!responseMessage.IsSuccessStatusCode)
                {
                    var str = await responseMessage.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Authorization Error Response failed with message {str}");
                }
        
                var redirectUriJson = await responseMessage.Content.ReadAsStringAsync();
                var callback = DeserializeObject<AuthorizationResponseCallback>(redirectUriJson);
                return callback?.ToUri() ?? Option<Uri>.None;
            });
        
        var callbackUriOption = await callbackTaskOption.Traverse(uri => uri);
        return callbackUriOption.Flatten();
    }

    /// <inheritdoc />
    public async Task<Option<PresentationCandidates>> FindCredentialCandidateForInputDescriptorAsync(InputDescriptor inputDescriptor)
    {
        var candidates = await _pexService.FindCredentialCandidates(
            [inputDescriptor],
            Option<Formats>.None);
        
        var list = candidates.ToList();
        return list.Any() 
            ? list.First() 
            : Option<PresentationCandidates>.None;
    }
    
    //TODO: Refactor this C'' method into current flows (too much duplicate code)
    public async Task<Option<Uri>> AcceptOnDemandRequest(
        AuthorizationRequest authorizationRequest,
        IEnumerable<SelectedCredential> selectedCredentials,
        IssuanceSession issuanceSession)
    {
        var credentials = selectedCredentials.ToList();
        
        var inputDescriptors = authorizationRequest
            .PresentationDefinition
            .InputDescriptors;
        
        var mdocNonce = Option<Nonce>.None;

        var presentationMapTasks = credentials.Select(async credential =>
        {
            var inputDescriptor = inputDescriptors.Single(descriptor => 
                descriptor.Id == credential.InputDescriptorId);

            var claims =
                from field in inputDescriptor.Constraints.Fields
                from path in field.Path.Select(path => path.TrimStart('$', '.'))
                select path;

            Format format;
            ICredential presentedCredential;

            var context = await _agentProvider.GetContextAsync();
            var session = await _authFlowSessionStorage.GetAsync(context, issuanceSession.AuthFlowSessionState);
            
            var client = _httpClientFactory.CreateClient();
            client.WithAuthorizationHeader(session.AuthorizationData.OAuthToken.UnwrapOrThrow(new Exception()));
            
            var sha256 = SHA256.Create();
            
            var presentation = string.Empty;
            switch (credential.Credential)
            {
                case SdJwtRecord sdJwt:
                    format = FormatFun.CreateSdJwtFormat();
                    
                    presentation = await _sdJwtVcHolderService.CreatePresentation(
                        sdJwt,
                        claims.ToArray(),
                        Option<IEnumerable<string>>.None,
                        authorizationRequest.ClientId,
                        authorizationRequest.Nonce);

                    var kbJwt = presentation[presentation.LastIndexOf('~')..][1..];
                    var kbJwtWithoutSignature = kbJwt[..kbJwt.LastIndexOf('.')];
                    
                    var kbJwtWithoutSignatureHash = sha256.ComputeHash(kbJwtWithoutSignature.GetUTF8Bytes());
                    
                    var content = new JObject();
                    content.Add("hash_bytes", Base64UrlEncoder.Encode(kbJwtWithoutSignatureHash));

                    var sdJwtHttpContent =
                        new StringContent
                        (
                            content.ToString(),
                            Encoding.UTF8,
                            MediaTypeNames.Application.Json
                        );
                    
                    var sdJwtSignatureResponse = await client.PostAsync(
                        session.AuthorizationData.IssuerMetadata.PresentationSigningEndpoint.UnwrapOrThrow(new Exception()), 
                        sdJwtHttpContent
                        );

                    if (sdJwtSignatureResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await sdJwtSignatureResponse.Content.ReadAsStringAsync();
                        var signatureBytes = JObject.Parse(responseContent)["signature_bytes"]?.ToString();

                        presentation = $"{presentation[..presentation.LastIndexOf('.')]}.{signatureBytes}";
                    }
                    
                    presentedCredential = sdJwt;
                    break;
                case MdocRecord mdocRecord:
                    format = FormatFun.CreateMdocFormat();
                    
                    var toDisclose = claims.Select(claim =>
                    {
                        // TODO: This is needed because in mdoc the requested attributes look like this: $[Namespace][ElementId]. Refactor this more clean
                        var keys = claim.Split(new[] { "['", "']" }, StringSplitOptions.RemoveEmptyEntries);
                        
                        var nameSpace = NameSpace.ValidNameSpace(keys[0]).UnwrapOrThrow();
                        var elementId = ElementIdentifier
                            .ValidElementIdentifier(keys[1])
                            .UnwrapOrThrow();

                        return (NameSpace: nameSpace, ElementId: elementId);
                    })
                    .GroupBy(nameSpaceAndElementId => nameSpaceAndElementId.NameSpace, tuple => tuple.ElementId)
                    .ToDictionary(group => group.Key, group => group.ToList());
                    
                    var mdoc = mdocRecord.Mdoc.SelectivelyDisclose(toDisclose);

                    var handover = authorizationRequest.ToVpHandover();
                    mdocNonce = handover.MdocGeneratedNonce;
                    var sessionTranscript = handover.ToSessionTranscript();

                    var deviceNamespaces =
                        from keyAuths in mdoc.IssuerSigned.IssuerAuth.Payload.DeviceKeyInfo.KeyAuthorizations
                        select keyAuths.ToDeviceNameSpaces();
                    
                    var deviceAuthentication = new DeviceAuthentication(
                        sessionTranscript, mdoc.DocType, deviceNamespaces);
                    
                    var sigStructure = new SigStructure(deviceAuthentication.ToCbor(), mdoc.IssuerSigned.IssuerAuth.ProtectedHeaders);
                    
                    var sigStructureByteString = sigStructure.ToCbor();
                    
                    var sigStructureHash = sha256.ComputeHash(sigStructureByteString.EncodeToBytes());
                    
                    var mDocPostContent = new JObject();
                    mDocPostContent.Add("hash_bytes", Base64UrlEncoder.Encode(sigStructureHash));

                    var mDocHttpContent =
                        new StringContent
                        (
                            mDocPostContent.ToString(),
                            Encoding.UTF8,
                            MediaTypeNames.Application.Json
                        );
                    
                    var mDocSignatureResponse = await client.PostAsync(
                        session.AuthorizationData.IssuerMetadata.PresentationSigningEndpoint.UnwrapOrThrow(new Exception()), 
                        mDocHttpContent
                        );

                    if (mDocSignatureResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await mDocSignatureResponse.Content.ReadAsStringAsync();
                        var signatureBytes = JObject.Parse(responseContent)["signature_bytes"]?.ToString();
                        
                        var coseSignature = new CoseSignature(Base64UrlEncoder.DecodeBytes(signatureBytes));
                        
                        var deviceSigned = new DeviceSignature(BuildProtectedHeaders(), coseSignature)
                            .ToDeviceSigned(deviceNamespaces);
                        
                        presentation = new Document(new AuthenticatedMdoc(mdoc, deviceSigned)).BuildDeviceResponse().EncodeToBase64Url();
                    }
                    
                    presentedCredential = mdocRecord;
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(credential.Credential));
            }

            return (InputDescriptorId: inputDescriptor.Id, Presentation: presentation, Format: format, PresentedCredential: presentedCredential);
        });

        var presentationMaps = new List<(string InputDescriptorId, string Presentation, Format Format, ICredential PresentedCredential)>();
        foreach (var task in presentationMapTasks)
        {
            var presentationMap = await task;
            presentationMaps.Add(presentationMap);
        }

        var authorizationResponse = await _oid4VpHaipClient.CreateAuthorizationResponseAsync(
            authorizationRequest,
            presentationMaps.Select(tuple => (tuple.InputDescriptorId, tuple.Presentation, tuple.Format)).ToArray()
        );

        var content = authorizationRequest.ResponseMode switch
        {
            AuthorizationRequest.DirectPost => authorizationResponse.ToFormUrl(),
            AuthorizationRequest.DirectPostJwt => authorizationResponse.Encrypt(authorizationRequest, mdocNonce).ToFormUrl(),
            _ => throw new ArgumentOutOfRangeException(nameof(authorizationRequest.ResponseMode))
        };

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(authorizationRequest.ResponseUri),
            Method = HttpMethod.Post,
            Content = content
        };
        
        // TODO: Introduce timeout
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Clear();
        var responseMessage = await httpClient.SendAsync(message);
        if (!responseMessage.IsSuccessStatusCode)
        {
            var str = await responseMessage.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Authorization Response failed with message {str}");
        }
        
        var presentedCredentials = presentationMaps.Select(presentationMap =>
        {
            PresentedCredentialSet result;
            
            switch (presentationMap.PresentedCredential)
            {
                case SdJwtRecord sdJwtRecord:
                    var issuanceSdJwtDoc = sdJwtRecord.ToSdJwtDoc();
                    var presentation = new SdJwtDoc(presentationMap.Presentation);
                    
                    var nonDisclosed =
                        from disclosure in issuanceSdJwtDoc.Disclosures
                        let base64Encoded = disclosure.Base64UrlEncoded
                        where presentation.Disclosures.All(itm => itm.Base64UrlEncoded != base64Encoded)
                        select disclosure;
                    
                    var presentedClaims = 
                        from claim in sdJwtRecord.Claims
                        where !nonDisclosed.Any(nd => claim.Key.StartsWith(nd.Path ?? string.Empty))
                        select new
                        {
                            key = claim.Key, 
                            value = new PresentedClaim { Value = claim.Value }  
                        };
        
                    result = new PresentedCredentialSet
                    {
                        SdJwtCredentialType = Vct.ValidVct(sdJwtRecord.Vct).UnwrapOrThrow(),
                        CredentialSetId = CredentialSetId.ValidCredentialSetId(sdJwtRecord.CredentialSetId).UnwrapOrThrow(),
                        PresentedClaims = presentedClaims.ToDictionary(itm => itm.key, itm => itm.value)
                    };
                    break;
                case MdocRecord mdocRecord:
                    var claims = mdocRecord.Mdoc.IssuerSigned.IssuerNameSpaces.Value.SelectMany(pair => pair.Value);

                    result = new PresentedCredentialSet
                    {
                        MDocCredentialType = mdocRecord.DocType,
                        CredentialSetId = mdocRecord.GetCredentialSetId(),
                        PresentedClaims = claims.ToDictionary(
                            item => item.ElementId.ToString(),
                            item => new PresentedClaim { Value = item.Element.ToString() }
                        )
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(presentationMap.PresentedCredential));
            }

            return result;
        });

        var context = await _agentProvider.GetContextAsync();
        
        var oidPresentationRecord = new OidPresentationRecord
        {
            Id = Guid.NewGuid().ToString(),
            ClientId = authorizationRequest.ClientId,
            ClientMetadata = authorizationRequest.ClientMetadata,
            Name = authorizationRequest.PresentationDefinition.Name,
            PresentedCredentialSets = presentedCredentials.ToList()
        };
        
        await _oid4VpRecordService.StoreAsync(context, oidPresentationRecord);

        var redirectUriJson = await responseMessage.Content.ReadAsStringAsync();

        try
        {
            Uri callbackUri = DeserializeObject<AuthorizationResponseCallback>(redirectUriJson);
            return callbackUri;
        }
        catch (Exception e)
        {
            _logger.LogWarning(
                e, 
                "Could not parse Redirect URI received from: {ResponseUri}", authorizationRequest.ResponseUri);
            
            return null;
        }
    }
}
