using WalletFramework.Oid4Vc.Oid4Vp.Models;
using WalletFramework.Oid4Vc.Oid4Vp.PresentationExchange.Models;
using WalletFramework.Oid4Vc.Oid4Vp.PresentationExchange.Services;
using static Newtonsoft.Json.JsonConvert;
using Format = WalletFramework.Oid4Vc.Oid4Vci.CredConfiguration.Models.Format;

namespace WalletFramework.Oid4Vc.Oid4Vp.Services;

/// <inheritdoc />
internal class Oid4VpHaipClient : IOid4VpHaipClient
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Oid4VpHaipClient" /> class.
    /// </summary>
    /// <param name="pexService">The service responsible for presentation exchange protocol operations.</param>
    public Oid4VpHaipClient(IPexService pexService)
    {
        _pexService = pexService;
    }

    private readonly IPexService _pexService;

    /// <inheritdoc />
    public async Task<AuthorizationResponse> CreateAuthorizationResponseAsync(
        AuthorizationRequest authorizationRequest,
        (string InputDescriptorId, string Presentation, Format Format)[] presentationMap)
    {
        var descriptorMaps = new List<DescriptorMap>();
        var vpToken = new List<string>();
            
        for (var index = 0; index < presentationMap.Length; index++)
        {
            vpToken.Add(presentationMap[index].Presentation);

            var descriptorMap = new DescriptorMap
            {
                Format = presentationMap[index].Format.ToString(),
                Path = presentationMap.Length > 1 ? "$[" + index + "]" : "$",
                Id = presentationMap[index].InputDescriptorId,
                PathNested = null
            };
            descriptorMaps.Add(descriptorMap);
        }

        var presentationSubmission = await _pexService.CreatePresentationSubmission(
            authorizationRequest.PresentationDefinition,
            descriptorMaps.ToArray());

        return new AuthorizationResponse
        {
            PresentationSubmission = presentationSubmission,
            VpToken = vpToken.Count > 1 ? SerializeObject(vpToken) : vpToken[0],
            State = authorizationRequest.State
        };
    }
}
