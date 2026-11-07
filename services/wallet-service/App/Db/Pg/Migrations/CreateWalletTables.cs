using FluentMigrator;

namespace PolesTaxi.WalletService.App.Db.Pg.Migrations;

[Migration(1)]
public sealed class CreateWalletTables : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            CREATE TABLE wallets (
                id UUID PRIMARY KEY,
                user_id TEXT NOT NULL UNIQUE,
                balance NUMERIC(12, 2) NOT NULL CHECK (balance >= 0),
                currency CHAR(3) NOT NULL DEFAULT 'RUB',
                created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now()
            );

            CREATE TABLE transactions (
                id UUID PRIMARY KEY,
                wallet_id UUID NOT NULL REFERENCES wallets(id),
                type TEXT NOT NULL CHECK (type IN ('debit', 'credit', 'refund')),
                amount NUMERIC(12, 2) NOT NULL CHECK (amount > 0),
                status TEXT NOT NULL CHECK (status IN ('pending', 'completed', 'failed', 'rolled_back')),
                order_id TEXT NULL,
                created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now()
            );

            CREATE INDEX ix_transactions_wallet_created ON transactions (wallet_id, created_at DESC);
        ");
    }

    public override void Down()
    {
        Execute.Sql("DROP TABLE IF EXISTS transactions; DROP TABLE IF EXISTS wallets;");
    }
}