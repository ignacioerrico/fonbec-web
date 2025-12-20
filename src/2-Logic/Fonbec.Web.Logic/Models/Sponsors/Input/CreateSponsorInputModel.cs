using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Sponsors.Input;
public record CreateSponsorInputModel(
    int ChapterId,
    string SponsorFirstName,
    string SponsorLastName,
    string SponsorNickName,
    Gender SponsorGender,
    string SponsorPhoneNumber,
    string SponsorNotes,
    string SponsorEmail,
    string SponsorSendAlsoTo,
    string SponsorBranchOffice
);
public class CreateSponsorInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateSponsorInputModel, CreateSponsorInputDataModel>()
            .Map(dest => dest.ChapterId, src => src.ChapterId)
            .Map(dest => dest.SponsorFirstName, src => src.SponsorFirstName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.SponsorLastName, src => src.SponsorLastName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.SponsorNickName, src => src.SponsorNickName.NormalizeText(),
             src => !string.IsNullOrWhiteSpace(src.SponsorNickName))
            .Map(dest => dest.SponsorGender, src => src.SponsorGender)
            .Map(dest => dest.SponsorPhoneNumber, src => src.SponsorPhoneNumber.NullOrTrimmed())
            .Map(dest => dest.SponsorNotes, src => src.SponsorNotes.NullOrTrimmed())
            .Map(dest => dest.SponsorEmail, src => src.SponsorEmail.Trim().ToLower())
            .Map(dest => dest.SponsorSendAlsoTo, src => src.SponsorSendAlsoTo.NormalizeText(),
             src => !string.IsNullOrWhiteSpace(src.SponsorSendAlsoTo))
            .Map(dest => dest.SponsorBranchOffice, src => src.SponsorBranchOffice.MustBeNonEmpty().NormalizeText());
    }
}
