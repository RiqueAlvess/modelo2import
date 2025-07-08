using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ImportadorModelo2.Services;
using ImportadorModelo2.Models;
using ImportadorModelo2.Core.Utils;

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
            
            LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, CanExecuteLogin);
            TogglePasswordVisibilityCommand = new RelayCommand(TogglePasswordVisibility);
            ForgotPasswordCommand = new RelayCommand(ExecuteForgotPassword);
            GoogleLoginCommand = new RelayCommand(ExecuteGoogleLogin);
            MicrosoftLoginCommand = new RelayCommand(ExecuteMicrosoftLogin);
            SignUpCommand = new RelayCommand(ExecuteSignUp);

            Task.Run(async () => await CarregarCredenciaisSalvasAsync());
        }

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

        public ICommand LoginCommand { get; }
        public ICommand TogglePasswordVisibilityCommand { get; }
        public ICommand ForgotPasswordCommand { get; }
        public ICommand GoogleLoginCommand { get; }
        public ICommand MicrosoftLoginCommand { get; }
        public ICommand SignUpCommand { get; }

        public event Action<Usuario>? LoginSucceeded;
        public event Action<string>? NavigationRequested;

        /// <summary>
        /// Executa o processo de login
        /// </summary>
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
                System.Diagnostics.Debug.WriteLine($"Erro no login: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Verifica se o login pode ser executado
        /// </summary>
        private bool CanExecuteLogin()
        {
            return !IsLoading && 
                   !string.IsNullOrWhiteSpace(Email) && 
                   !string.IsNullOrWhiteSpace(Password);
        }

        /// <summary>
        /// Alterna visibilidade da senha
        /// </summary>
        private void TogglePasswordVisibility()
        {
            ShowPassword = !ShowPassword;
        }

        /// <summary>
        /// Navega para recuperação de senha
        /// </summary>
        private void ExecuteForgotPassword()
        {
            NavigationRequested?.Invoke("ForgotPassword");
        }

        /// <summary>
        /// Inicia login com Google
        /// </summary>
        private void ExecuteGoogleLogin()
        {
            NavigationRequested?.Invoke("GoogleLogin");
        }

        /// <summary>
        /// Inicia login com Microsoft
        /// </summary>
        private void ExecuteMicrosoftLogin()
        {
            NavigationRequested?.Invoke("MicrosoftLogin");
        }

        /// <summary>
        /// Navega para cadastro
        /// </summary>
        private void ExecuteSignUp()
        {
            NavigationRequested?.Invoke("SignUp");
        }

        /// <summary>
        /// Limpa mensagem de erro
        /// </summary>
        private void ClearError()
        {
            ErrorMessage = null;
        }

        /// <summary>
        /// Carrega credenciais salvas localmente
        /// </summary>
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
}