using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace RuralTourism.Web.Client.Services;

public sealed class TokenStorage
{
    private const string StorageKey = "RuralTourism.AuthToken";
    private readonly IJSRuntime _js;

    public TokenStorage(IJSRuntime js)
    {
        _js = js;
    }

        // persistent == true -> localStorage, otherwise sessionStorage
        public ValueTask SaveTokenAsync(string token, bool persistent = true)
        {
            if (persistent)
            {
                return _js.InvokeVoidAsync("localStorage.setItem", StorageKey, token);
            }

            return _js.InvokeVoidAsync("sessionStorage.setItem", StorageKey, token);
        }

        public async ValueTask<string?> GetTokenAsync()
        {
            // prefer localStorage (persistent) then sessionStorage
            var local = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(local)) return local;
            return await _js.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
        }

        public async ValueTask RemoveTokenAsync()
        {
            // remove from both storages. Call each storage API separately to avoid passing
            // an anonymous JS function reference which is unsupported by IJSRuntime.
            try
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
            }
            catch
            {
                // ignore
            }

            try
            {
                await _js.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
            }
            catch
            {
                // ignore
            }
        }
}

public sealed class AuthService
{
    private readonly HttpClient _http;
    private readonly TokenStorage _storage;
    private string? _currentToken;

    public event Action? AuthenticationStateChanged;

    public AuthService(HttpClient http, TokenStorage storage)
    {
        _http = http;
        _storage = storage;
    }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(_currentToken);

    public string? Token => _currentToken;

    private Task? _initTask;

    public async ValueTask InitializeAsync()
    {
        if (_initTask != null)
        {
            await _initTask;
            return;
        }

        var tcs = new TaskCompletionSource();
        _initTask = tcs.Task;

        try
        {
            var token = await _storage.GetTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                SetToken(token);
            }
            tcs.SetResult();
        }
        catch (Exception ex)
        {
            _initTask = null; // Allow retry on failure
            tcs.SetException(ex);
            throw;
        }
    }

    public async Task<LoginResult> LoginAsync(LoginRequest payload, bool remember = true)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", payload);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync();
            var message = string.IsNullOrWhiteSpace(detail) ? "µÇÂ½Ê§°Ü" : detail.Trim('"');
            return new(false, message);
        }

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (loginResponse?.Token == null)
        {
            return new(false, "µÇÂ¼½Ó¿ÚÎ´·µ»ØÁîÅÆ");
        }

        // store token in chosen storage
        await _storage.SaveTokenAsync(loginResponse.Token, persistent: remember);
        SetToken(loginResponse.Token);
        return new(true, null, loginResponse.Token);
    }

    public async Task LogoutAsync()
    {
        _currentToken = null;
        _http.DefaultRequestHeaders.Authorization = null;
        await _storage.RemoveTokenAsync();
        NotifyStateChanged();
    }

    private void SetToken(string token)
    {
        _currentToken = token;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        AuthenticationStateChanged?.Invoke();
    }

    private static string GetErrorMessage(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail)) return "??????";

        try
        {
            using var doc = JsonDocument.Parse(detail);
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("error", out var errorProp))
            {
                var error = errorProp.GetString();
                if (!string.IsNullOrWhiteSpace(error)) return error;
            }
        }
        catch
        {
        }

        return detail.Trim('"');
    }

    public string? GetUserId()
    {
        if (string.IsNullOrEmpty(_currentToken)) return null;
        try
        {
            var parts = _currentToken.Split('.');
            if (parts.Length <= 1) return null;
            var payload = parts[1];
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", out var idProp))
            {
                return idProp.GetString();
            }
            if (doc.RootElement.TryGetProperty("sub", out var subProp))
            {
                return subProp.GetString();
            }
        }
        catch { }
        return null;
    }

    public bool IsInRole(string role)
    {
        if (string.IsNullOrEmpty(_currentToken)) return false;
        try
        {
            var parts = _currentToken.Split('.');
            if (parts.Length <= 1) return false;
            var payload = parts[1];
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out var roleProp))
            {
                if (roleProp.ValueKind == JsonValueKind.Array)
                {
                    return roleProp.EnumerateArray().Any(r => r.GetString()?.Equals(role, StringComparison.OrdinalIgnoreCase) == true);
                }
                return roleProp.GetString()?.Equals(role, StringComparison.OrdinalIgnoreCase) == true;
            }
            if (doc.RootElement.TryGetProperty("role", out var rolePropShort))
            {
                 if (rolePropShort.ValueKind == JsonValueKind.Array)
                {
                    return rolePropShort.EnumerateArray().Any(r => r.GetString()?.Equals(role, StringComparison.OrdinalIgnoreCase) == true);
                }
                return rolePropShort.GetString()?.Equals(role, StringComparison.OrdinalIgnoreCase) == true;
            }
        }
        catch { }
        return false;
    }

    public async Task<bool> RegisterAsync(RegisterRequest payload)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", payload);
        return response.IsSuccessStatusCode;
    }
}

public sealed record LoginRequest(string UserNameOrEmail, string Password);
public sealed record RegisterRequest(string UserName, string Email, string Password);

public sealed record LoginResponse([property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("expires")] DateTime Expires);

public sealed record LoginResult(bool Success, string? ErrorMessage, string? Token = null);
