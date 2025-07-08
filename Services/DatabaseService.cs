using System;
using System.Threading.Tasks;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ImportadorModelo2.Services
{
    public interface IDatabaseService
    {
        Task<NpgsqlConnection> GetConnectionAsync();
        Task<bool> TestConnectionAsync();
        Task InitializeDatabaseAsync();
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseService>? _logger;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService>? logger = null)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada.");
            _logger = logger;
        }

        public async Task<NpgsqlConnection> GetConnectionAsync()
        {
            try
            {
                var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                return connection;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao conectar ao banco de dados");
                throw new InvalidOperationException("Não foi possível conectar ao banco de dados", ex);
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = await GetConnectionAsync();
                return connection.State == System.Data.ConnectionState.Open;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Falha no teste de conexão");
                return false;
            }
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                using var connection = await GetConnectionAsync();
                
                // Verificar se a tabela usuarios existe
                var checkTableSql = @"
                    SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                        AND table_name = 'usuarios'
                    );";

                using var checkCommand = new NpgsqlCommand(checkTableSql, connection);
                var result = await checkCommand.ExecuteScalarAsync();
                var tableExists = result != null && (bool)result;

                if (!tableExists)
                {
                    // Criar tabela usuarios
                    var createTableSql = @"
                        CREATE TABLE usuarios (
                            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                            email TEXT UNIQUE NOT NULL,
                            senha_hash TEXT NOT NULL,
                            nome TEXT,
                            empresa TEXT,
                            hash_maquina TEXT,
                            data_ativacao TIMESTAMP,
                            ativo BOOLEAN DEFAULT TRUE,
                            criado_em TIMESTAMP DEFAULT now(),
                            atualizado_em TIMESTAMP DEFAULT now()
                        );

                        -- Índices para performance
                        CREATE INDEX idx_usuarios_email ON usuarios(email);
                        CREATE INDEX idx_usuarios_hash_maquina ON usuarios(hash_maquina);
                        CREATE INDEX idx_usuarios_ativo ON usuarios(ativo);
                    ";

                    using var createCommand = new NpgsqlCommand(createTableSql, connection);
                    await createCommand.ExecuteNonQueryAsync();

                    _logger?.LogInformation("Tabela 'usuarios' criada com sucesso");
                }
                else
                {
                    _logger?.LogInformation("Tabela 'usuarios' já existe");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao inicializar banco de dados");
                throw;
            }
        }
    }
}