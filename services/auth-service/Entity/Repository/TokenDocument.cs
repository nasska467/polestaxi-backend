namespace PolesTaxi.AuthService.Entity.Repository;

public sealed class TokenDocument
{
    public string UserId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public string? Family { get; set; }
}