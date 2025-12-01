using DevNationCrono.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevNationCrono.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets - cada tabela
    public DbSet<Piloto> Pilotos { get; set; }
    public DbSet<Modalidade> Modalidades { get; set; }
    public DbSet<Evento> Eventos { get; set; }
    public DbSet<Etapa> Etapas { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Inscricao> Inscricoes { get; set; }
    public DbSet<Tempo> Tempos { get; set; }
    public DbSet<DispositivoColetor> DispositivosColetores { get; set; }
    public DbSet<LogLeitura> LogLeituras { get; set; }
    public DbSet<Pagamento> Pagamentos { get; set; }
    public DbSet<Configuracao> Configuracoes { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurarPiloto(modelBuilder);
        ConfigurarInscricao(modelBuilder);
        ConfigurarTempo(modelBuilder);
    }

    private void ConfigurarPiloto(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Piloto>(entity =>
        {
            // Índices
            entity.HasIndex(e => e.Cpf).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Nome);

            // Conversões de data para UTC
            entity.Property(e => e.DataCriacao)
                .HasConversion(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            entity.Property(e => e.DataAtualizacao)
                .HasConversion(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });
    }

    private void ConfigurarInscricao(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Inscricao>(entity =>
        {
            // Chave composta única
            entity.HasIndex(e => new { e.IdPiloto, e.IdEvento, e.IdCategoria })
                .IsUnique();

            // Índices
            entity.HasIndex(e => e.NumeroMoto);
            entity.HasIndex(e => e.StatusPagamento);

            // Relacionamentos
            entity.HasOne(e => e.Piloto)
                .WithMany(p => p.Inscricoes)
                .HasForeignKey(e => e.IdPiloto)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Evento)
                .WithMany(ev => ev.Inscricoes)
                .HasForeignKey(e => e.IdEvento)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigurarTempo(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tempo>(entity =>
        {
            // Índices
            entity.HasIndex(e => e.NumeroMoto);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.IdEtapa, e.Volta });
            entity.HasIndex(e => e.Sincronizado);

            // Timestamp com precisão de milissegundos
            entity.Property(e => e.Timestamp)
                .HasPrecision(3);

            //// Decimal para tempo calculado
            //entity.Property(e => e.TempoCalculado)
            //    .HasPrecision(10, 3);
        });
    }
}

