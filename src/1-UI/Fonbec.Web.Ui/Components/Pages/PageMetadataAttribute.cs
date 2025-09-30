namespace Fonbec.Web.Ui.Components.Pages;

[AttributeUsage(AttributeTargets.Class)]
public class PageMetadataAttribute(string codename, string description, string[] roles) : Attribute
{
    public string Codename { get; } = codename;

    public string Description { get; } = description;

    public string[] Roles { get; set; } = roles;
}