using System.Net.Http.Json;

namespace Todo.Maui.Client
{
    public partial class MainPage : ContentPage
    {
        private readonly AuthenticatedClientProvider _clientProvider;

        public MainPage(AuthenticatedClientProvider clientProvider)
        {
            InitializeComponent();

            BindingContext = this;

            _clientProvider = clientProvider;
        }

        public IEnumerable<TodoItem> TodoItems { get; private set; } = Array.Empty<TodoItem>();
        public string UserName => Preferences.Get("username", "<Unknown>");

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (await _clientProvider.GetAuthenticatedClientAsync() is not HttpClient client)
            {
                await Shell.Current.GoToAsync(nameof(LogInPage));
                return;
            }

            var response = await client.GetAsync("todos");

            if (!response.IsSuccessStatusCode)
            {
                // We should probably only do this for 401s, but we don't have a better way to handle errors right now.
                await Shell.Current.GoToAsync(nameof(LogInPage));
                return;
            }

            TodoItems = await response.Content.ReadFromJsonAsync<TodoItem[]>() ?? Array.Empty<TodoItem>();
            OnPropertyChanged(nameof(UserName));
            OnPropertyChanged(nameof(TodoItems));
        }

        private async void OnLogOutButtonClicked(object sender, EventArgs e)
        {
            SecureStorage.Remove("token");
            await Shell.Current.GoToAsync(nameof(LogInPage));
        }
    }
}