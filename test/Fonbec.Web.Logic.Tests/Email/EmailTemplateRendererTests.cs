using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Email;

namespace Fonbec.Web.Logic.Tests.Email;

public class EmailTemplateRendererTests
{
    [Fact]
    public void Subject_Capitalizes_The_Document_Phrase_And_Adds_The_Prefix()
    {
        var values = EmailNotificationValues.ForDocument(LetterShare(), "https://fonbec.test/historial");

        EmailTemplateRenderer.ApplySubjectPrefix(
                "FONBEC |",
                EmailTemplateRenderer.RenderSubject("{Nuevo-documento} disponible", values))
            .Should().Be("FONBEC | Nueva carta disponible");
    }

    [Fact]
    public void Empty_Prefix_Leaves_The_Subject_Unchanged()
    {
        EmailTemplateRenderer.ApplySubjectPrefix("  ", "Tu nueva cuenta")
            .Should().Be("Tu nueva cuenta");
    }

    [Fact]
    public void Prefix_Is_Separated_By_A_Single_Space()
    {
        EmailTemplateRenderer.ApplySubjectPrefix("[FONBEC]", "Tu nueva cuenta")
            .Should().Be("[FONBEC] Tu nueva cuenta");
    }

    [Theory]
    [InlineData(DocumentType.Letter, false, "nueva carta", "una nueva carta", "carta", "la carta", "carta rechazada")]
    [InlineData(DocumentType.ReportCard, false, "nuevo boletín", "un nuevo boletín", "boletín", "el boletín", "boletín rechazado")]
    [InlineData(DocumentType.ReportCard, true, "nueva libreta universitaria", "una nueva libreta universitaria", "libreta universitaria", "la libreta universitaria", "libreta universitaria rechazada")]
    [InlineData(DocumentType.Other, false, "nuevo documento", "un nuevo documento", "documento", "el documento", "documento rechazado")]
    public void Document_Phrases_Follow_The_Type_And_Education(
        DocumentType documentType,
        bool university,
        string nuevo,
        string unNuevo,
        string documento,
        string elDocumento,
        string rechazado)
    {
        var values = EmailNotificationValues.ForDocument(
            LetterShare(documentType, university), "https://fonbec.test/historial");

        values["nuevo-documento"].Should().Be(nuevo);
        values["un-nuevo-documento"].Should().Be(unNuevo);
        values["documento"].Should().Be(documento);
        values["el-documento"].Should().Be(elDocumento);
        values["documento-rechazado"].Should().Be(rechazado);
        EmailTemplateRenderer.RenderSubject("{Nuevo-documento}", values)
            .Should().Be(char.ToUpper(nuevo[0]) + nuevo[1..]);
    }

    [Fact]
    public void Names_Are_Capitalized_And_The_Body_Becomes_Linked_Paragraphs()
    {
        var html = EmailTemplateRenderer.RenderBody(
            """
            Hola, {padrino-apodo}:

            Hay {un-nuevo-documento} disponible de tu {ahijado} {becario-apodo}.

            [Ver historial de documentos]{enlace-historial-documentos}
            """,
            EmailNotificationValues.ForDocument(LetterShare(), "https://fonbec.test/padrinos/token/7"));

        html.Should().Contain("<p>Hola, Ana:</p>");
        html.Should().Contain("<p>Hay una nueva carta disponible de tu ahijada Mari.</p>");
        html.Should().Contain("<a href=\"https://fonbec.test/padrinos/token/7\">Ver historial de documentos</a>");
    }

    [Fact]
    public void Unknown_Tokens_Stay_Visible_And_Values_Are_Encoded()
    {
        var html = EmailTemplateRenderer.RenderBody(
            "Hola {no-existe}: {mensaje-de-padrino}",
            new Dictionary<string, string?> { ["mensaje-de-padrino"] = "a <b>b</b>\nsegunda" });

        html.Should().Contain("{no-existe}");
        html.Should().Contain("a &lt;b&gt;b&lt;/b&gt;<br>segunda");
        html.Should().NotContain("<b>b</b>");
    }

    [Fact]
    public void Month_Capitalization_Follows_The_Token()
    {
        var values = EmailNotificationValues.ForPlan(new DateTime(2026, 9, 1), "https://fonbec.test/plan");

        EmailTemplateRenderer.RenderSubject("{Plan-mes} de {plan-año}", values)
            .Should().Be("Septiembre de 2026");
        EmailTemplateRenderer.RenderSubject("{plan-mes}", values)
            .Should().Be("septiembre");
    }

    private static DocumentShareNotificationDataModel LetterShare(
        DocumentType documentType = DocumentType.Letter,
        bool university = false) => new()
    {
        IsCompany = false,
        RecipientName = "ana",
        RecipientNickName = "ana",
        RecipientLastName = "perez",
        RecipientGender = Gender.Female,
        DocumentType = documentType,
        StudentIsAtUniversity = university,
        StudentFirstName = "maría",
        StudentLastName = "garcía",
        StudentNickName = "mari",
        StudentGender = Gender.Female,
    };
}
