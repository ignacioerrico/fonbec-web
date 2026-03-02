using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Sponsors.Input;

public record UpdateSponsorInputModel(
    int SponsorId,
    string SponsorFirstName,
    string SponsorLastName,
    string SponsorNickName,
    Gender SponsorGender,
    string SponsorPhoneNumber,
    string SponsorEmail,
    int UpdatedById
);

public class UpdateSponsorInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UpdateSponsorInputModel, UpdateSponsorInputDataModel>()
            .Map(dest => dest.SponsorId, src => src.SponsorId)
            .Map(dest => dest.SponsorFirstName, src => src.SponsorFirstName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.SponsorLastName, src => src.SponsorLastName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.SponsorNickName, src => src.SponsorNickName.NormalizeText(),
                src => !string.IsNullOrWhiteSpace(src.SponsorNickName))
            .Map(dest => dest.SponsorGender, src => src.SponsorGender)
            .Map(dest => dest.SponsorPhoneNumber, src => src.SponsorPhoneNumber.NullOrTrimmed())
            .Map(dest => dest.SponsorEmail, src => src.SponsorEmail.Trim().ToLower(),
                src => !string.IsNullOrWhiteSpace(src.SponsorEmail))
            .Map(dest => dest.UpdatedById, src => src.UpdatedById);
    }
}