using System.Text;
using LanguageExt;
using OneOf;
using WalletFramework.Core.Cryptography.Abstractions;
using WalletFramework.Core.Cryptography.Models;
using WalletFramework.Core.Functional;
using WalletFramework.Core.Json;
using WalletFramework.Core.Uri;
using WalletFramework.MdocLib.Security;
using WalletFramework.Oid4Vc.Oid4Vci.AuthFlow.Models;
using WalletFramework.Oid4Vc.Oid4Vci.Authorization.DPop.Abstractions;
using WalletFramework.Oid4Vc.Oid4Vci.Authorization.DPop.Models;
using WalletFramework.Oid4Vc.Oid4Vci.Authorization.Models;
using WalletFramework.Oid4Vc.Oid4Vci.CredConfiguration.Models;
using WalletFramework.Oid4Vc.Oid4Vci.CredConfiguration.Models.Mdoc;
using WalletFramework.Oid4Vc.Oid4Vci.CredConfiguration.Models.SdJwt;
using WalletFramework.Oid4Vc.Oid4Vci.CredRequest.Abstractions;
using WalletFramework.Oid4Vc.Oid4Vci.CredRequest.Models;
using WalletFramework.Oid4Vc.Oid4Vci.CredRequest.Models.Mdoc;
using WalletFramework.Oid4Vc.Oid4Vci.Extensions;
using WalletFramework.Oid4Vc.Oid4Vci.Issuer.Models;
using WalletFramework.Oid4Vc.Oid4Vci.CredRequest.Models.SdJwt;
using WalletFramework.Oid4Vc.Oid4Vci.CredResponse;
using WalletFramework.Oid4Vc.Oid4Vp.Models;
using WalletFramework.SdJwtVc.Services.SdJwtVcHolderService;

namespace WalletFramework.Oid4Vc.Oid4Vci.CredRequest.Implementations;

public class CredentialRequestService : ICredentialRequestService
{
    private const int MaxBatchSize = 20;
    
    public CredentialRequestService(
        HttpClient httpClient,
        IDPopHttpClient dPopHttpClient,
        ISdJwtSigner sdJwtSigner,
        IKeyStore keyStore)
    {
        _dPopHttpClient = dPopHttpClient;
        _sdJwtSigner = sdJwtSigner;
        _keyStore = keyStore;
        _httpClient = httpClient;
    }

    private readonly HttpClient _httpClient;
    private readonly IDPopHttpClient _dPopHttpClient;
    private readonly ISdJwtSigner _sdJwtSigner;
    private readonly IKeyStore _keyStore;

    private async Task<CredentialRequest> CreateCredentialRequest(
        KeyId keyId,
        Format format,
        OneOf<OAuthToken, DPopToken> token,
        IssuerMetadata issuerMetadata,
        Option<ClientOptions> clientOptions,
        Option<AuthorizationRequest> authorizationRequest)
    {
        var cNonce = token.Match(
            oauthToken => oauthToken.CNonce,
            dPopToken => dPopToken.Token.CNonce);

        var proof = Option<ProofOfPossession>.None;
        var proofs = Option<ProofsOfPossession>.None;
        var sessionTranscript = Option<SessionTranscript>.None;

        await authorizationRequest.Match(
            Some: _ =>
            {
                if (format == "mso_mdoc")
                    sessionTranscript = authorizationRequest.UnwrapOrThrow(new Exception()).ToVpHandover()
                        .ToSessionTranscript();
                return Task.CompletedTask;
            },
            None: async () =>
            {
                await issuerMetadata.BatchCredentialIssuance.Match(
                    Some: async batchCredentialIssuance =>
                    {
                        await batchCredentialIssuance.BatchSize.Match(
                            Some: async batchSize =>
                            {
                                proofs = await GetProofsOfPossessionAsync(Math.Min(MaxBatchSize, batchSize), keyId,
                                    issuerMetadata, cNonce, clientOptions);
                            },
                            None: async () =>
                            {
                                proof = await GetProofOfPossessionAsync(keyId, issuerMetadata, cNonce, clientOptions);
                            });
                    },
                    None: async () =>
                        proof = await GetProofOfPossessionAsync(keyId, issuerMetadata, cNonce, clientOptions));
            });

        return new CredentialRequest(format, proof, proofs, sessionTranscript);
    }

    private async Task<ProofOfPossession> GetProofOfPossessionAsync(KeyId keyId, IssuerMetadata issuerMetadata, string cNonce, Option<ClientOptions> clientOptions)
    {
        return new ProofOfPossession
        {
            ProofType = "jwt",
            Jwt = await GenerateKbProofOfPossession(keyId, issuerMetadata, cNonce, clientOptions)
        };
    }
    
    private async Task<ProofsOfPossession> GetProofsOfPossessionAsync(int batchSize, KeyId keyId, IssuerMetadata issuerMetadata, string cNonce, Option<ClientOptions> clientOptions)
    {
        var jwts = new List<string>();
        for(var i = 0; i < batchSize; i++)
        {
            jwts.Add(await GenerateKbProofOfPossession(keyId, issuerMetadata, cNonce, clientOptions));
        }
        
        return new ProofsOfPossession("jwt", jwts.ToArray());
    }

    private async Task<string> GenerateKbProofOfPossession(
        KeyId keyId,
        IssuerMetadata issuerMetadata,
        string cNonce,
        Option<ClientOptions> clientOptions)
    {
        return await _sdJwtSigner.GenerateKbProofOfPossessionAsync(
            keyId,
            issuerMetadata.CredentialIssuer.ToString(),
            cNonce,
            "openid4vci-proof+jwt",
            null,
            clientOptions.ToNullable()?.ClientId,
            Option<IEnumerable<string>>.None);
    }

    async Task<Validation<CredentialResponse>> ICredentialRequestService.RequestCredentials(
        OneOf<SdJwtConfiguration, MdocConfiguration> configuration,
        IssuerMetadata issuerMetadata,
        OneOf<OAuthToken, DPopToken> token,
        Option<ClientOptions> clientOptions,
        Option<AuthorizationRequest> authorizationRequest)
    {
        var keyId = await _keyStore.GenerateKey(isPermanent: authorizationRequest.IsNone);

        var requestJson = await configuration.Match(
            async sdJwt =>
            {
                var vciRequest = await CreateCredentialRequest(
                    keyId,
                    sdJwt.Format,
                    token,
                    issuerMetadata,
                    clientOptions,
                    authorizationRequest);
                
                var result = new SdJwtCredentialRequest(vciRequest, sdJwt.Vct);
                return result.EncodeToJson();
            },
            async mdoc =>
            {
                var vciRequest = await CreateCredentialRequest(
                    keyId,
                    mdoc.Format,
                    token,
                    issuerMetadata,
                    clientOptions,
                    authorizationRequest);
                
                var result = new MdocCredentialRequest(vciRequest, mdoc);
                return result.EncodeToJson();
            }
        );

        var content = new StringContent(
            requestJson,
            Encoding.UTF8,
            "application/json");

        var response = await token.Match(
            async authToken => await _httpClient
                .WithAuthorizationHeader(authToken)
                .PostAsync(issuerMetadata.CredentialEndpoint, content),
            async dPopToken =>
            {
                var config = dPopToken.DPop.Config with
                {
                    Audience = issuerMetadata.CredentialEndpoint.ToStringWithoutTrail(),
                    OAuthToken = dPopToken.Token
                };

                var dPopResponse = await _dPopHttpClient.Post(
                    issuerMetadata.CredentialEndpoint,
                    config,
                    () => content);

                return dPopResponse.ResponseMessage;
            });

        var responseContent = await response.Content.ReadAsStringAsync();

        return 
            from jObject in JsonFun.ParseAsJObject(responseContent)
            from credResponse in CredentialResponse.ValidCredentialResponse(jObject, keyId)
            select credResponse;
    }
}
