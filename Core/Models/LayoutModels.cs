using System;
using System.Collections.Generic;

namespace ImportadorModelo2.Core.Models
{
    /// <summary>
    /// Modelo principal do layout de importação
    /// </summary>
    public class LayoutModel
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataAtualizacao { get; set; }
        public string VersaoSistema { get; set; } = "1.0.0";
        public int LinhaHeader { get; set; } = 1;
        public string? NomeArquivoOrigem { get; set; }
        public RegrasNegocioModel RegrasNegocio { get; set; } = new();
        public List<MapeamentoCampoModel> Mapeamentos { get; set; } = new();
        public MetadadosLayoutModel Metadados { get; set; } = new();
    }

    /// <summary>
    /// Regras de negócio para importação
    /// </summary>
    public class RegrasNegocioModel
    {
        // Criação automática
        public bool CriarFuncionario { get; set; } = true;
        public bool CriarCargo { get; set; } = true;
        public bool CriarSetor { get; set; } = true;
        public bool CriarCentroCusto { get; set; } = true;
        public bool CriarUnidade { get; set; } = true;
        public bool CriarTurno { get; set; } = true;
        public bool CriarMotivoLicenca { get; set; } = false;
        public bool CriarUnidadeContratante { get; set; } = false;

        // Atualização automática
        public bool AtualizarFuncionario { get; set; } = true;
        public bool AtualizarCargo { get; set; } = false;
        public bool AtualizarSetor { get; set; } = false;
        public bool AtualizarCentroCusto { get; set; } = false;
        public bool AtualizarUnidade { get; set; } = false;
        public bool AtualizarTurno { get; set; } = false;
        public bool AtualizarMotivoLicenca { get; set; } = false;

        // Outras configurações
        public bool CriarHistorico { get; set; } = true;
        public bool DestravarBloqueado { get; set; } = false;
        public bool NaoImportarSemHierarquia { get; set; } = true;
    }

    /// <summary>
    /// Mapeamento entre campo da API e coluna da planilha
    /// </summary>
    public class MapeamentoCampoModel
    {
        public string CampoAPI { get; set; } = string.Empty;
        public string ColunaPlanilha { get; set; } = string.Empty;
        public int IndiceColuna { get; set; }
        public string? ValorPadrao { get; set; }
        public bool Obrigatorio { get; set; }
        public string TipoDado { get; set; } = "string";
        public string? Formato { get; set; }
        public List<string>? ValoresPermitidos { get; set; }
        public TransformacaoModel? Transformacao { get; set; }
    }

    /// <summary>
    /// Transformações a serem aplicadas nos dados
    /// </summary>
    public class TransformacaoModel
    {
        public string Tipo { get; set; } = string.Empty; // "uppercase", "lowercase", "trim", "format_date", "format_cpf", etc.
        public Dictionary<string, object> Parametros { get; set; } = new();
    }

    /// <summary>
    /// Metadados do layout
    /// </summary>
    public class MetadadosLayoutModel
    {
        public int TotalColunas { get; set; }
        public int CamposMapeados { get; set; }
        public int CamposObrigatorios { get; set; }
        public int CamposObrigatoriosMapeados { get; set; }
        public List<string> Categorias { get; set; } = new();
        public Dictionary<string, int> EstatisticasPorCategoria { get; set; } = new();
        public string? UltimoTeste { get; set; }
        public bool TesteRealizado { get; set; }
        public List<string> Observacoes { get; set; } = new();
    }

    /// <summary>
    /// Resultado de validação do layout
    /// </summary>
    public class ValidacaoLayoutModel
    {
        public bool Valido { get; set; }
        public List<string> Erros { get; set; } = new();
        public List<string> Avisos { get; set; } = new();
        public List<CampoValidacaoModel> CamposValidacao { get; set; } = new();
    }

    /// <summary>
    /// Validação específica de um campo
    /// </summary>
    public class CampoValidacaoModel
    {
        public string NomeCampo { get; set; } = string.Empty;
        public bool Valido { get; set; }
        public string? Erro { get; set; }
        public string? Sugestao { get; set; }
    }

    /// <summary>
    /// Configurações específicas da API
    /// </summary>
    public class ConfiguracaoAPIModel
    {
        public string ChaveAcesso { get; set; } = string.Empty;
        public string CodigoEmpresaPrincipal { get; set; } = string.Empty;
        public string CodigoResponsavel { get; set; } = string.Empty;
        public string CodigoUsuario { get; set; } = string.Empty;
        public string UrlAPI { get; set; } = string.Empty;
        public int TimeoutSegundos { get; set; } = 30;
        public bool LogarRequisicoes { get; set; } = true;
    }

    /// <summary>
    /// Enum para tipos de busca da API
    /// </summary>
    public enum TipoBuscaEnum
    {
        CODIGO,
        CODIGO_RH,
        NOME,
        CPF,
        MATRICULA,
        MATRICULA_RH,
        DATA_NASCIMENTO,
        CPF_PENDENTE,
        CPF_ATIVO,
        CPF_DATA_ADMISSAO,
        CPF_DATA_ADMISSAO_PERIODO
    }

    /// <summary>
    /// Enum para tipos de contratação
    /// </summary>
    public enum TipoContratacaoEnum
    {
        CLT,
        COOPERADO,
        TERCERIZADO,
        AUTONOMO,
        TEMPORARIO,
        PESSOA_JURIDICA,
        ESTAGIARIO,
        MENOR_APRENDIZ,
        ESTATUTARIO,
        COMISSIONADO_INTERNO,
        COMISSIONADO_EXTERNO,
        APOSENTADO,
        APOSENTADO_INATIVO_PREFEITURA,
        PENSIONISTA,
        SERVIDOR_PUBLICO_EFETIVO,
        EXTRANUMERARIO,
        AUTARQUICO,
        INATIVO,
        TITULO_PRECARIO,
        SERVIDOR_ADM_CENTRALIZADA_OU_DESCENTRALIZADA
    }

    /// <summary>
    /// Enum para regime de trabalho
    /// </summary>
    public enum RegimeTrabalhoEnum
    {
        NORMAL,
        TURNO
    }

    /// <summary>
    /// Enum para sexo
    /// </summary>
    public enum SexoEnum
    {
        MASCULINO,
        FEMININO
    }

    /// <summary>
    /// Enum para estado civil
    /// </summary>
    public enum EstadoCivilEnum
    {
        SOLTEIRO,
        CASADO,
        SEPARADO,
        DIVORCIADO,
        VIUVO,
        OUTROS,
        DESQUITADO,
        UNIAO_ESTAVEL
    }

    /// <summary>
    /// Enum para situação do funcionário
    /// </summary>
    public enum SituacaoFuncionarioEnum
    {
        ATIVO,
        AFASTADO,
        PENDENTE,
        FERIAS,
        INATIVO
    }

    /// <summary>
    /// Enum para estados brasileiros
    /// </summary>
    public enum EstadoBrasileiroEnum
    {
        AC, AL, AM, AP, BA, CE, DF, ES, GO, MA, MG, MS, MT, PA, PB, PE, PI, PR, RJ, RN, RO, RR, RS, SC, SE, SP, TO
    }
}