namespace WalletFramework.Oid4Vc.ClientAttestation;

public record CombinedWalletAttestation
{
    public WalletInstanceAttestationJwt WalletInstanceAttestationJwt { get; }
    
    public WalletInstanceAttestationPopJwt WalletInstanceAttestationPopJwt { get; }

    private CombinedWalletAttestation(
        WalletInstanceAttestationJwt walletInstanceAttestationJwt, 
        WalletInstanceAttestationPopJwt walletInstanceAttestationPopJwt)
    {
        WalletInstanceAttestationJwt = walletInstanceAttestationJwt;
        WalletInstanceAttestationPopJwt = walletInstanceAttestationPopJwt;
    }

    public static CombinedWalletAttestation Create(
        WalletInstanceAttestationJwt walletInstanceAttestationJwt, 
        WalletInstanceAttestationPopJwt walletInstanceAttestationPopJwt)
        => new(walletInstanceAttestationJwt, walletInstanceAttestationPopJwt);
}

public static class CombinedWalletAttestationExtensions
{
    public static string ToStringRepresentation(this CombinedWalletAttestation combinedWalletAttestation)
        => combinedWalletAttestation.WalletInstanceAttestationJwt + "~" +
           combinedWalletAttestation.WalletInstanceAttestationPopJwt;
}
