using FluentAssertions;
using Fonbec.Web.Ui.Content;

namespace Fonbec.Web.Ui.Tests.Content;

public class PeopleFileTests
{
    [Fact]
    public void Read_KeepsACommaInsideQuotes()
    {
        const string csv = """
            nombre,ROL,detalle
            "Ana, Gómez",Desarrollo,"Filial, ejemplo"
            """;

        var people = PeopleFile.Read(csv);

        people.Should().ContainSingle();
        people[0].Should().Be(new Person("Ana, Gómez", "Desarrollo", "Filial, ejemplo"));
    }

    [Fact]
    public void Read_SkipsARowWithoutAName()
    {
        const string csv = """
            Nombre,Rol,Detalle
            ,Desarrollo,Sin nombre
            Ana Gómez,,Sin rol
            Luis Fernández,Coordinación,Filial de ejemplo
            """;

        var people = PeopleFile.Read(csv);

        people.Should().ContainSingle();
        people[0].Should().Be(new Person("Luis Fernández", "Coordinación", "Filial de ejemplo"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\n")]
    public void Read_ReturnsNoPeopleForAnEmptyFile(string csv)
    {
        PeopleFile.Read(csv).Should().BeEmpty();
    }

    [Fact]
    public void Read_ReturnsNoPeopleWhenTheFileHasOnlyTheHeader()
    {
        PeopleFile.Read("Nombre,Rol,Detalle\n").Should().BeEmpty();
    }
}