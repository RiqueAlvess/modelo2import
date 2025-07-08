using System;

namespace ImportadorModelo2.Models
{
    public class Usuario
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public string? Nome { get; set; }
        public string? Empresa { get; set; }
        public string? HashMaquina { get; set; }
        public DateTime? DataAtivacao { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime CriadoEm { get; set; }
        public DateTime AtualizadoEm { get; set; }
    }

    // DTOs para Login
    public record LoginRequest(string Email, string Senha, string HashMaquina);
    
    public record LoginResponse(
        bool Sucesso, 
        string? Mensagem, 
        Usuario? Usuario = null
    );
}