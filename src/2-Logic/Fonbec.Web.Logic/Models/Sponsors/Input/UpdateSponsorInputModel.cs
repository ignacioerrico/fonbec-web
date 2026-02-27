using Fonbec.Web.DataAccess.Entities.Enums;

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