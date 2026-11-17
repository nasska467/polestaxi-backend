using FluentMigrator;

namespace PolesTaxi.WalletService.App.Db.Pg.Migrations;

[Migration(2)] // Номер миграции — 2, чтобы она применилась после первой (создания таблиц)
public sealed class AddIdempotencyKey : Migration
{
    public override void Up()
    {
        Execute.Sql("""
            ALTER TABLE transactions ADD COLUMN IF NOT EXISTS idempotency_key TEXT NULL;
            CREATE UNIQUE INDEX IF NOT EXISTS ux_transactions_idempotency
                ON transactions (idempotency_key) WHERE idempotency_key IS NOT NULL;
            """);
    }

    public override void Down()
    {
        Execute.Sql("""
            DROP INDEX IF EXISTS ux_transactions_idempotency;
            ALTER TABLE transactions DROP COLUMN IF EXISTS idempotency_key;
            """);
    }
}