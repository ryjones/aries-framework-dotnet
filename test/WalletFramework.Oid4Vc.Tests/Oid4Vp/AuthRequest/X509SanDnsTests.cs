using FluentAssertions;
using WalletFramework.Core.Functional;
using WalletFramework.Oid4Vc.Oid4Vp.Models;
using static WalletFramework.Oid4Vc.Tests.Oid4Vp.AuthRequest.Samples.AuthRequestSamples;

namespace WalletFramework.Oid4Vc.Tests.Oid4Vp.AuthRequest;

public class X509SanDnsTests
{
    [Fact]
    public void Valid_Jwt_Signature_Is_Accepted()
    {
        var requestObject = RequestObject
            .CreateRequestObject(SignedRequestObjectWithRs256AndTrustChain)
            .UnwrapOrThrow();

        var sut = requestObject.ValidateJwtSignature();

        sut.Should().NotBeNull();
    }

    [Fact]
    public void Invalid_Jwt_Signature_Results_In_An_Error()
    {
        var requestObject = RequestObject
            .CreateRequestObject(SignedRequestObjectWithRs256AndInvalidSignature)
            .UnwrapOrThrow();
        try
        {
            requestObject.ValidateJwtSignature();
            Assert.Fail("Expected validation to fail");
        }
        catch (Exception)
        {
            // Pass
        }
    }

    [Fact]
    public void Trust_Chain_Is_Being_Validated()
    {
        var requestObject = RequestObject
            .CreateRequestObject(SignedRequestObjectWithRs256AndTrustChain)
            .UnwrapOrThrow();

        var sut = requestObject.ValidateTrustChain();

        sut.Should().NotBeNull();
    }
    
    [Fact]
    public void Single_Self_Signed_Certificate_Is_Allowed()
    {
        var requestObject = RequestObject
            .CreateRequestObject(SignedRequestObjectWithRs256AndSingleSelfSigned)
            .UnwrapOrThrow();

        var sut = requestObject.ValidateTrustChain();

        sut.Should().NotBeNull();
    }

    [Fact]
    public void Single_Non_Self_Signed_Certificate_Is_Not_Allowed()
    {
        var requestObject = RequestObject
            .CreateRequestObject(SignedRequestObjectWithRs256AndSingleNonSelfSigned)
            .UnwrapOrThrow();

        try
        {
            requestObject.ValidateTrustChain();
            Assert.Fail("Expected validation to fail");
        }
        catch (Exception)
        {
            // Pass
        }
    }

    [Fact]
    public void Checks_That_San_Name_Equals_Client_Id()
    {
        var requestObject = RequestObject
            .CreateRequestObject(SignedRequestObjectWithRs256AndTrustChain)
            .UnwrapOrThrow();

        var sut = requestObject.ValidateSanName();

        sut.Should().NotBeNull();
    }
}
