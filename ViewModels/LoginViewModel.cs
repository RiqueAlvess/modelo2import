using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ImportadorModelo2.Services;
using ImportadorModelo2.Models;

namespace ImportadorModelo2.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IAutenticacaoService _autenticacaoService;
        private string _email = string.Empty;
        private string _password = string.Empty;
        private bool _rememberMe = false;
        private bool _showPassword = false;
        private bool _isLoading = false;
        private string? _errorMessage;

        public LoginViewModel(IAutenticacaoService autenticacaoService)
        {
            _autenticacaoService = autenticacaoService;
            
            // Inicializar comandos
            LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, CanExecuteLogin);
            TogglePasswordVisibilityCommand = new RelayCommand(TogglePasswordVisibility);
            ForgotPasswordCommand = new RelayCommand(ExecuteForgotPassword);
            GoogleLoginCommand = new RelayCommand(ExecuteGoogleLogin);
            MicrosoftLoginCommand = new RelayCommand(ExecuteMicrosoftLogin);
            SignUpCommand = new RelayCommand(ExecuteSignUp);

            // Carregar credenciais salvas
            Task.Run(async () => await CarregarCredenciaisSalvasAsync());
        }

        // Propriedades
        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    ((AsyncRelayCommand)LoginCommand).NotifyCanExecuteChanged();
                    ClearError();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    ((AsyncRelayCommand)LoginCommand).NotifyCanExecuteChanged();
                    ClearError();
                }
            }
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        public bool ShowPassword
        {
            get => _showPassword;
            set => SetProperty(ref _showPassword, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    ((AsyncRelayCommand)LoginCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        // Comandos
        public ICommand LoginCommand { get; }
        public ICommand TogglePasswordVisibilityCommand { get; }
        public ICommand ForgotPasswordCommand { get; }
        public ICommand GoogleLoginCommand { get; }
        public ICommand MicrosoftLoginCommand { get; }
        public ICommand SignUpCommand { get; }

        // Eventos
        public event Action<Usuario>? LoginSucceeded;
        public event Action<string>? NavigationRequested;

        // Métodos dos comandos
        private async Task ExecuteLoginAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();

                var hashMaquina = await _autenticacaoService.ObterHashMaquinaAsync();
                var resultado = await _autenticacaoService.LoginAsync(Email, Password, hashMaquina);

                if (resultado.Sucesso && resultado.Usuario != null)
                {
                    if (RememberMe)
                    {
                        // Salvar credenciais localmente se necessário
                        await _autenticacaoService.SalvarCredenciaisAsync(Email, RememberMe);
                    }

                    LoginSucceeded?.Invoke(resultado.Usuario);
                }
                else
                {
                    ErrorMessage = resultado.Mensagem ?? "Email ou senha incorretos";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Erro de conexão. Tente novamente.";
                // Log do erro
                System.Diagnostics.Debug.WriteLine($"Erro no login: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteLogin()
        {
            return !IsLoading && 
                   !string.IsNullOrWhiteSpace(Email) && 
                   !string.IsNullOrWhiteSpace(Password);
        }

        private void TogglePasswordVisibility()
        {
            ShowPassword = !ShowPassword;
        }

        private void ExecuteForgotPassword()
        {
            NavigationRequested?.Invoke("ForgotPassword");
        }

        private void ExecuteGoogleLogin()
        {
            // TODO: Implementar login com Google
            NavigationRequested?.Invoke("GoogleLogin");
        }

        private void ExecuteMicrosoftLogin()
        {
            // TODO: Implementar login com Microsoft
            NavigationRequested?.Invoke("MicrosoftLogin");
        }

        private void ExecuteSignUp()
        {
            NavigationRequested?.Invoke("SignUp");
        }

        private void ClearError()
        {
            ErrorMessage = null;
        }

        private async Task CarregarCredenciaisSalvasAsync()
        {
            try
            {
                var (email, lembrarMe) = await _autenticacaoService.CarregarCredenciaisAsync();
                if (!string.IsNullOrEmpty(email))
                {
                    Email = email;
                    RememberMe = lembrarMe;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar credenciais: {ex.Message}");
            }
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Helper classes para comandos
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;

            try
            {
                _isExecuting = true;
                NotifyCanExecuteChanged();
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                NotifyCanExecuteChanged();
            }
        }

        public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}