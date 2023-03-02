using System.Net.Http.Json;

namespace Todo.Maui.Client;

public partial class LogInPage : ContentPage
{
    private readonly AuthenticatedClientProvider _authClientProvider;

    public LogInPage(AuthenticatedClientProvider authClientProvider)
    {
        InitializeComponent();

        BindingContext = this;

        _authClientProvider = authClientProvider;
    }

    public UserInfo UserInfo { get; set; } = new();
    public bool UseCookies { get; set; }

    public string? ErrorMessage { get; set; }
    public bool WasError => ErrorMessage is not null;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _authClientProvider.Reset();
    }

    private async void OnLogInButtonClicked(object sender, EventArgs e)
    {
        try
        {
            if (UseCookies)
            {
                await _authClientProvider.AuthenticateWithCookieAsync(UserInfo);
            }
            else
            {
                await _authClientProvider.AuthenticateWithTokenAsync(UserInfo);
            }
        }
        catch (Exception ex)
        {
            NotifyLogInError(ex.Message);
            return;
        }

        Preferences.Set("username", UserInfo.Username);
        await Shell.Current.GoToAsync("..");
    }

    private async void OnCreateUserButtonClicked(object sender, EventArgs e)
    {
        var response = await _authClientProvider.UnauthenticatedClient.PostAsJsonAsync("users", UserInfo);

        if (!response.IsSuccessStatusCode)
        {
            NotifyLogInError($"{(int)response.StatusCode} HTTP status creating user at /users");
            return;
        }

        OnLogInButtonClicked(sender, e);
    }

    private void NotifyLogInError(string errorMessage)
    {
        ErrorMessage = errorMessage;
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(WasError));
    }
}