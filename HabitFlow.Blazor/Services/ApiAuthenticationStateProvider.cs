using System.Security.Claims;
using HabitFlow.Client;
using Microsoft.AspNetCore.Components.Authorization;

namespace HabitFlow.Blazor.Services;

/// <summary>
/// Dostawca stanu autentykacji który weryfikuje użytkownika przez API.
/// </summary>
public class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHabitFlowApiClient _apiClient;
    private AuthenticationState? _cachedState;

    public ApiAuthenticationStateProvider(IHabitFlowApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Zwróć cache jeśli istnieje
        if (_cachedState != null)
        {
            return _cachedState;
        }

        try
        {
            // Spróbuj pobrać profil użytkownika z API
            var profile = await _apiClient.GetProfileAsync(CancellationToken.None);

            if (profile != null)
            {
                // Użytkownik jest zalogowany
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, profile.UserId.ToString()),
                    new Claim(ClaimTypes.Email, profile.Email ?? string.Empty),
                };

                var identity = new ClaimsIdentity(claims, "ApiAuth");
                var user = new ClaimsPrincipal(identity);
                _cachedState = new AuthenticationState(user);

                return _cachedState;
            }
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            // Użytkownik nie jest zalogowany
        }
        catch
        {
            // Ignoruj błędy - traktuj jako niezalogowany
        }

        // Użytkownik niezalogowany
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        _cachedState = new AuthenticationState(anonymousUser);
        return _cachedState;
    }

    /// <summary>
    /// Powiadamia o zmianie stanu autentykacji i czyści cache.
    /// </summary>
    public void NotifyAuthenticationStateChanged()
    {
        _cachedState = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
