using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TCIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyReminderRepetition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_reminder_schedules_repeat_index",
                table: "reminder_schedules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_reminder_rules_repeat",
                table: "reminder_rules");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_reminder_rule_id_occurrence_start_at_utc_sc~",
                table: "outbox_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_outbox_messages_repeat_index",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_notifications_reminder_rule_id_original_start_at_utc_repeat~",
                table: "notifications");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notifications_repeat_index",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "repeat_index",
                table: "reminder_schedules");

            migrationBuilder.DropColumn(
                name: "repeat_count",
                table: "reminder_rules");

            migrationBuilder.DropColumn(
                name: "repeat_index",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "repeat_index",
                table: "notifications");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reminder_rules_repeat_interval",
                table: "reminder_rules",
                sql: "repeat_every_minutes IS NULL OR repeat_every_minutes > 0");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_reminder_rule_id_occurrence_start_at_utc_sc~",
                table: "outbox_messages",
                columns: new[] { "reminder_rule_id", "occurrence_start_at_utc", "scheduled_fire_at_utc", "event_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_reminder_rule_id_original_start_at_utc_schedu~",
                table: "notifications",
                columns: new[] { "reminder_rule_id", "original_start_at_utc", "scheduled_fire_at_utc", "recipient_user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_reminder_rules_repeat_interval",
                table: "reminder_rules");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_reminder_rule_id_occurrence_start_at_utc_sc~",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_notifications_reminder_rule_id_original_start_at_utc_schedu~",
                table: "notifications");

            migrationBuilder.AddColumn<int>(
                name: "repeat_index",
                table: "reminder_schedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "repeat_count",
                table: "reminder_rules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "repeat_index",
                table: "outbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "repeat_index",
                table: "notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_reminder_schedules_repeat_index",
                table: "reminder_schedules",
                sql: "repeat_index >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reminder_rules_repeat",
                table: "reminder_rules",
                sql: "repeat_count >= 0 AND (repeat_count = 0 OR (repeat_every_minutes IS NOT NULL AND repeat_every_minutes > 0))");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_reminder_rule_id_occurrence_start_at_utc_sc~",
                table: "outbox_messages",
                columns: new[] { "reminder_rule_id", "occurrence_start_at_utc", "scheduled_fire_at_utc", "repeat_index", "event_version" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_outbox_messages_repeat_index",
                table: "outbox_messages",
                sql: "repeat_index >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_reminder_rule_id_original_start_at_utc_repeat~",
                table: "notifications",
                columns: new[] { "reminder_rule_id", "original_start_at_utc", "repeat_index", "recipient_user_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_notifications_repeat_index",
                table: "notifications",
                sql: "repeat_index >= 0");
        }
    }
}
