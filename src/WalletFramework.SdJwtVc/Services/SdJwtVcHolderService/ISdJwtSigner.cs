using LanguageExt;
using WalletFramework.Core.Cryptography.Models;

namespace WalletFramework.SdJwtVc.Services.SdJwtVcHolderService;

public interface ISdJwtSigner
{
    Task<string> CreateSignedJwt(object header, object payload, KeyId keyId);
    
    Task<string> GenerateKbProofOfPossessionAsync(
        KeyId keyId,
        string audience,
        string nonce,
        string type,
        string? sdHash,
        string? clientId,
        Option<IEnumerable<string>> transactionDataHashes);
}
