using Newtonsoft.Json;

namespace WalletFramework.Oid4Vc.Errors;

/// <summary>
///     Represents an error response when the request is invalid or unauthorized.
/// </summary>
public record ErrorResponse(string Error, string? ErrorDescription, string? ErrorUri)
{
    /// <summary>
    ///     Gets or sets the error code indicating the type of error that occurred.
    /// </summary>
    [JsonProperty("error")]
    public string Error { get; } = Error;

    /// <summary>
    ///     Gets or sets the human-readable text providing additional information about the error.
    /// </summary>
    [JsonProperty("error_description", NullValueHandling = NullValueHandling.Ignore)]
    public string? ErrorDescription { get; } = ErrorDescription;

    /// <summary>
    ///     Gets or sets the URI identifying a human-readable web page with information about the error.
    /// </summary>
    [JsonProperty("error_uri", NullValueHandling = NullValueHandling.Ignore)]
    public string? ErrorUri { get; } = ErrorUri;
}

public static class ErrorResponseFun
{
    public static FormUrlEncodedContent ToFormUrlContent(this ErrorResponse errorResponse)
    {
        var dict = new Dictionary<string, string>
        {
            { "error", errorResponse.Error },
        };
        
        if (errorResponse.ErrorDescription is not null)
        {
            dict.Add("error_description", errorResponse.ErrorDescription);
        }
        
        if (errorResponse.ErrorUri is not null)
        {
            dict.Add("error_uri", errorResponse.ErrorUri);
        }

        return new FormUrlEncodedContent(dict);
    }
}
