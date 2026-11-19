namespace PolesTaxi.OrderService.Entity.Repository;

public sealed class OrderLocationDocument
{
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string? Address { get; set; }
}

public sealed class OrderDocument
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? DriverId { get; set; }
    public string TaxiType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public OrderLocationDocument Pickup { get; set; } = new();
    public OrderLocationDocument Dropoff { get; set; } = new();
    public long DistanceMeters { get; set; }
    public long DurationSeconds { get; set; }
    public double Price { get; set; }
    public double Surge { get; set; } = 1;
    public int? UserRating { get; set; }
    public int? DriverRating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}