using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meetora.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoogleSubject = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PictureUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "meeting_proposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PublicTokenHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    PublicTokenProtected = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    PublicTokenCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meeting_proposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meeting_proposals_app_users_OrganizerUserId",
                        column: x => x.OrganizerUserId,
                        principalTable: "app_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "meeting_time_options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingProposalId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meeting_time_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meeting_time_options_meeting_proposals_MeetingProposalId",
                        column: x => x.MeetingProposalId,
                        principalTable: "meeting_proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "time_option_votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingProposalId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoterId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoterName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_option_votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_time_option_votes_meeting_proposals_MeetingProposalId",
                        column: x => x.MeetingProposalId,
                        principalTable: "meeting_proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_time_option_votes_meeting_time_options_TimeOptionId",
                        column: x => x.TimeOptionId,
                        principalTable: "meeting_time_options",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_app_users_Email",
                table: "app_users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_app_users_GoogleSubject",
                table: "app_users",
                column: "GoogleSubject",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meeting_proposals_OrganizerUserId",
                table: "meeting_proposals",
                column: "OrganizerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_meeting_proposals_PublicTokenHash",
                table: "meeting_proposals",
                column: "PublicTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meeting_proposals_Status",
                table: "meeting_proposals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_meeting_time_options_MeetingProposalId",
                table: "meeting_time_options",
                column: "MeetingProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_meeting_time_options_MeetingProposalId_StartAt_EndAt",
                table: "meeting_time_options",
                columns: new[] { "MeetingProposalId", "StartAt", "EndAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_time_option_votes_MeetingProposalId",
                table: "time_option_votes",
                column: "MeetingProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_time_option_votes_MeetingProposalId_VoterId",
                table: "time_option_votes",
                columns: new[] { "MeetingProposalId", "VoterId" });

            migrationBuilder.CreateIndex(
                name: "IX_time_option_votes_MeetingProposalId_VoterId_TimeOptionId",
                table: "time_option_votes",
                columns: new[] { "MeetingProposalId", "VoterId", "TimeOptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_time_option_votes_TimeOptionId",
                table: "time_option_votes",
                column: "TimeOptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "time_option_votes");

            migrationBuilder.DropTable(
                name: "meeting_time_options");

            migrationBuilder.DropTable(
                name: "meeting_proposals");

            migrationBuilder.DropTable(
                name: "app_users");
        }
    }
}
