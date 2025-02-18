using System.Drawing;
using SD_JWT.Models;
using WalletFramework.Core.Credentials;
using WalletFramework.Core.Cryptography.Models;
using WalletFramework.Core.Functional;
using WalletFramework.Oid4Vc.Oid4Vci.CredConfiguration.Models.SdJwt;
using WalletFramework.SdJwtVc.Models.Credential;
using WalletFramework.SdJwtVc.Models.Records;

namespace WalletFramework.Oid4Vc.Oid4Vci.Implementations;

public static class SdJwtRecordExtensions
{
    public static SdJwtRecord ToRecord(
        this SdJwtDoc sdJwtDoc,
        SdJwtConfiguration configuration,
        KeyId keyId,
        CredentialSetId credentialSetId,
        bool isOneTimeUse)
    {
        var claims = configuration.ExtractClaimMetadata();
        
        var display = 
            from displays in configuration.CredentialConfiguration.Display
            select displays.Select(credentialDisplay =>
            {
                var backgroundColor = credentialDisplay.BackgroundColor.ToNullable() ?? Color.White;
                var textColor = credentialDisplay.TextColor.ToNullable() ?? Color.Black;
            
                return new SdJwtDisplay
                {
                    Logo = new SdJwtDisplay.SdJwtLogo
                    {
                        AltText = credentialDisplay.Logo.ToNullable()?.AltText.ToNullable(),
                        Uri = credentialDisplay.Logo.ToNullable()?.Uri!
                    },
                    Name = credentialDisplay.Name.ToNullable(),
                    BackgroundColor = backgroundColor,
                    Locale = credentialDisplay.Locale.ToNullable(),
                    TextColor = textColor
                };
            }).ToList();
        
        var record = new SdJwtRecord(
            sdJwtDoc,
            claims,
            display.Fallback([]),
            keyId, 
            credentialSetId,
            isOneTimeUse);

        return record;
    }
    
    internal static SdJwtDoc ToSdJwtDoc(this SdJwtRecord record)
    {
        return new SdJwtDoc(record.EncodedIssuerSignedJwt + "~" + string.Join("~", record.Disclosures) + "~");
    }
}
