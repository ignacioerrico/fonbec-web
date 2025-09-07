namespace Fonbec.Web.DataAccess.Entities;

public class Chapter
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Chapter(string name)
    {
        Name = name;
    }
    public Chapter(string name, int id) {
        Name = name;
        Id = id;
    }
}
