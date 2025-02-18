using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Storage;
using LanguageExt;
using WalletFramework.Core.Credentials;
using WalletFramework.SdJwtVc.Models.Records;

namespace WalletFramework.SdJwtVc.Services.SdJwtVcHolderService;

/// <summary>
///     Provides methods for handling SD-JWT credentials.
/// </summary>
public interface ISdJwtVcHolderService
{
    /// <summary>
    ///     Deletes a specific SD-JWT record by its ID.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="recordId">The ID of the SD-JWT credential record to delete.</param>
    /// <returns>
    ///     A task representing the asynchronous operation. The task result indicates whether the deletion was successful.
    /// </returns>
    Task<bool> DeleteAsync(IAgentContext context, string recordId);

    /// <summary>
    ///     Creates a SD-JWT in presentation format where the provided claims are disclosed.
    ///     The key binding is optional and can be activated by providing an audience and a nonce.
    /// </summary>
    /// <remarks>
    ///     The SD-JWT is created using the provided SD-JWT credential and the provided claims are disclosed
    /// </remarks>
    /// <param name="disclosedClaimPaths">The claims to disclose</param>
    /// <param name="sdJwt">The SD-JWT credential</param>
    /// <param name="transactionDataHashes">The transaction data hashes</param>
    /// <param name="audience">The targeted audience</param>
    /// <param name="nonce">The nonce</param>
    /// <returns>The SD-JWT in presentation format</returns>
    Task<string> CreatePresentation(
        SdJwtRecord sdJwt,
        string[] disclosedClaimPaths,
        Option<IEnumerable<string>> transactionDataHashes,
        string? audience = null,
        string? nonce = null);
        
    /// <summary>
    ///     Retrieves a specific SD-JWT record by its ID.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="credentialId">The ID of the SD-JWT credential record to retrieve.</param>
    /// <returns>
    ///     A task representing the asynchronous operation. The task result contains the <see cref="SdJwtRecord" />
    ///     associated with the given ID.
    /// </returns>
    Task<SdJwtRecord> GetAsync(IAgentContext context, string credentialId);

    /// <summary>
    ///     Lists SD-JWT records based on specified criteria.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="query">The search query to filter SD-JWT records. Default is null, meaning no filter.</param>
    /// <param name="count">The maximum number of records to retrieve. Default is 100.</param>
    /// <param name="skip">The number of records to skip. Default is 0.</param>
    /// <returns>
    ///     A task representing the asynchronous operation. The task result contains a list of <see cref="SdJwtRecord" />
    ///     that match the criteria.
    /// </returns>
    Task<List<SdJwtRecord>> ListAsync(
        IAgentContext context,
        ISearchQuery? query = null,
        int count = 100,
        int skip = 0);

    Task<Option<IEnumerable<SdJwtRecord>>> ListAsync(IAgentContext context, CredentialSetId setId);

    /// <summary>
    ///     Updates a SD-JWT record.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="record">The SD-JWT record to be saved</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the ID of the stored JWT record.</returns>
    Task UpdateAsync(IAgentContext context, SdJwtRecord record);
    
    /// <summary>
    ///     Adds a SD-JWT record.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="record">The SD-JWT record to be saved</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the ID of the stored JWT record.</returns>
    Task AddAsync(IAgentContext context, SdJwtRecord record);
}
