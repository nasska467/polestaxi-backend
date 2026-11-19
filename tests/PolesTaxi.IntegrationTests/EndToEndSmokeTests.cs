using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace PolesTaxi.IntegrationTests;

public sealed class EndToEndSmokeTests
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:8088";

    [Fact]
    public async Task Full_taxi_lifecycle_and_observability_through_nginx()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(15) };

        // 1. Проверяем здоровье NGINX
        var healthResp = await client.GetAsync("/nginx-health");
        Assert.Equal(HttpStatusCode.OK, healthResp.StatusCode);

        // 2. Регистрация пассажира (ROLE_USER) и водителя (ROLE_DRIVER)
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var passengerEmail = $"pass_{suffix}@polestaxi.local";
        var driverEmail = $"drv_{suffix}@polestaxi.local";
        const string password = "Password123!";

        await RegisterUserAsync(client, "Passenger Test", passengerEmail, password, "ROLE_USER");
        await RegisterUserAsync(client, "Driver Test", driverEmail, password, "ROLE_DRIVER");

        // 3. Вход в систему и получение JWT-токенов
        var passToken = await LoginAsync(client, passengerEmail, password);
        var drvToken = await LoginAsync(client, driverEmail, password);

        // 4. Водитель проверяет профиль и выходит на линию (DRIVER_STATUS_AVAILABLE)
        using var drvClient = CreateAuthorizedClient(drvToken, $"e2e-driver-{suffix}");
        var drvMeResp = await drvClient.GetAsync("/v1/drivers/me");
        Assert.Equal(HttpStatusCode.OK, drvMeResp.StatusCode);

        var statusResp = await drvClient.PatchAsJsonAsync("/v1/drivers/me/status", new
        {
            status = "DRIVER_STATUS_AVAILABLE"
        });
        Assert.Equal(HttpStatusCode.OK, statusResp.StatusCode);

        // 5. Пассажир проверяет кошелёк и создаёт заказ
        using var passClient = CreateAuthorizedClient(passToken, $"e2e-order-{suffix}");
        var walletResp = await passClient.GetAsync("/v1/wallets/me");
        Assert.Equal(HttpStatusCode.OK, walletResp.StatusCode);

        var createOrderResp = await passClient.PostAsJsonAsync("/v1/orders", new
        {
            pickupLat = 53.9006,
            pickupLon = 27.5590,
            pickupAddress = "Minsk, Nezavisimosti 1",
            dropoffLat = 53.9150,
            dropoffLon = 27.5800,
            dropoffAddress = "Minsk, Yakuba Kolasa 10",
            taxiType = "TAXI_TYPE_ECONOMY"
        });
        Assert.Equal(HttpStatusCode.OK, createOrderResp.StatusCode);
        Assert.Equal($"e2e-order-{suffix}", createOrderResp.Headers.GetValues("X-Request-Id").First());

        var orderJson = await createOrderResp.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = orderJson.GetProperty("id").GetString()!;

        // 6. Проверяем чтение заказа по GraphQL и REST
        var gqlResp = await drvClient.PostAsJsonAsync("/graphql", new
        {
            query = $"query {{ order(id: \"{orderId}\") {{ id status }} }}"
        });
        Assert.Equal(HttpStatusCode.OK, gqlResp.StatusCode);

        var getOrderResp = await passClient.GetAsync($"/v1/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, getOrderResp.StatusCode);

        // 7. Отмена заказа пассажиром (возврат средств в кошелёк)
        var cancelResp = await passClient.PostAsync($"/v1/orders/{orderId}/cancel", EmptyJson());
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);
    }

    [Fact]
    public async Task Load_smoke_50_concurrent_requests_through_gateway()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(15) };

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"load_{suffix}@polestaxi.local";
        const string password = "Password123!";

        await RegisterUserAsync(client, "Load User", email, password, "ROLE_USER");
        var token = await LoginAsync(client, email, password);

        using var authClient = CreateAuthorizedClient(token, $"load-{suffix}");

        var sw = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => authClient.GetAsync("/v1/wallets/me"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);
        sw.Stop();

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"50 requests took too long: {sw.Elapsed}");
    }

    private static async Task RegisterUserAsync(HttpClient client, string name, string email, string password, string role)
    {
        var resp = await client.PostAsJsonAsync("/v1/users/register", new
        {
            name,
            phone = $"+79{Random.Shared.Next(100000000, 999999999)}",
            email,
            password,
            role
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var resp = await client.PostAsJsonAsync("/v1/auth/login", new { login = email, password });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("accessToken").GetString()!;
    }

    private static HttpClient CreateAuthorizedClient(string token, string requestId)
    {
        var c = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(15) };
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        c.DefaultRequestHeaders.Add("X-Request-Id", requestId);
        return c;
    }

    private static StringContent EmptyJson() => new("{}", Encoding.UTF8, "application/json");
}
