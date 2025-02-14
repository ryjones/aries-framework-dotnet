using LanguageExt;
using PeterO.Cbor;
using WalletFramework.Core.Cryptography.Models;
using WalletFramework.Core.Encoding;
using WalletFramework.MdocLib.Device;
using WalletFramework.MdocLib.Security;
using WalletFramework.MdocLib.Security.Abstractions;
using static WalletFramework.Core.Encoding.Sha256Hash;

namespace WalletFramework.Oid4Vc.Oid4Vp.Models;

public record Oid4VpHandover(
    Sha256Hash ClientIdSha256Hash,
    Sha256Hash ResponseUriSha256Hash,
    // TODO: Nonce from AuthRequest has weak type
    string Nonce) : IHandover
{
    public CBORObject ToCbor()
    {
        var result = CBORObject.NewArray();
        
        result.Add(ClientIdSha256Hash.AsBytes);
        result.Add(ResponseUriSha256Hash.AsBytes);
        result.Add(Nonce);

        return result;
    }

    public SessionTranscript ToSessionTranscript() => new(
        Option<DeviceEngagement>.None,
        Option<PublicKey>.None,
        this);
}

public static class Oid4VpHandoverFun
{
    public static Oid4VpHandover ToVpHandover(this AuthorizationRequest request, Option<Nonce> mdocGeneratedNonce)
    {
        var clientIdToHash = CBORObject.NewArray();
        var clientId = CBORObject.FromObject(request.ClientId);
        clientIdToHash.Add(clientId);
        mdocGeneratedNonce.Match(
            nonce =>
            {
                clientIdToHash.Add(nonce.AsBase64Url.ToString());
            },
            () =>
            {
                clientIdToHash.Add(string.Empty);
            });

        var clientIdToHashBytes = clientIdToHash.EncodeToBytes();
        
        var responseUriToHash = CBORObject.NewArray();
        var responseUri = CBORObject.FromObject(request.ResponseUri);
        responseUriToHash.Add(responseUri);
        mdocGeneratedNonce.Match(
            nonce =>
            {
                responseUriToHash.Add(nonce.AsBase64Url.ToString());
            },
            () =>
            {
                responseUriToHash.Add(string.Empty);
            });
        
        var responseUriToHashBytes = responseUriToHash.EncodeToBytes();

        var clientIdHash = ComputeHash(clientIdToHashBytes);
        var responseUriHash = ComputeHash(responseUriToHashBytes);

        return new Oid4VpHandover(clientIdHash, responseUriHash, request.Nonce);
    }
}
