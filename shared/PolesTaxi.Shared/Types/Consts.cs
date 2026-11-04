namespace PolesTaxi.Shared.Consts;

public static class Roles
{
    public const string User = "User";
    public const string Driver = "Driver";
    public const string Analyst = "Analyst";
    public const string Admin = "Admin";
    public static readonly string[] RegistrationAllowed = [User, Driver];
}

public static class KafkaTopics
{
    public const string UserRegistered = "user.registered";
    public const string DriverRegistered = "driver.registered";
    public const string OrderStatusChanged = "order.status_changed";
    public const string PaymentCompleted = "payment.completed";
    public const string TripRated = "trip.rated";
}