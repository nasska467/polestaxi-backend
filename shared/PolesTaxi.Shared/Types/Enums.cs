namespace PolesTaxi.Shared.Types;

public enum Role { Unspecified = 0, User = 1, Driver = 2, Analyst = 3, Admin = 4 }

public enum TaxiType { Unspecified = 0, Economy = 1, Comfort = 2, Business = 3 }

public enum OrderStatus { Unspecified = 0, Created = 1, DriverAssigned = 2, InProgress = 3, Completed = 4, Cancelled = 5 }

public enum DriverStatus { Unspecified = 0, Offline = 1, Available = 2, OnTrip = 3 }

public static class EnumMaps
{
    public static string ToWire(this Role role) => role switch
    {
        Role.User => "User",
        Role.Driver => "Driver",
        Role.Analyst => "Analyst",
        Role.Admin => "Admin",
        _ => "Unspecified"
    };

    public static Role RoleFromWire(string value) => value switch
    {
        "User" => Role.User,
        "Driver" => Role.Driver,
        "Analyst" => Role.Analyst,
        "Admin" => Role.Admin,
        _ => Role.Unspecified
    };

    public static bool CanTransition(DriverStatus from, DriverStatus to) => (from, to) switch
    {
        (DriverStatus.Offline, DriverStatus.Available) => true,
        (DriverStatus.Available, DriverStatus.OnTrip) => true,
        (DriverStatus.Available, DriverStatus.Offline) => true,
        (DriverStatus.OnTrip, DriverStatus.Available) => true,
        (DriverStatus.OnTrip, DriverStatus.Offline) => true,
        _ when from == to => true,
        _ => false
    };
}