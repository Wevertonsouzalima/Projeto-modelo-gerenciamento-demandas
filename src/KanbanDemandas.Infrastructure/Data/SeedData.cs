using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace KanbanDemandas.Infrastructure.Data;

/// <summary>
/// Seed de dados iniciais aplicado via HasData no OnModelCreating.
/// Fornece usuários, sistemas e um quadro de exemplo para primeiro uso.
/// </summary>
public static class SeedData
{
    private static readonly DateTime DataSeed = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void Aplicar(ModelBuilder modelBuilder)
    {
        SeedUsuarios(modelBuilder);
        SeedSistemas(modelBuilder);
        SeedQuadroExemplo(modelBuilder);
    }

    private static void SeedUsuarios(ModelBuilder mb)
    {
        mb.Entity<Usuario>().HasData(
            new Usuario
            {
                Id = 1, Nome = "Admin", Email = "admin@empresa.com",
                Ativo = true, NotificarPorEmail = false,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Usuario
            {
                Id = 2, Nome = "Ana Lima", Email = "ana.lima@empresa.com",
                Ativo = true, NotificarPorEmail = true,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Usuario
            {
                Id = 3, Nome = "Bruno Costa", Email = "bruno.costa@empresa.com",
                Ativo = true, NotificarPorEmail = true,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Usuario
            {
                Id = 4, Nome = "Carla Mendes", Email = "carla.mendes@empresa.com",
                Ativo = true, NotificarPorEmail = false,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Usuario
            {
                Id = 5, Nome = "Diego Santos", Email = "diego.santos@empresa.com",
                Ativo = true, NotificarPorEmail = true,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            }
        );
    }

    private static void SeedSistemas(ModelBuilder mb)
    {
        mb.Entity<Sistema>().HasData(
            new Sistema
            {
                Id = 1, Nome = "Portal do Cliente", Ativo = true,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Sistema
            {
                Id = 2, Nome = "ERP Interno", Ativo = true,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Sistema
            {
                Id = 3, Nome = "App Mobile", Ativo = true,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Sistema
            {
                Id = 4, Nome = "Integrações", Ativo = true,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            }
        );
    }

    private static void SeedQuadroExemplo(ModelBuilder mb)
    {
        // Quadro de demonstração
        mb.Entity<Quadro>().HasData(new Quadro
        {
            Id = 1, Nome = "Time de Desenvolvimento", Descricao = "Quadro principal do time",
            Cor = "#1565C0", CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
        });

        // Listas padrão
        mb.Entity<Lista>().HasData(
            new Lista
            {
                Id = 1, Nome = "Backlog", QuadroId = 1, Ordem = 1,
                EhBacklog = true, LimiteWip = null,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Lista
            {
                Id = 2, Nome = "A Fazer", QuadroId = 1, Ordem = 2,
                LimiteWip = 10,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Lista
            {
                Id = 3, Nome = "Em Andamento", QuadroId = 1, Ordem = 3,
                LimiteWip = 5,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Lista
            {
                Id = 4, Nome = "Code Review", QuadroId = 1, Ordem = 4,
                LimiteWip = 3,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Lista
            {
                Id = 5, Nome = "Homologação", QuadroId = 1, Ordem = 5,
                LimiteWip = null,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Lista
            {
                Id = 6, Nome = "Pré-produção", QuadroId = 1, Ordem = 6,
                LimiteWip = null,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Lista
            {
                Id = 7, Nome = "Concluído", QuadroId = 1, Ordem = 7,
                LimiteWip = null,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            }
        );

        // Campos configuráveis de exemplo: Homologação e Pré-produção
        mb.Entity<DefinicaoCampo>().HasData(
            new DefinicaoCampo
            {
                Id = 1, Nome = "Data Homologado", Tipo = TipoCampo.Data,
                ListaId = 5, Obrigatorio = true, PedirNovamenteACadaEntrada = true,
                Ordem = 1, CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new DefinicaoCampo
            {
                Id = 2, Nome = "Homologado Por", Tipo = TipoCampo.Texto,
                ListaId = 5, Obrigatorio = false, PedirNovamenteACadaEntrada = false,
                Ordem = 2, CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new DefinicaoCampo
            {
                Id = 3, Nome = "Data Prevista de Change", Tipo = TipoCampo.Data,
                ListaId = 6, Obrigatorio = true, PedirNovamenteACadaEntrada = false,
                Ordem = 1, CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new DefinicaoCampo
            {
                Id = 4, Nome = "Número do Change", Tipo = TipoCampo.Texto,
                ListaId = 6, Obrigatorio = false, PedirNovamenteACadaEntrada = false,
                Ordem = 2, CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            }
        );

        // Etiquetas padrão do quadro
        mb.Entity<Etiqueta>().HasData(
            new Etiqueta
            {
                Id = 1, Nome = "Bug", Cor = "#E53935", QuadroId = 1,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Etiqueta
            {
                Id = 2, Nome = "Feature", Cor = "#43A047", QuadroId = 1,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Etiqueta
            {
                Id = 3, Nome = "Melhoria", Cor = "#1E88E5", QuadroId = 1,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Etiqueta
            {
                Id = 4, Nome = "Urgente", Cor = "#FB8C00", QuadroId = 1,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            },
            new Etiqueta
            {
                Id = 5, Nome = "Técnico", Cor = "#8E24AA", QuadroId = 1,
                CriadoEm = DataSeed, CriadoPorId = 1, Excluido = false
            }
        );
    }
}
