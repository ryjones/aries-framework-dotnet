using LanguageExt;
using WalletFramework.Oid4Vc.ClientAttestation;
using WalletFramework.Oid4Vc.Oid4Vci.AuthFlow.Models;
using WalletFramework.Oid4Vc.Oid4Vp.Models;
using WalletFramework.Oid4Vc.Oid4Vp.PresentationExchange.Models;

namespace WalletFramework.Oid4Vc.Oid4Vp.Services;

/// <summary>
///     This Service offers methods to handle the OpenId4Vp protocol according to the HAIP
/// </summary>
public interface IOid4VpClientService
{
    /// <summary>
    ///     Aborts an authorization request for example when the wallet wants to deny the request
    /// </summary>
    /// <param name="cancellation">Information for aborting the auth request</param>
    /// <remarks>The error is based on OpenID4VP and OAuth2</remarks>
    /// <returns>Maybe a callback URI from the relying party</returns>
    Task<Option<Uri>> AbortAuthorizationRequest(AuthorizationRequestCancellation cancellation);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="authorizationRequest"></param>
    /// <param name="selectedCredentials"></param>
    /// <param name="clientAttestation"></param>
    /// <returns></returns>
    Task<Option<Uri>> AcceptAuthorizationRequest(
        AuthorizationRequest authorizationRequest,
        IEnumerable<SelectedCredential> selectedCredentials,
        CombinedWalletAttestation? clientAttestation = null);

    /// <summary>
    ///     Finds possible PresentationCandidates for an InputDescriptor.
    /// </summary>
    /// <param name="inputDescriptor"></param>
    /// <returns>
    ///     A task representing the asynchronous operation. The task result contains the Presentation Candidate if one was
    ///     found for the Input Descriptor.
    /// </returns>
    Task<Option<PresentationCandidates>> FindCredentialCandidateForInputDescriptorAsync(
        InputDescriptor inputDescriptor);

    /// <summary>
    ///     Processes an OpenID4VP Authorization Request Url.
    /// </summary>
    /// <param name="authorizationRequestUri"></param>
    /// <returns>
    ///     A task representing the asynchronous operation. The task result contains the Authorization Response object
    ///     associated with the OpenID4VP Authorization Request Url and Credentials Candidates that can be used to answer the
    ///     request.
    /// </returns>
    // Task<Validation<AuthorizationRequestCandidates>> ProcessAuthorizationRequestUri(
    //     AuthorizationRequestUri authorizationRequestUri);
    Task<Validation<AuthorizationRequestCancellation, AuthorizationRequestCandidates>> ProcessAuthorizationRequestUri(
        AuthorizationRequestUri authorizationRequestUri);

    /// <summary>
    ///     Prepares and sends an Authorization Response containing a Presentation Submission with on demand credentials (C'')
    ///     and the VP Token to the Redirect Uri.
    /// </summary>
    /// <param name="authorizationRequest"></param>
    /// <param name="selectedCredentials"></param>
    /// <param name="issuanceSession"></param>
    /// <returns>
    ///     A task representing the asynchronous operation. The task result contains the Callback Url of the Authorization
    ///     Response if present.
    /// </returns>
    Task<Option<Uri>> AcceptOnDemandRequest(
        AuthorizationRequest authorizationRequest,
        IEnumerable<SelectedCredential> selectedCredentials,
        IssuanceSession issuanceSession);
}
