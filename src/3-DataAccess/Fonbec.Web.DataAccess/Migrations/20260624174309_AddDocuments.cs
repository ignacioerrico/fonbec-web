using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Fonbec.Web.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "NotificationPreference",
                table: "Sponsors",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicAccessToken",
                table: "Sponsors",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("UPDATE Sponsors SET PublicAccessToken = NEWID()");

            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    AssessmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpellingScore = table.Column<int>(type: "int", nullable: false),
                    PenmanshipScore = table.Column<int>(type: "int", nullable: false),
                    ContentScore = table.Column<int>(type: "int", nullable: false),
                    HasRedFlags = table.Column<bool>(type: "bit", nullable: false),
                    HasGreenFlags = table.Column<bool>(type: "bit", nullable: false),
                    IssuesNotes = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Appraisal = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.AssessmentId);
                });

            migrationBuilder.CreateTable(
                name: "BlobPaths",
                columns: table => new
                {
                    BlobPathId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoragePath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    Sha256 = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlobPaths", x => x.BlobPathId);
                });

            migrationBuilder.CreateTable(
                name: "RejectedReasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AppliesToDocumentType = table.Column<byte>(type: "tinyint", nullable: true),
                    RequiresNotes = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RejectedReasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    DocumentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentType = table.Column<byte>(type: "tinyint", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SponsorId = table.Column<int>(type: "int", nullable: true),
                    FileKind = table.Column<byte>(type: "tinyint", nullable: false),
                    BlobPathId = table.Column<long>(type: "bigint", nullable: true),
                    OriginalBlobPathId = table.Column<long>(type: "bigint", nullable: true),
                    ImprovedBlobPathId = table.Column<long>(type: "bigint", nullable: true),
                    YouTubeVideoId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TextContent = table.Column<string>(type: "nvarchar(max)", maxLength: 8192, nullable: true),
                    UploaderNotes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    DigitalImprovementStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    ImprovementLockedById = table.Column<int>(type: "int", nullable: true),
                    ImprovementLockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedById = table.Column<int>(type: "int", nullable: false),
                    ApprovedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedReasonId = table.Column<int>(type: "int", nullable: true),
                    RejectionNotes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.DocumentId);
                    table.CheckConstraint("CK_Document_ApprovedOrRejected", "[ApprovedOn] IS NULL OR [RejectedOn] IS NULL");
                    table.CheckConstraint("CK_Document_ImprovementComplete", "[DigitalImprovementStatus] <> 3 OR [ImprovedBlobPathId] IS NOT NULL");
                    table.CheckConstraint("CK_Document_ImprovementNotApplicable", "[DigitalImprovementStatus] <> 0 OR [ImprovedBlobPathId] IS NULL");
                    table.CheckConstraint("CK_Letter_SponsorRequired", "[DocumentType] <> 1 OR [SponsorId] IS NOT NULL");
                    table.CheckConstraint("CK_OtherDocument_SponsorNull", "[DocumentType] <> 3 OR [SponsorId] IS NULL");
                    table.CheckConstraint("CK_ReportCard_SponsorNull", "[DocumentType] <> 2 OR [SponsorId] IS NULL");
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_ImprovementLockedById",
                        column: x => x.ImprovementLockedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_BlobPaths_BlobPathId",
                        column: x => x.BlobPathId,
                        principalTable: "BlobPaths",
                        principalColumn: "BlobPathId");
                    table.ForeignKey(
                        name: "FK_Documents_BlobPaths_ImprovedBlobPathId",
                        column: x => x.ImprovedBlobPathId,
                        principalTable: "BlobPaths",
                        principalColumn: "BlobPathId");
                    table.ForeignKey(
                        name: "FK_Documents_BlobPaths_OriginalBlobPathId",
                        column: x => x.OriginalBlobPathId,
                        principalTable: "BlobPaths",
                        principalColumn: "BlobPathId");
                    table.ForeignKey(
                        name: "FK_Documents_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_PlannedDeliveries_PlanId",
                        column: x => x.PlanId,
                        principalTable: "PlannedDeliveries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_RejectedReasons_RejectedReasonId",
                        column: x => x.RejectedReasonId,
                        principalTable: "RejectedReasons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalTable: "Sponsors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DocumentQueueItems",
                columns: table => new
                {
                    QueueItemId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<long>(type: "bigint", nullable: false),
                    EnqueuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ReviewLockedById = table.Column<int>(type: "int", nullable: true),
                    ReviewLockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DequeueCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentQueueItems", x => x.QueueItemId);
                    table.ForeignKey(
                        name: "FK_DocumentQueueItems_AspNetUsers_ReviewLockedById",
                        column: x => x.ReviewLockedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DocumentQueueItems_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentShares",
                columns: table => new
                {
                    DocumentShareId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<long>(type: "bigint", nullable: false),
                    SponsorId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SharedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SharedById = table.Column<int>(type: "int", nullable: false),
                    NotificationSentOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentShares", x => x.DocumentShareId);
                    table.ForeignKey(
                        name: "FK_DocumentShares_AspNetUsers_SharedById",
                        column: x => x.SharedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DocumentShares_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentShares_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalTable: "Sponsors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DocumentShares_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LetterReviews",
                columns: table => new
                {
                    LetterReviewId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<long>(type: "bigint", nullable: false),
                    ConfirmedIsLetter = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmedWrittenDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedAddressee = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmedSignerMatchesStudent = table.Column<bool>(type: "bit", nullable: false),
                    AssessmentId = table.Column<long>(type: "bigint", nullable: false),
                    ReviewedById = table.Column<int>(type: "int", nullable: false),
                    ReviewedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LetterReviews", x => x.LetterReviewId);
                    table.ForeignKey(
                        name: "FK_LetterReviews_AspNetUsers_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LetterReviews_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "AssessmentId");
                    table.ForeignKey(
                        name: "FK_LetterReviews_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportCardReviews",
                columns: table => new
                {
                    ReportCardReviewId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<long>(type: "bigint", nullable: false),
                    ConfirmedIsReportCardOrTranscript = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmedStudentNameCorrect = table.Column<bool>(type: "bit", nullable: false),
                    ReviewedById = table.Column<int>(type: "int", nullable: false),
                    ReviewedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportCardReviews", x => x.ReportCardReviewId);
                    table.ForeignKey(
                        name: "FK_ReportCardReviews_AspNetUsers_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReportCardReviews_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "RejectedReasons",
                columns: new[] { "Id", "AppliesToDocumentType", "Code", "Description", "RequiresNotes" },
                values: new object[,]
                {
                    { 1, (byte)1, "NotALetter", "No es una carta", false },
                    { 2, (byte)1, "WrongAddressee", "Destinatario incorrecto", false },
                    { 3, (byte)1, "WrongSigner", "Firma incorrecta", false },
                    { 4, (byte)1, "Illegible", "Ilegible", false },
                    { 5, (byte)1, "InappropriateContent", "Contenido inapropiado", false },
                    { 6, (byte)1, "WrongDate", "Fecha incorrecta", false },
                    { 7, (byte)2, "NotReportCard", "No es boletín o libreta", false },
                    { 8, (byte)2, "WrongStudentName", "Nombre del estudiante incorrecto", false },
                    { 9, (byte)3, "Unreadable", "No legible", false },
                    { 10, (byte)3, "WrongDocument", "Documento incorrecto", false },
                    { 11, null, "Other", "Otro", true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sponsors_PublicAccessToken",
                table: "Sponsors",
                column: "PublicAccessToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentQueueItems_DocumentId",
                table: "DocumentQueueItems",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentQueueItems_ReviewLockedById",
                table: "DocumentQueueItems",
                column: "ReviewLockedById");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_BlobPathId",
                table: "Documents",
                column: "BlobPathId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ChapterId",
                table: "Documents",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ImprovedBlobPathId",
                table: "Documents",
                column: "ImprovedBlobPathId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ImprovementLockedById",
                table: "Documents",
                column: "ImprovementLockedById");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_OriginalBlobPathId",
                table: "Documents",
                column: "OriginalBlobPathId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_PlanId",
                table: "Documents",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_RejectedReasonId",
                table: "Documents",
                column: "RejectedReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SponsorId",
                table: "Documents",
                column: "SponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Status_UploadedOn",
                table: "Documents",
                columns: new[] { "Status", "UploadedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_StudentId",
                table: "Documents",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_StudentId_SponsorId_PlanId",
                table: "Documents",
                columns: new[] { "StudentId", "SponsorId", "PlanId" },
                unique: true,
                filter: "[DocumentType] = 1 AND [Status] <> 5");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UploadedById",
                table: "Documents",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_DocumentId_SponsorId",
                table: "DocumentShares",
                columns: new[] { "DocumentId", "SponsorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_SharedById",
                table: "DocumentShares",
                column: "SharedById");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_SponsorId",
                table: "DocumentShares",
                column: "SponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_StudentId",
                table: "DocumentShares",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_LetterReviews_AssessmentId",
                table: "LetterReviews",
                column: "AssessmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LetterReviews_DocumentId",
                table: "LetterReviews",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LetterReviews_ReviewedById",
                table: "LetterReviews",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_ReportCardReviews_DocumentId",
                table: "ReportCardReviews",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportCardReviews_ReviewedById",
                table: "ReportCardReviews",
                column: "ReviewedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentQueueItems");

            migrationBuilder.DropTable(
                name: "DocumentShares");

            migrationBuilder.DropTable(
                name: "LetterReviews");

            migrationBuilder.DropTable(
                name: "ReportCardReviews");

            migrationBuilder.DropTable(
                name: "Assessments");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "BlobPaths");

            migrationBuilder.DropTable(
                name: "RejectedReasons");

            migrationBuilder.DropIndex(
                name: "IX_Sponsors_PublicAccessToken",
                table: "Sponsors");

            migrationBuilder.DropColumn(
                name: "NotificationPreference",
                table: "Sponsors");

            migrationBuilder.DropColumn(
                name: "PublicAccessToken",
                table: "Sponsors");
        }
    }
}
