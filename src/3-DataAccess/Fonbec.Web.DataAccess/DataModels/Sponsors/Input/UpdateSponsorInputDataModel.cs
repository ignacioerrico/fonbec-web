using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Sponsors.Input;

public record UpdateSponsorInputDataModel(
    int SponsorId,
    string SponsorFirstName,
    string SponsorLastName,
    string SponsorNickName,
    Gender SponsorGender,
    string SponsorPhoneNumber,
    string SponsorEmail,
    int UpdatedById
);