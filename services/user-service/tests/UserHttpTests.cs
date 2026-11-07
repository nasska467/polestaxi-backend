using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PolesTaxi.Shared.Hosting;
using PolesTaxi.UserService.App.Db.Mongo;
using PolesTaxi.UserService.Entity.Repository;
using PolesTaxi.UserService.Handler.Grpc;
using PolesTaxi.UserService.Repository;
using PolesTaxi.UserService.Repository.Mongo;
using PolesTaxi.UserService.Service;
using Xunit;
using MongoClient = PolesTaxi.UserService.App.Db.Mongo.MongoClient;

namespace PolesTaxi.UserService.Tests;

public sealed class UserHttpTests
{
    [Fact]
    public async Task Register_then_get_me()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddPolesTaxiGrpc();
        
        builder.Services.AddSingleton(_ => MongoClient.New("mongodb://127.0.0.1:27018", "polestaxi_users_test", CancellationToken.None).GetAwaiter().GetResult());
        builder.Services.AddSingleton<IMongoCollection<UserDocument>>(sp => sp.GetRequiredService<MongoClient>().Users);
        builder.Services.AddScoped<IUserRepository, UserMongoRepository>();
        builder.Services.AddScoped<IUserAppService, UserAppService>();
        builder.Services.AddScoped<UserGrpcHandler>();

        var app = builder.Build();
        app.MapGrpcService<UserGrpcHandler>();
        await app.StartAsync();

        var client = app.GetTestClient();

        // Генерируем уникальные email и телефон, чтобы избежать конфликта
        var uniqueEmail = $"ann_{Guid.NewGuid():N}@t.local";
        var uniquePhone = $"+7999{Random.Shared.Next(1000000, 9999999)}";

        var registerBody = new
        {
            name = "Ann",
            phone = uniquePhone,
            email = uniqueEmail,
            password = "password1",
            role = "ROLE_USER"
        };

        var response = await client.PostAsJsonAsync("/v1/users/register", registerBody);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var userId = json!["id"].ToString();

        client.DefaultRequestHeaders.Add("x-user-id", userId);
        var meResponse = await client.GetAsync("/v1/users/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var meJson = await meResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        meJson!["email"].ToString().Should().Be(uniqueEmail);

        await app.StopAsync();
    }
}