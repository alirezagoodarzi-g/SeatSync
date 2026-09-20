using System.Security.Claims;

namespace SeatSync.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        if (idClaim is null || !Guid.TryParse(idClaim.Value, out var id))
        {
            throw new InvalidOperationException("User id claim is missing or invalid.");
        }
        return id;
    }
    public static Guid? TryGetUserId(this ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        return idClaim is not null && Guid.TryParse(idClaim.Value, out var id) ? id : null;
    }
}