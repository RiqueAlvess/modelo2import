using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ImportadorModelo2.Models;
using ImportadorModelo2.Core.Utils;

namespace ImportadorModelo2.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private Usuario? _usuario;
        private string _usuarioNome = string.Empty;
        private string _usuarioEmail = string.Empty;
        private string _mensagemBoasVindas = string.Empty;

        public MainViewModel()
        {
            UsuarioNome = "Usuário";
            UsuarioEmail = "usuario@exemplo.com";
            MensagemBoasVindas = "Bem-vindo ao sistema!";

            NovaImportacaoCommand = new RelayCommand(ExecuteNovaImportacao);
            VisualizarLogsCommand = new RelayCommand(ExecuteVisualizarLogs);
            NovoLayoutCommand = new RelayCommand(ExecuteNovoLayout);
            LogoutCommand = new RelayCommand(ExecuteLogout);
        }

        public MainViewModel(Usuario usuario) : this()
        {
            SetUsuario(usuario);
        }

        public string UsuarioNome
        {
            get => _usuarioNome;
            set => SetProperty(ref _usuarioNome, value);
        }

        public string UsuarioEmail
        {
            get => _usuarioEmail;
            set => SetProperty(ref _usuarioEmail, value);
        }

        public string MensagemBoasVindas
        {
            get => _mensagemBoasVindas;
            set => SetProperty(ref _mensagemBoasVindas, value);
        }

        public Usuario? Usuario
        {
            get => _usuario;
            set
            {
                if (SetProperty(ref _usuario, value) && value != null)
                {
                    SetUsuario(value);
                }
            }
        }

        public ICommand NovaImportacaoCommand { get; }
        public ICommand VisualizarLogsCommand { get; }
        public ICommand NovoLayoutCommand { get; }
        public ICommand LogoutCommand { get; }

        public event Action? NovaImportacaoRequested;
        public event Action? VisualizarLogsRequested;
        public event Action? NovoLayoutRequested;
        public event Action? LogoutRequested;

        /// <summary>
        /// Define dados do usuário logado
        /// </summary>
        private void SetUsuario(Usuario usuario)
        {
            UsuarioNome = usuario.Nome ?? "Usuário";
            UsuarioEmail = usuario.Email;

            var saudacao = GetSaudacao();
            var nomeDisplay = !string.IsNullOrEmpty(usuario.Nome) ? usuario.Nome : usuario.Email.Split('@')[0];
            
            MensagemBoasVindas = $"{saudacao}, {nomeDisplay}!";

            if (!string.IsNullOrEmpty(usuario.Empresa))
            {
                MensagemBoasVindas += $"\nEmpresa: {usuario.Empresa}";
            }
        }

        /// <summary>
        /// Retorna saudação baseada no horário
        /// </summary>
        private string GetSaudacao()
        {
            var hora = DateTime.Now.Hour;
            
            return hora switch
            {
                >= 6 and < 12 => "Bom dia",
                >= 12 and < 18 => "Boa tarde",
                _ => "Boa noite"
            };
        }

        /// <summary>
        /// Executa comando de nova importação
        /// </summary>
        private void ExecuteNovaImportacao()
        {
            try
            {
                NovaImportacaoRequested?.Invoke();
                System.Diagnostics.Debug.WriteLine("Comando Nova Importação executado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao executar Nova Importação: {ex.Message}");
            }
        }

        /// <summary>
        /// Executa comando de visualizar logs
        /// </summary>
        private void ExecuteVisualizarLogs()
        {
            try
            {
                VisualizarLogsRequested?.Invoke();
                System.Diagnostics.Debug.WriteLine("Comando Visualizar Logs executado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao executar Visualizar Logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Executa comando de novo layout
        /// </summary>
        private void ExecuteNovoLayout()
        {
            try
            {
                NovoLayoutRequested?.Invoke();
                System.Diagnostics.Debug.WriteLine("Comando Novo Layout executado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao executar Novo Layout: {ex.Message}");
            }
        }

        /// <summary>
        /// Executa comando de logout
        /// </summary>
        private void ExecuteLogout()
        {
            try
            {
                LogoutRequested?.Invoke();
                System.Diagnostics.Debug.WriteLine("Comando Logout executado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao executar Logout: {ex.Message}");
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