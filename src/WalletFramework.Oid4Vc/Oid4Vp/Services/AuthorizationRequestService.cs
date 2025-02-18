using System.Net.Http.Headers;
using System.Web;
using LanguageExt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WalletFramework.Core.Functional;
using WalletFramework.Oid4Vc.Oid4Vp.Errors;
using WalletFramework.Oid4Vc.Oid4Vp.Models;
using static WalletFramework.Oid4Vc.Oid4Vp.Models.RequestObject;
using static WalletFramework.Oid4Vc.Oid4Vp.Models.ClientIdScheme.ClientIdSchemeValue;
using static Newtonsoft.Json.JsonConvert;
using static WalletFramework.Oid4Vc.Oid4Vp.Models.AuthorizationRequest;

namespace WalletFramework.Oid4Vc.Oid4Vp.Services;

public class AuthorizationRequestService(IHttpClientFactory httpClientFactory) : IAuthorizationRequestService
{
    public async Task<Validation<AuthorizationRequestCancellation, AuthorizationRequest>> GetAuthorizationRequest(AuthorizationRequestUri authorizationRequestUri)
    {
        return await authorizationRequestUri.Value.Match(
            async authRequestByReference => 
                await GetAuthRequestByReference(authRequestByReference),
            async authRequestByValue => 
                await GetAuthRequestByValue(authRequestByValue));
    }

    private async Task<Validation<AuthorizationRequestCancellation, AuthorizationRequest>> GetAuthRequestByReference(
        AuthorizationRequestByReference authRequestByReference)
    {
        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Clear();

        var jsonString = await httpClient.GetStringAsync(authRequestByReference.RequestUri);
        var requestObjectValidation = CreateRequestObject(jsonString);

        return await requestObjectValidation.MatchAsync(async requestObject =>
            {
                var authRequest = requestObject.ToAuthorizationRequest();
                var clientMetadataOption = 
                    await FetchClientMetadata(authRequest).OnException(_ => Option<ClientMetadata>.None);

                var error = new InvalidRequestError($"Client ID Scheme {requestObject.ClientIdScheme} is not supported");
            
                Validation<AuthorizationRequestCancellation, AuthorizationRequest> result = 
                    requestObject.ClientIdScheme.Value switch
                    {
                        X509SanDns => requestObject
                            .ValidateJwtSignature()
                            .ValidateTrustChain()
                            .ValidateSanName()
                            .ToAuthorizationRequest()
                            .WithX509(requestObject)
                            .WithClientMetadata(clientMetadataOption),
                        //TODO: Remove Redirect URi in the future (kept for now for compatibility)
                        RedirectUri => requestObject
                            .ToAuthorizationRequest()
                            .WithClientMetadata(clientMetadataOption),
                        _ => new AuthorizationRequestCancellation(authRequest.GetResponseUriMaybe(), [error])
                    };

                return result;
            },
            seq => seq);
    }

    private async Task<Validation<AuthorizationRequestCancellation, AuthorizationRequest>> GetAuthRequestByValue(
        AuthorizationRequestByValue authRequestByValue)
    {
        var queryParams = HttpUtility.ParseQueryString(authRequestByValue.RequestUri.Query);

        var jObject = new JObject();
        foreach (var key in queryParams.AllKeys)
        {
            var value = queryParams[key];

            if (string.IsNullOrEmpty(value)) 
                continue;
                    
            try
            {
                jObject[key] = JToken.Parse(value);
            }
            catch (JsonReaderException)
            {
                jObject[key] = value;
            }
        }

        if (jObject.TryGetValue("presentation_definition_uri", out var presentationDefinitionUri))
        {
            var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Clear();
            
            var requestObjectJson = await httpClient.GetStringAsync(presentationDefinitionUri.ToString());
            jObject["presentation_definition"] = JToken.Parse(requestObjectJson);
        }
                
        var jsonString = SerializeObject(jObject);
        
        return await CreateAuthorizationRequest(jsonString).MatchAsync(async authRequest =>
            {
                var clientMetadataOption = 
                    await FetchClientMetadata(authRequest).OnException(_ => Option<ClientMetadata>.None);
                    
                var error = new InvalidRequestError($"Client ID Scheme {authRequest.ClientIdScheme} is not supported");
            
                Validation<AuthorizationRequestCancellation, AuthorizationRequest> result = 
                    authRequest.ClientIdScheme.Value switch
                    {
                        RedirectUri => authRequest.WithClientMetadata(clientMetadataOption),
                        _ => new AuthorizationRequestCancellation(authRequest.GetResponseUriMaybe(), [error])
                    };

                return result;
            },
            seq => seq);
    }
    
    private async Task<Option<ClientMetadata>> FetchClientMetadata(AuthorizationRequest authorizationRequest)
    {
        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (authorizationRequest.ClientMetadata != null)
            return authorizationRequest.ClientMetadata;

        if (string.IsNullOrWhiteSpace(authorizationRequest.ClientMetadataUri))
            return null;
            
        var response = await httpClient.GetAsync(authorizationRequest.ClientMetadataUri);
        var clientMetadata = await response.Content.ReadAsStringAsync();
        
        return DeserializeObject<ClientMetadata>(clientMetadata);
    }
}
