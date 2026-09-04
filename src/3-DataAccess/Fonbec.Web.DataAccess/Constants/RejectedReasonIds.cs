namespace Fonbec.Web.DataAccess.Constants;

/// <summary>Stable seed ids for <see cref="Entities.RejectedReason"/> rows. Other is last.</summary>
public static class RejectedReasonIds
{
    public const int MissingWrittenDate = 1;
    public const int MissingAddressee = 2;
    public const int MissingAuthor = 3;
    public const int NotALetter = 4;
    public const int WrongAddressee = 5;
    public const int WrongSigner = 6;
    public const int NotReportCard = 7;
    public const int WrongStudentName = 8;
    public const int Unreadable = 9;
    public const int WrongPeriod = 10;
    public const int Other = 11;
}
