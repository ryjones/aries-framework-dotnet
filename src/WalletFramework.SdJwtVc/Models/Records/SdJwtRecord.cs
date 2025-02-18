using System.Collections.Immutable;
using Hyperledger.Aries.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SD_JWT.Models;
using WalletFramework.Core.Credentials;
using WalletFramework.Core.Credentials.Abstractions;
using WalletFramework.Core.Cryptography.Models;
using WalletFramework.Core.Functional;
using WalletFramework.SdJwtVc.Models.Credential;
using WalletFramework.SdJwtVc.Models.Credential.Attributes;
using WalletFramework.SdJwtVc.Models.StatusList;

namespace WalletFramework.SdJwtVc.Models.Records;

/// <summary>
///     A record that represents a Selective Disclosure JSON Web Token (SD-JWT) Credential with additional properties.
///     Inherits from base class RecordBase.
/// </summary>
public sealed class SdJwtRecord : RecordBase, ICredential
{
    public const int CurrentVersion = 2;
    
    /// <summary>
    ///     Gets or sets the attributes that should be displayed.
    /// </summary>
    public Dictionary<string, ClaimMetadata>? DisplayedAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the attributes that should be displayed.
    /// </summary>
    public List<string>? AttributeOrder { get; set; }

    /// <summary>
    ///     Gets or sets the flattened structure of the claims in the credential.
    ///     The key is a JSON path to the claim value in the decoded SdJwtVc.
    /// </summary>
    public Dictionary<string, string> Claims { get; set; }

    /// <summary>
    ///     Gets the disclosures.
    /// </summary>
    public ImmutableArray<string> Disclosures { get; set; }

    /// <summary>
    ///     Gets or sets the display of the credential.
    /// </summary>
    public List<SdJwtDisplay>? Display { get; set; }

    /// <summary>
    ///     Gets the Issuer-signed JWT part of the SD-JWT.
    /// </summary>
    public string EncodedIssuerSignedJwt { get; set; } = null!;
    
    /// <summary>
    ///     Tracks the state of the SD-JWT.
    /// </summary>
    public CredentialState CredentialState { get; set; }
    
    /// <summary>
    ///     Tracks whether it's a one-time use SD-JWT.
    /// </summary>
    public bool OneTimeUse { get; set; }
    
    /// <summary>
    ///     Tracks the Expiration Date of the Sd-JWT
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    ///     Tracks when the Sd-JWT was issued
    /// </summary>
    public DateTime? IssuedAt { get; set; }
    
    /// <summary>
    ///     Tracks when the Sd-JWT was issued
    /// </summary>
    public StatusListEntry? StatusListEntry { get; set; }
    
    /// <summary>
    ///     Tracks when the Sd-JWT is valid from
    /// </summary>
    public DateTime? NotBefore { get; set; }

    //TODO: Use CredentialSetId Type instead fo string
    public string CredentialSetId
    {
        get => Get();
        set => Set(value, false);
    }
    
    /// <summary>
    ///     Gets or sets the identifier for the issuer.
    /// </summary>
    [JsonIgnore]
    public string IssuerId
    {
        get => Get();
        set => Set(value, false);
    }

    /// <summary>
    ///     Gets the key record ID.
    /// </summary>
    [JsonIgnore]
    public KeyId KeyId
    {
        get
        {
            var str = Get();
            return KeyId
                .ValidKeyId(str)
                .UnwrapOrThrow(new InvalidOperationException("Persisted Key-ID in SD-JWT Record is corrupt"));
        }
        private set
        {
            string keyId = value;
            Set(keyId);
        }
    }

    /// <inheritdoc />
    public override string TypeName => "AF.SdJwtRecord";

    /// <summary>
    ///     Gets the verifiable credential type.
    /// </summary>
    [JsonIgnore]
    public string Vct
    {
        get => Get();
        set => Set(value, false);
    }
    
#pragma warning disable CS8618
    /// <summary>
    ///     Parameterless Default Constructor.
    /// </summary>
    public SdJwtRecord()
    {
    }
#pragma warning restore CS8618

    /// <summary>
    ///     Constructor for Serialization.
    /// </summary>
    /// <param name="displayedAttributes">The attributes that should be displayed.</param>
    /// <param name="claims">The claims made.</param>
    /// <param name="disclosures">The disclosures.</param>
    /// <param name="display">The display of the credential.</param>
    /// <param name="issuerId">The Id of the issuer</param>
    /// <param name="encodedIssuerSignedJwt">The Issuer-signed JWT part of the SD-JWT.</param>
    /// <param name="credentialSetId">The CredentialSetId.</param>
    /// <param name="statusListEntry">The status list.</param>
    /// <param name="expiresAt">The Expiration Date.</param>
    /// <param name="issuedAt">The Issued at date.</param>
    /// <param name="notBefore">The valid after date.</param>
    /// <param name="recordVersion"></param>
    /// <param name="isOneTimeUse">Indicator whether the credential should be sued only once.</param>
    [JsonConstructor]
    public SdJwtRecord(
        Dictionary<string, ClaimMetadata> displayedAttributes,
        Dictionary<string, string> claims,
        ImmutableArray<string> disclosures,
        List<SdJwtDisplay> display,
        string issuerId,
        string encodedIssuerSignedJwt,
        string credentialSetId,
        StatusListEntry statusListEntry,
        DateTime? expiresAt,
        DateTime? issuedAt,
        DateTime? notBefore,
        int recordVersion,
        bool isOneTimeUse = false)
    {
        Claims = claims;
        Disclosures = disclosures;
           
        Display = display;
        DisplayedAttributes = displayedAttributes;
            
        EncodedIssuerSignedJwt = encodedIssuerSignedJwt;

        ExpiresAt = expiresAt;
        IssuedAt = issuedAt;
        NotBefore = notBefore;
        IssuerId = issuerId;
        CredentialSetId = credentialSetId;
        OneTimeUse = isOneTimeUse;
        StatusListEntry = statusListEntry;
        RecordVersion = recordVersion;
    }
    
    public SdJwtRecord(
        string serializedSdJwtWithDisclosures,
        Dictionary<string, ClaimMetadata> displayedAttributes,
        List<SdJwtDisplay> display,
        KeyId keyId,
        CredentialSetId credentialSetId,
        bool isOneTimeUse = false)
    {
        Id = Guid.NewGuid().ToString();
            
        var sdJwtDoc = new SdJwtDoc(serializedSdJwtWithDisclosures);
        EncodedIssuerSignedJwt = sdJwtDoc.IssuerSignedJwt;
        Disclosures = sdJwtDoc.Disclosures.Select(x => x.Serialize()).ToImmutableArray();
        Claims = sdJwtDoc.GetAllSubjectClaims();
        Display = display;
        DisplayedAttributes = displayedAttributes;
        StatusListEntry = (sdJwtDoc.UnsecuredPayload.SelectToken("status")?.SelectToken("status_list")?.ToObject<StatusListEntry>() 
                           ?? sdJwtDoc.UnsecuredPayload.SelectToken("status")?.ToObject<StatusListEntry>()) is not null
            ? sdJwtDoc.UnsecuredPayload.SelectToken("status")?.SelectToken("status_list")?.ToObject<StatusListEntry>() 
              ?? sdJwtDoc.UnsecuredPayload.SelectToken("status")?.ToObject<StatusListEntry>()
            : null;

        CredentialSetId = credentialSetId;
        CredentialState = CredentialState.Active;
        OneTimeUse = isOneTimeUse;
        
        KeyId = keyId;
        ExpiresAt = sdJwtDoc.UnsecuredPayload.SelectToken("exp")?.Value<long>() is not null
            ? DateTimeOffset.FromUnixTimeSeconds(sdJwtDoc.UnsecuredPayload.SelectToken("exp")!.Value<long>()).DateTime
            : null;
        IssuedAt = sdJwtDoc.UnsecuredPayload.SelectToken("iat")?.Value<long>() is not null
            ? DateTimeOffset.FromUnixTimeSeconds(sdJwtDoc.UnsecuredPayload.SelectToken("iat")!.Value<long>()).DateTime
            : null;
        NotBefore = sdJwtDoc.UnsecuredPayload.SelectToken("nbf")?.Value<long>() is not null
            ? DateTimeOffset.FromUnixTimeSeconds(sdJwtDoc.UnsecuredPayload.SelectToken("nbf")!.Value<long>()).DateTime
            : null;
        IssuerId = sdJwtDoc.UnsecuredPayload.SelectToken("iss")?.Value<string>() 
                   ?? throw new ArgumentNullException(nameof(IssuerId), "iss claim is missing or null");
        Vct = sdJwtDoc.UnsecuredPayload.SelectToken("vct")?.Value<string>() 
              ?? throw new ArgumentNullException(nameof(Vct), "vct claim is missing or null");

        RecordVersion = CurrentVersion;
    }
    
    public SdJwtRecord(
        SdJwtDoc sdJwtDoc,
        Dictionary<string, ClaimMetadata> displayedAttributes,
        List<SdJwtDisplay> display,
        KeyId keyId,
        CredentialSetId credentialSetId,
        bool isOneTimeUse = false)
    {
        Id = Guid.NewGuid().ToString();
            
        EncodedIssuerSignedJwt = sdJwtDoc.IssuerSignedJwt;
        Disclosures = sdJwtDoc.Disclosures.Select(disclosure => disclosure.Serialize()).ToImmutableArray();
        Claims = sdJwtDoc.GetAllSubjectClaims();
        Display = display;
        DisplayedAttributes = displayedAttributes;
        StatusListEntry = (sdJwtDoc.UnsecuredPayload.SelectToken("status")?.SelectToken("status_list")?.ToObject<StatusListEntry>() 
                           ?? sdJwtDoc.UnsecuredPayload.SelectToken("status")?.ToObject<StatusListEntry>()) is not null
            ? sdJwtDoc.UnsecuredPayload.SelectToken("status")?.SelectToken("status_list")?.ToObject<StatusListEntry>() 
              ?? sdJwtDoc.UnsecuredPayload.SelectToken("status")?.ToObject<StatusListEntry>()
            : null;
            
        CredentialSetId = credentialSetId;
        CredentialState = CredentialState.Active;
        OneTimeUse = isOneTimeUse;
        
        KeyId = keyId;
        
        ExpiresAt = sdJwtDoc.UnsecuredPayload.SelectToken("exp")?.Value<long>() is not null
            ? DateTimeOffset.FromUnixTimeSeconds(sdJwtDoc.UnsecuredPayload.SelectToken("exp")!.Value<long>()).DateTime
            : null;
        IssuedAt = sdJwtDoc.UnsecuredPayload.SelectToken("iat")?.Value<long>() is not null
            ? DateTimeOffset.FromUnixTimeSeconds(sdJwtDoc.UnsecuredPayload.SelectToken("iat")!.Value<long>()).DateTime
            : null;
        NotBefore = sdJwtDoc.UnsecuredPayload.SelectToken("nbf")?.Value<long>() is not null
            ? DateTimeOffset.FromUnixTimeSeconds(sdJwtDoc.UnsecuredPayload.SelectToken("nbf")!.Value<long>()).DateTime
            : null;
        IssuerId = sdJwtDoc.UnsecuredPayload.SelectToken("iss")?.Value<string>() 
                   ?? throw new ArgumentNullException(nameof(IssuerId), "iss claim is missing or null");
        Vct = sdJwtDoc.UnsecuredPayload.SelectToken("vct")?.Value<string>() 
              ?? throw new ArgumentNullException(nameof(Vct), "vct claim is missing or null");

        RecordVersion = CurrentVersion;
    }

    public CredentialId GetId()
    {
        var id = CredentialId
            .ValidCredentialId(Id)
            .UnwrapOrThrow(new InvalidOperationException("SD-JWT RecordId is corrupt"));

        return id;
    }

    public CredentialSetId GetCredentialSetId() =>
        Core.Credentials.CredentialSetId.ValidCredentialSetId(CredentialSetId)
            .UnwrapOrThrow(new InvalidOperationException("The Id is corrupt"));
}
    
internal static class JsonExtensions
{
    internal static Dictionary<string, string> GetAllSubjectClaims(this SdJwtDoc sdJwtDoc)
    {
        var unsecuredPayload = (JObject)sdJwtDoc.UnsecuredPayload.DeepClone();
            
        RemoveRegisteredClaims(unsecuredPayload);
            
        var allLeafClaims = GetLeafNodePaths(unsecuredPayload);

        return allLeafClaims.ToDictionary(key => key, key => unsecuredPayload.SelectToken(key)?.ToString() ?? string.Empty);

        void RemoveRegisteredClaims(JObject jObject)
        {
            string[] registeredClaims = { "iss", "sub", "aud", "exp", "nbf", "iat", "jti", "vct", "cnf", "status" };
            foreach (var claim in registeredClaims)
            {
                jObject.Remove(claim);
            }
        }
    }
        
    private static List<string> GetLeafNodePaths(JObject jObject)
    {
        var leafNodePaths = new List<string>();

        TraverseJToken(jObject, "", leafNodePaths);

        return leafNodePaths;
    }

    private static void TraverseJToken(JToken token, string currentPath, List<string> leafNodePaths)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                foreach (var property in token.Children<JProperty>())
                {
                    TraverseJToken(property.Value, $"{currentPath}.{property.Name}", leafNodePaths);
                }
                break;

            case JTokenType.Array:
                int index = 0;
                foreach (var item in token.Children())
                {
                    TraverseJToken(item, $"{currentPath}[{index}]", leafNodePaths);
                    index++;
                }
                break;

            default:
                //TODO decide if path without $ should be used -> currentPath.TrimStart('.')
                leafNodePaths.Add(currentPath.TrimStart('.'));
                break;
        }
    }
}
