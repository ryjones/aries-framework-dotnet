using Newtonsoft.Json;
using WalletFramework.Oid4Vc.Oid4Vp.PresentationExchange.Models;

namespace WalletFramework.Oid4Vc.Oid4Vp.Models;

/// <summary>
///  Represents an OpenID4VP Authorization Response.
/// </summary>
public class AuthorizationResponse
{
    /// <summary>
    ///     Gets or sets the VP Token.
    /// </summary>
    [JsonProperty("vp_token")]
    public string VpToken { get; set; } = null!;
        
    /// <summary>
    ///   Gets or sets the Presentation Submission.
    /// </summary>
    [JsonProperty("presentation_submission")]
    public PresentationSubmission PresentationSubmission { get; set; } = null!;
        
    /// <summary>
    ///     Gets or sets the State.
    /// </summary>
    [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
    public string? State { get; set; }
}

public static class AuthorizationResponseFun
{
    public static FormUrlEncodedContent ToFormUrl(this AuthorizationResponse authorizationResponse)
    {
        var dict = new Dictionary<string, string>
        {
            { "vp_token", authorizationResponse.VpToken },
            { "presentation_submission", JsonConvert.SerializeObject(authorizationResponse.PresentationSubmission) },
            { "state", authorizationResponse.State ?? string.Empty }
        };

        return new FormUrlEncodedContent(dict);
    }
}
