using WalletFramework.Core.Encoding;
using WalletFramework.Oid4Vc.Payment;

namespace WalletFramework.Oid4Vc.Oid4Vp.TransactionData;

/// <summary>
///     The transaction data hash which will be put into the response
/// </summary>
/// <param name="hash">The hash</param>
/// <remarks>Currently only supports sha-256</remarks>
public readonly struct TransactionDataHash(Sha256Hash hash)
{
    public string AsString => hash.AsString;
}    

public static class TransactionDataHashFun
{
    public static TransactionDataHash Hash(this PaymentTransactionData transactionData)
    {
        var hashWith256 = new Func<PaymentTransactionData, TransactionDataHash>(data =>
        {
            var bytes = data.Encoded.AsByteArray;
            var hash = Sha256Hash.ComputeHash(bytes);
            return new TransactionDataHash(hash);
        });

        var hashAlg = transactionData.TransactionData.TransactionDataHashesAlg.First();
        return hashAlg.AsString switch
        {
            "sha-256" => hashWith256(transactionData),
            _ => throw new InvalidOperationException($"The transaction data hash alg {hashAlg.AsString} is not supported")
        };
    }
}
