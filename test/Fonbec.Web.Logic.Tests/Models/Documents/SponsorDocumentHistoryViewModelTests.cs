using FluentAssertions;
using Fonbec.Web.Logic.Models.Documents;

namespace Fonbec.Web.Logic.Tests.Models.Documents;

public class SponsorDocumentHistoryViewModelTests
{
    [Fact]
    public void CcRecipientLine_Is_Null_When_There_Are_No_Cc_Names()
    {
        var history = new SponsorDocumentHistoryViewModel
        {
            CcRecipientNames = ["  ", ""],
        };

        history.CcRecipientLine.Should().BeNull();
    }

    [Fact]
    public void CcRecipientLine_Names_A_Single_Recipient()
    {
        var history = new SponsorDocumentHistoryViewModel
        {
            CcRecipientNames = [" Alicia Mureau "],
        };

        history.CcRecipientLine.Should().Be("Con copia a Alicia Mureau.");
    }

    [Fact]
    public void CcRecipientLine_Joins_Two_Names_With_Y()
    {
        var history = new SponsorDocumentHistoryViewModel
        {
            CcRecipientNames = ["Alicia Mureau", "Pedro Roque"],
        };

        history.CcRecipientLine.Should().Be("Con copia a Alicia Mureau y Pedro Roque.");
    }

    [Fact]
    public void CcRecipientLine_Joins_Three_Names_With_Commas_And_Y()
    {
        var history = new SponsorDocumentHistoryViewModel
        {
            CcRecipientNames = ["Alicia Mureau", "Pedro Roque", "Luis Perez"],
        };

        history.CcRecipientLine.Should().Be("Con copia a Alicia Mureau, Pedro Roque y Luis Perez.");
    }
}