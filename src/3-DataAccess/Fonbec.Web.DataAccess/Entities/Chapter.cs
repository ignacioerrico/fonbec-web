namespace Fonbec.Web.DataAccess.Entities;

public class Chapter : Auditable
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Chapter(string name)
    {
        Name = name;
    }
}
