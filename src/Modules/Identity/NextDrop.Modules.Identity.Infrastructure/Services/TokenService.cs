using System.Security.Cryptography;
using System.Text;
using NextDrop.Modules.Identity.Application.Abstractions;

namespace NextDrop.Modules.Identity.Infrastructure.Services;

public class TokenService : ITokenService
{
    public string GenerateSecureToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
