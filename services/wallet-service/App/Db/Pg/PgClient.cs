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
            try
            {
                var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
                Console.WriteLine("FluentMigrator: Начинаю применение миграций...");
                
                // Применяем все миграции
                runner.MigrateUp();
                
                Console.WriteLine("FluentMigrator: Миграции успешно применены!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FluentMigrator ОШИБКА: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                throw;
            }
        }

        return new PgClient(NpgsqlDataSource.Create(connectionString));
    }

    public void Dispose() => DataSource.Dispose();
}