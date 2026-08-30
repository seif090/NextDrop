using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextDrop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsAndRealtimeFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notifications");

            migrationBuilder.CreateTable(
                name: "notification_templates",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TitleTemplate = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    BodyTemplate = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DataJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Priority = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "processed_integration_events",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_integration_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllowOrderNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    AllowMarketingNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    InAppEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_preferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notification_deliveries",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProviderMessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NextRetryAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_deliveries_notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalSchema: "notifications",
                        principalTable: "notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_NextRetryAtUtc",
                schema: "notifications",
                table: "notification_deliveries",
                column: "NextRetryAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_NotificationId",
                schema: "notifications",
                table: "notification_deliveries",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_Status",
                schema: "notifications",
                table: "notification_deliveries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_Type_Channel_Language_Version",
                schema: "notifications",
                table: "notification_templates",
                columns: new[] { "Type", "Channel", "Language", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_CreatedAtUtc",
                schema: "notifications",
                table: "notifications",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_ExpiresAtUtc",
                schema: "notifications",
                table: "notifications",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_Status",
                schema: "notifications",
                table: "notifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId",
                schema: "notifications",
                table: "notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_processed_integration_events_ConsumerName_EventId",
                schema: "notifications",
                table: "processed_integration_events",
                columns: new[] { "ConsumerName", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_processed_integration_events_ProcessedAtUtc",
                schema: "notifications",
                table: "processed_integration_events",
                column: "ProcessedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_user_preferences_UserId",
                schema: "notifications",
                table: "user_preferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_deliveries",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "notification_templates",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "processed_integration_events",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "user_preferences",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "notifications");
        }
    }
}
