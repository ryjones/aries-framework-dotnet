using LanguageExt;

namespace WalletFramework.Core.Uri;

public static class UriFun
{
    public static string ToStringWithoutTrail(this System.Uri uri) => uri.ToString().TrimEnd('/');
    
    public static Option<System.Uri> TryToParseUri(string uriString)
    {
        try
        {
            return new System.Uri(uriString);
        }
        catch (Exception)
        {
            return Option<System.Uri>.None;
        }
    }
}
