using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Ui.Models;

public record FonbecClaimModel(
    int UserId,
    string FirstName,
    string LastName,
    string? NickName,
    Gender Gender,
    int? ChapterId,
    string? ChapterName
);