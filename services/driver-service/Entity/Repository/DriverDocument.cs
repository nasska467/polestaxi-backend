using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace PolesTaxi.DriverService.Entity.Repository;

public sealed class DriverDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TaxiType { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    
    [BsonIgnoreIfNull]
    public GeoJsonPoint<GeoJson2DGeographicCoordinates>? Location { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}