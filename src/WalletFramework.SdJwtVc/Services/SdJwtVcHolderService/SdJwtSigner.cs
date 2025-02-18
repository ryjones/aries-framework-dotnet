using System.Text;
using LanguageExt;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using WalletFramework.Core.Cryptography.Abstractions;
using WalletFramework.Core.Cryptography.Models;
using WalletFramework.Core.String;

namespace WalletFramework.SdJwtVc.Services.SdJwtVcHolderService;

public class SdJwtSigner : ISdJwtSigner
{
    private readonly IKeyStore _keyStore;

    public SdJwtSigner(IKeyStore keyStore) => _keyStore = keyStore;

    public async Task<string> GenerateKbProofOfPossessionAsync(
        KeyId keyId,
        string audience,
        string nonce,
        string type,
        string? sdHash,
        string? clientId,
        Option<IEnumerable<string>> transactionDataHashes)
    {
        var header = new Dictionary<string, object>
        {
            { "alg", "ES256" },
            { "typ", type }
        };

        if (string.Equals(type, "openid4vci-proof+jwt", StringComparison.OrdinalIgnoreCase))
        {
            var publicKey = await _keyStore.GetPublicKey(keyId);
            header["jwk"] = publicKey.ToObj();
        }

        var payload = new Dictionary<string, object>
        {
            { "aud", audience },
            { "nonce", nonce },
            { "iat" , DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };

        transactionDataHashes.IfSome(hashes => 
            payload.Add("transaction_data_hashes", hashes.ToArray())
        );

        if (!sdHash.IsNullOrEmpty())
            payload["sd_hash"] = sdHash!;
        
        if (!clientId.IsNullOrEmpty())
            payload["iss"] = clientId!;

        return await CreateSignedJwt(header, payload, keyId);
    }
    
    public async Task<string> CreateSignedJwt(object header, object payload, KeyId keyId)
    {
        var encodedHeader = Base64UrlEncoder.Encode(JsonConvert.SerializeObject(header));
        var encodedPayload = Base64UrlEncoder.Encode(JsonConvert.SerializeObject(payload));

        var dataToSign = encodedHeader + "." + encodedPayload;
        var signature = await _keyStore.Sign(keyId, Encoding.UTF8.GetBytes(dataToSign));

        var encodedSignature = Base64UrlEncoder.Encode(signature);
        return encodedHeader + "." + encodedPayload + "." + encodedSignature;
    }
}
