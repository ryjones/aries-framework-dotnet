using Newtonsoft.Json.Linq;
using WalletFramework.Core.Functional;
using WalletFramework.Core.Json;

namespace WalletFramework.Oid4Vc.Oid4Vp.TransactionData;

public record TransactionData(
    TransactionDataType Type,
    List<TransactionDataCredentialId> CredentialIds,
    List<TransactionDataHashesAlg> TransactionDataHashesAlg)
{
    public static Validation<TransactionData> FromJObject(JObject jObject)
    {
        var typesValidation =
            from jToken in jObject.GetByKey("type")
            from type in TransactionDataType.FromJToken(jToken)
            select type;

        var idsValidation =
            from jToken in jObject.GetByKey("credential_ids")
            from jArray in jToken.ToJArray()
            from credentialIds in jArray.TraverseAll(TransactionDataCredentialId.FromJToken)
            select credentialIds.ToList();
        
        var hashesAlgValidation =
            from jToken in jObject.GetByKey("transaction_data_hashes_alg")
            from jArray in jToken.ToJArray()
            from hashesAlgs in jArray.TraverseAll(Oid4Vp.TransactionData.TransactionDataHashesAlg.FromJToken)
            select hashesAlgs.ToList();

        var dataHashesAlgs = hashesAlgValidation.Match(
            algs => algs,
            _ => [Oid4Vp.TransactionData.TransactionDataHashesAlg.CreateSha256Alg()]);

        return
            from transactionDataType in typesValidation
            from credentialIds in idsValidation
            select new TransactionData(transactionDataType, credentialIds, dataHashesAlgs);
    }
}
