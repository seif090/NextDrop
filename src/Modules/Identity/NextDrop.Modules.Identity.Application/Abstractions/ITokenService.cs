namespace NextDrop.Modules.Identity.Application.Abstractions;

public interface ITokenService
{
    string GenerateSecureToken();
    string HashToken(string token);
}
