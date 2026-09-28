namespace Reservas.Common.DTOs.Auth;

public class AuthUserDto
{
    public string id { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string[] roles { get; set; } = Array.Empty<string>();
}