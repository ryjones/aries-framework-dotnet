using LanguageExt;
using WalletFramework.Oid4Vc.Oid4Vp.Models;
using WalletFramework.Oid4Vc.Oid4Vp.PresentationExchange.Models;

namespace WalletFramework.Oid4Vc.Oid4Vp.PresentationExchange.Services;

/// <summary>
/// Pex Service.
/// </summary>
public interface IPexService
{
    /// <summary>
    /// Creates a presentation submission.
    /// </summary>
    /// <param name="presentationDefinition">The presentation definition.</param>
    /// <param name="descriptorMaps">Data used to build Descriptor Maps.</param>
    /// <returns>
    ///     A task representing the asynchronous operation. The task result contains the Presentation Submission.
    /// </returns>
    Task<PresentationSubmission> CreatePresentationSubmission(PresentationDefinition presentationDefinition, DescriptorMap[] descriptorMaps);

    /// <summary>
    ///     Finds the credential candidates based on the provided credentials and input descriptors.
    /// </summary>
    /// <param name="inputDescriptors">An array of input descriptors to be satisfied.</param>
    /// <param name="supportedFormatSigningAlgorithms">An array of input descriptors to be satisfied.</param>
    /// <returns>An array of credential candidates, each containing a list of credentials that match the input descriptors.</returns>
    Task<IEnumerable<PresentationCandidates>> FindCredentialCandidates(
        IEnumerable<InputDescriptor> inputDescriptors,
        Option<Formats> supportedFormatSigningAlgorithms);
}
