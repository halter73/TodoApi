using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Todo.Maui.Client;

public class AuthenticatedClientProvider : IDisposable
{
#if ANDROID
    private readonly Xamarin.Android.Net.AndroidMessageHandler _handler = new Xamarin.Android.Net.AndroidMessageHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
    };
#elif IOS
    private readonly NSUrlSessionHandler _handler = new NSUrlSessionHandler
    {
        TrustOverrideForUrl = (sender, url, trust) => true,
    };
#else
    private readonly HttpClientHandler _handler = new HttpClientHandler();
#endif

    private readonly Uri _baseAddress;
    private readonly CookieContainer _cookies;
    private readonly HttpClient _httpClient;

    public AuthenticatedClientProvider()
    {
        var host = DeviceInfo.Platform == DevicePlatform.Android ? "10.0.2.2" : "localhost";
        _baseAddress = new Uri($"https://{host}:5001");

        // TODO: Use different handler for cookie auth or manually set the Cookie header.
        // The manual option would make it harder to refresh the cookies, and the cookie is
        // ignored unless we set the X-Auth-Scheme request header to Identity.Application anyway.
        _cookies = new CookieContainer();
        _handler.CookieContainer = _cookies;
        _handler.UseCookies = true;

        _httpClient = new HttpClient(_handler);
        _httpClient.BaseAddress = _baseAddress;
    }

    public HttpClient UnauthenticatedClient => ResetHttpClient();

    // TODO: Use a DelegatingHandler instead?
    public async Task<HttpClient?> GetAuthenticatedClientAsync()
    {
        ResetHttpClient();

        if (await SecureStorage.Default.GetAsync("token") is string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return _httpClient;
        }
        else if (await SecureStorage.Default.GetAsync("cookie") is string cookieFromStorage)
        {
            _httpClient.DefaultRequestHeaders.Add("X-Auth-Scheme", "Identity.Application");

            // If we already have a cookie in the container, use that. It might have refreshed.
            var cookieFromContainer = _cookies.GetCookieHeader(_baseAddress);
            if (!string.IsNullOrEmpty(cookieFromContainer))
            {
                // Store the refreshed cookie.
                await SecureStorage.Default.SetAsync("cookie", cookieFromContainer);
            }
            else
            {
                _cookies.SetCookies(_baseAddress, cookieFromStorage);
            }

            return _httpClient;
        }

        return null;
    }

    public async Task AuthenticateWithTokenAsync(UserInfo userInfo)
    {
        var response = await _httpClient.PostAsJsonAsync("users/token", userInfo);
        response.EnsureSuccessStatusCode();

        var authToken = await response.Content.ReadFromJsonAsync<AuthToken>();

        if (string.IsNullOrEmpty(authToken?.Token))
        {
            throw new InvalidDataException("No token in /users/token response.");
        }

        await SecureStorage.Default.SetAsync("token", authToken.Token);
    }

    public async Task AuthenticateWithCookieAsync(UserInfo userInfo)
    {
        var response = await _httpClient.PostAsJsonAsync("users/cookie", userInfo);
        response.EnsureSuccessStatusCode();

        var cookie = _cookies.GetCookieHeader(_baseAddress);

        if (string.IsNullOrEmpty(cookie))
        {
            throw new InvalidDataException("No cookie set by /users/cookie.");
        }

        await SecureStorage.Default.SetAsync("cookie", cookie);
    }

    private HttpClient ResetHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Remove("X-Auth-Scheme");
        _httpClient.DefaultRequestHeaders.Authorization = null;
        return _httpClient;
    }

    public void Reset()
    {
        ResetHttpClient();
        SecureStorage.Remove("token");
        SecureStorage.Remove("cookie");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
