using System.Net.Http.Json;

namespace Todo.Maui.Client;

public partial class LogInPage : ContentPage
{
    private readonly AuthenticatedClientProvider _clientProvider;

    public LogInPage(AuthenticatedClientProvider clientProvider)
    {
        InitializeComponent();

        BindingContext = this;

        _clientProvider = clientProvider;
    }

    public UserInfo UserInfo { get; set; } = new();
    public bool UseCookies { get; set; }

    public string? ErrorMessage { get; set; }
    public bool WasError => ErrorMessage is not null;

    private async void OnLogInButtonClicked(object sender, EventArgs e)
    {
        if (await _clientProvider.AuthenticateAsync(UserInfo, UseCookies) is string error)
        {
            NotifyLogInError(error);
            return;
        }

        Preferences.Set("username", UserInfo.Username);
        await Shell.Current.GoToAsync("..");
    }

    private async void OnCreateUserButtonClicked(object sender, EventArgs e)
    {
        var response = await _clientProvider.UnauthenticatedClient.PostAsJsonAsync("users", UserInfo);

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