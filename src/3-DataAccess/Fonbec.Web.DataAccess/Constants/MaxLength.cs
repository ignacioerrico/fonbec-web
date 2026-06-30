namespace Fonbec.Web.DataAccess.Constants;

public static class MaxLength
{
    public static class Auditable
    {
        public const int Notes = 128;
    }

    public static class FonbecWebUser
    {
        public const int Email = 128; // Default is 256
        public const int FirstName = 40;
        public const int LastName = 40;
        public const int NickName = 20;
        public const int PhoneNumber = 20;
    }

    public static class Chapter
    {
        public const int Name = 30;
    }

    public static class Company
    {
        public const int Name = 30;
        public const int Email = 128;
        public const int PhoneNumber = 20;
    }

    public static class PointOfContact
    {
        public const int FirstName = 40;
        public const int LastName = 40;
        public const int NickName = 20;
        public const int Email = 128;
        public const int PhoneNumber = 20;
    }

    public static class Document
    {
        public const int YouTubeVideoId = 20;
        public const int TextContent = 8192;
        public const int UploaderNotes = 512;
        public const int RejectionNotes = 512;
        public const int Description = 256;
    }

    public static class DocumentDescriptionOption
    {
        public const int Text = Document.Description;
    }

    public static class BlobPath
    {
        public const int StoragePath = 512;
        public const int MimeType = 128;
    }

    public static class RejectedReason
    {
        public const int Code = 32;
        public const int Description = 256;
    }

    public static class Assessment
    {
        public const int IssuesNotes = 1024;
        public const int Appraisal = 1024;
    }
}