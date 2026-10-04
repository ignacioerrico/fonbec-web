using Fonbec.Web.DataAccess.DataModels.Sponsors;
using Mapster;

namespace Fonbec.Web.Logic.Models.Sponsors;

public class SponsorRecipientsViewModel
{
    public int SponsorId { get; set; }

    public string SponsorFullName { get; set; } = string.Empty;

    public string SponsorEmail { get; set; } = string.Empty;

    public bool IsSponsorActive { get; set; }

    public List<SendAlsoToViewModel> Recipients { get; set; } = [];
}

public class SendAlsoToViewModel
{
    public int Id { get; set; }

    public string RecipientName { get; set; } = string.Empty;

    public string RecipientEmail { get; set; } = string.Empty;

    public bool SendAsBcc { get; set; }
}

public class SponsorRecipientsViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SponsorSendAlsoTosDataModel, SponsorRecipientsViewModel>()
            .Map(dest => dest.SponsorId, src => src.SponsorId)
            .Map(dest => dest.SponsorFullName, src => $"{src.SponsorFirstName} {src.SponsorLastName}")
            .Map(dest => dest.SponsorEmail, src => src.SponsorEmail)
            .Map(dest => dest.IsSponsorActive, src => src.IsSponsorActive)
            .Map(dest => dest.Recipients, src => src.Recipients);

        config.NewConfig<SendAlsoToDataModel, SendAlsoToViewModel>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.RecipientName, src => src.RecipientName)
            .Map(dest => dest.RecipientEmail, src => src.RecipientEmail)
            .Map(dest => dest.SendAsBcc, src => src.SendAsBcc);
    }
}