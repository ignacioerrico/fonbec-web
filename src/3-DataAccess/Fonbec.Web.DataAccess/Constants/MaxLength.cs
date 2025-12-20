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

    //TODO: Add sponsors max lengths when needed
}