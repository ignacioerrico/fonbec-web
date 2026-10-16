namespace Fonbec.Web.DataAccess.Entities.Enums;

public enum EmailMessageKind : byte
{
    NewDocument = 1,
    PlanReady = 2,
    DocumentRejected = 3,
    HeldRejection = 4,
    RecipientMessage = 5,
    NewAccount = 6,
    NewPassword = 7,
    ConfirmEmail = 8,
    ResetPassword = 9,
}