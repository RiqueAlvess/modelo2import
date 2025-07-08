using System.ComponentModel;
using System.Runtime.CompilerServices;
using ImportadorModelo2.Models;

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
            // Valores padrão
            UsuarioNome = "Usuário";
            UsuarioEmail = "usuario@exemplo.com";
            MensagemBoasVindas = "Bem-vindo ao sistema!";
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

        private string GetSaudacao()
        {
            var hora = System.DateTime.Now.Hour;
            
            return hora switch
            {
                >= 6 and < 12 => "Bom dia",
                >= 12 and < 18 => "Boa tarde",
                _ => "Boa noite"
            };
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
}