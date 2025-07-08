using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ImportadorModelo2.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ImportadorModelo2.Services
{
    public interface ILayoutService
    {
        Task<List<LayoutModel>> ListarLayoutsAsync();
        Task<LayoutModel?> CarregarLayoutAsync(Guid id);
        Task<bool> SalvarLayoutAsync(LayoutModel layout);
        Task<bool> ExcluirLayoutAsync(Guid id);
        Task<ValidacaoLayoutModel> ValidarLayoutAsync(LayoutModel layout);
        string ObterCaminhoLayouts();
    }

    public class LayoutService : ILayoutService
    {
        private readonly string _caminhoLayouts;
        private readonly ILogger<LayoutService>? _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public LayoutService(IConfiguration configuration, ILogger<LayoutService>? logger = null)
        {
            _logger = logger;
            
            var baseDirectory = configuration.GetValue<string>("AppSettings:LayoutsFolder") ?? "Layouts";
            _caminhoLayouts = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                                          "ImportadorModelo2", baseDirectory);
            
            // Criar diretório se não existir
            Directory.CreateDirectory(_caminhoLayouts);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            _logger?.LogInformation("LayoutService inicializado. Pasta: {CaminhoLayouts}", _caminhoLayouts);
        }

        /// <summary>
        /// Lista todos os layouts salvos
        /// </summary>
        public async Task<List<LayoutModel>> ListarLayoutsAsync()
        {
            try
            {
                var layouts = new List<LayoutModel>();
                var arquivos = Directory.GetFiles(_caminhoLayouts, "*.json");

                foreach (var arquivo in arquivos)
                {
                    try
                    {
                        var layout = await CarregarLayoutDoArquivoAsync(arquivo);
                        if (layout != null)
                        {
                            layouts.Add(layout);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Erro ao carregar layout do arquivo: {Arquivo}", arquivo);
                    }
                }

                // Ordenar por data de criação (mais recente primeiro)
                return layouts.OrderByDescending(l => l.DataCriacao).ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao listar layouts");
                return new List<LayoutModel>();
            }
        }

        /// <summary>
        /// Carrega um layout específico pelo ID
        /// </summary>
        public async Task<LayoutModel?> CarregarLayoutAsync(Guid id)
        {
            try
            {
                var nomeArquivo = $"{id}.json";
                var caminhoArquivo = Path.Combine(_caminhoLayouts, nomeArquivo);

                if (!File.Exists(caminhoArquivo))
                {
                    _logger?.LogWarning("Layout não encontrado: {Id}", id);
                    return null;
                }

                return await CarregarLayoutDoArquivoAsync(caminhoArquivo);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao carregar layout: {Id}", id);
                return null;
            }
        }

        /// <summary>
        /// Salva um layout no disco
        /// </summary>
        public async Task<bool> SalvarLayoutAsync(LayoutModel layout)
        {
            try
            {
                // Garantir que o layout tem um ID
                if (layout.Id == Guid.Empty)
                {
                    layout.Id = Guid.NewGuid();
                    layout.DataCriacao = DateTime.Now;
                }
                else
                {
                    layout.DataAtualizacao = DateTime.Now;
                }

                // Atualizar metadados
                AtualizarMetadados(layout);

                var nomeArquivo = $"{layout.Id}.json";
                var caminhoArquivo = Path.Combine(_caminhoLayouts, nomeArquivo);

                var json = JsonSerializer.Serialize(layout, _jsonOptions);
                await File.WriteAllTextAsync(caminhoArquivo, json);

                _logger?.LogInformation("Layout salvo com sucesso: {Nome} ({Id})", layout.Nome, layout.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao salvar layout: {Nome}", layout.Nome);
                return false;
            }
        }

        /// <summary>
        /// Exclui um layout
        /// </summary>
        public async Task<bool> ExcluirLayoutAsync(Guid id)
        {
            try
            {
                var nomeArquivo = $"{id}.json";
                var caminhoArquivo = Path.Combine(_caminhoLayouts, nomeArquivo);

                if (!File.Exists(caminhoArquivo))
                {
                    _logger?.LogWarning("Layout não encontrado para exclusão: {Id}", id);
                    return false;
                }

                await Task.Run(() => File.Delete(caminhoArquivo));
                
                _logger?.LogInformation("Layout excluído com sucesso: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao excluir layout: {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// Valida um layout
        /// </summary>
        public async Task<ValidacaoLayoutModel> ValidarLayoutAsync(LayoutModel layout)
        {
            var validacao = new ValidacaoLayoutModel();
            
            await Task.Run(() =>
            {
                try
                {
                    // Validações básicas
                    if (string.IsNullOrWhiteSpace(layout.Nome))
                    {
                        validacao.Erros.Add("Nome do layout é obrigatório");
                    }

                    if (layout.LinhaHeader <= 0)
                    {
                        validacao.Erros.Add("Linha do cabeçalho deve ser maior que zero");
                    }

                    if (!layout.Mapeamentos.Any())
                    {
                        validacao.Erros.Add("Pelo menos um campo deve ser mapeado");
                    }

                    // Validar campos obrigatórios
                    var camposObrigatorios = ObterCamposObrigatorios();
                    var camposMapeados = layout.Mapeamentos.Select(m => m.CampoAPI).ToHashSet();

                    foreach (var campoObrigatorio in camposObrigatorios)
                    {
                        if (!camposMapeados.Contains(campoObrigatorio))
                        {
                            validacao.Avisos.Add($"Campo obrigatório não mapeado: {campoObrigatorio}");
                        }
                    }

                    // Validar duplicatas
                    var colunasDuplicadas = layout.Mapeamentos
                        .GroupBy(m => m.IndiceColuna)
                        .Where(g => g.Count() > 1 && g.Key >= 0)
                        .Select(g => g.Key);

                    foreach (var coluna in colunasDuplicadas)
                    {
                        validacao.Erros.Add($"Coluna {coluna} está mapeada para múltiplos campos");
                    }

                    validacao.Valido = !validacao.Erros.Any();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Erro durante validação do layout");
                    validacao.Erros.Add("Erro inesperado durante validação");
                    validacao.Valido = false;
                }
            });

            return validacao;
        }

        /// <summary>
        /// Retorna o caminho onde os layouts são salvos
        /// </summary>
        public string ObterCaminhoLayouts()
        {
            return _caminhoLayouts;
        }

        #region Métodos Privados

        /// <summary>
        /// Carrega layout de um arquivo específico
        /// </summary>
        private async Task<LayoutModel?> CarregarLayoutDoArquivoAsync(string caminhoArquivo)
        {
            try
            {
                var json = await File.ReadAllTextAsync(caminhoArquivo);
                var layout = JsonSerializer.Deserialize<LayoutModel>(json, _jsonOptions);
                
                return layout;
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, "Erro ao deserializar layout: {Arquivo}", caminhoArquivo);
                return null;
            }
        }

        /// <summary>
        /// Atualiza metadados do layout
        /// </summary>
        private void AtualizarMetadados(LayoutModel layout)
        {
            var metadados = layout.Metadados;
            
            metadados.CamposMapeados = layout.Mapeamentos.Count(m => m.IndiceColuna >= 0);
            metadados.TotalColunas = layout.Mapeamentos.Select(m => m.IndiceColuna).Where(i => i >= 0).Distinct().Count();
            
            var camposObrigatorios = ObterCamposObrigatorios();
            var camposMapeados = layout.Mapeamentos.Where(m => m.IndiceColuna >= 0).Select(m => m.CampoAPI).ToHashSet();
            
            metadados.CamposObrigatorios = camposObrigatorios.Count;
            metadados.CamposObrigatoriosMapeados = camposObrigatorios.Count(c => camposMapeados.Contains(c));

            // Estatísticas por categoria
            metadados.EstatisticasPorCategoria.Clear();
            var categorias = layout.Mapeamentos
                .Where(m => m.IndiceColuna >= 0)
                .GroupBy(m => ObterCategoriaCampo(m.CampoAPI))
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var categoria in categorias)
            {
                metadados.EstatisticasPorCategoria[categoria.Key] = categoria.Value;
            }

            metadados.Categorias = metadados.EstatisticasPorCategoria.Keys.ToList();
        }

        /// <summary>
        /// Retorna lista de campos obrigatórios da API
        /// </summary>
        private List<string> ObterCamposObrigatorios()
        {
            return new List<string>
            {
                "nomeFuncionario",
                "dataAdmissao", 
                "dataNascimento",
                "sexo",
                "estadoCivil",
                "codigoEmpresa",
                "tipoContratacao",
                "regimeTrabalho"
            };
        }

        /// <summary>
        /// Determina a categoria de um campo da API
        /// </summary>
        private string ObterCategoriaCampo(string campoAPI)
        {
            if (campoAPI.StartsWith("cargo.")) return "Cargo";
            if (campoAPI.StartsWith("setor.")) return "Setor";
            if (campoAPI.StartsWith("centroCusto.")) return "Centro de Custo";
            if (campoAPI.StartsWith("unidade.")) return "Unidade";
            if (campoAPI.StartsWith("turno.")) return "Turno";
            if (new[] { "codigoEmpresa", "tipoContratacao", "regimeTrabalho" }.Contains(campoAPI)) return "Identificação";
            
            return "Funcionário";
        }

        #endregion
    }
}