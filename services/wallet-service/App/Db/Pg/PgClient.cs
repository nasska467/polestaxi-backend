using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace PolesTaxi.WalletService.App.Db.Pg;

public sealed class PgClient : IDisposable
{
    public NpgsqlDataSource DataSource { get; }

    private PgClient(NpgsqlDataSource dataSource) => DataSource = dataSource;

    public static PgClient New(string connectionString)
    {
        var services = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(PgClient).Assembly).For.Migrations())
            .BuildServiceProvider(false);

        using (var scope = services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();
        }

        return new PgClient(NpgsqlDataSource.Create(connectionString));
    }

    public void Dispose() => DataSource.Dispose();
}