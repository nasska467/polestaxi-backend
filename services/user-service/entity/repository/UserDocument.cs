using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PolesTaxi.UserService.Entity.Repository;

public sealed class UserDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    
    public List<string> Roles { get; set; } = [];
    public double Rating { get; set; }
    public List<double> RecentRatings { get; set; } = [];
    
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}