using Microsoft.EntityFrameworkCore;
using SharingMezzi.Core.Entities;

namespace SharingMezzi.Infrastructure.Database
{
    public class SharingMezziContext : DbContext
    {
        public SharingMezziContext(DbContextOptions<SharingMezziContext> options) : base(options) { }

        public DbSet<Mezzo> Mezzi { get; set; }
        public DbSet<Parcheggio> Parcheggi { get; set; }
        public DbSet<Slot> Slots { get; set; }
        public DbSet<Utente> Utenti { get; set; }
        public DbSet<Corsa> Corse { get; set; }
        public DbSet<Pagamento> Pagamenti { get; set; }        public DbSet<Ricarica> Ricariche { get; set; } // NUOVO
        public DbSet<SegnalazioneManutenzione> SegnalazioniManutenzione { get; set; }
        public DbSet<SensoreBatteria> SensoriBatteria { get; set; }
        public DbSet<AttuatoreSblocco> AttuatoriSblocco { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configurazioni entità
            modelBuilder.Entity<Mezzo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Modello).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TariffaPerMinuto).HasPrecision(10, 2);
                
                entity.HasOne(e => e.Parcheggio)
                    .WithMany(p => p.Mezzi)
                    .HasForeignKey(e => e.ParcheggioId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Utente>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Credito).HasPrecision(10, 2).HasDefaultValue(0);
                entity.Property(e => e.PuntiEco).HasDefaultValue(0);
                entity.Property(e => e.CreditoMinimo).HasPrecision(10, 2).HasDefaultValue(5.00m);
            });

            modelBuilder.Entity<Corsa>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CostoTotale).HasPrecision(10, 2);
                
                entity.HasOne(e => e.ParcheggioPartenza)
                    .WithMany(p => p.CorsePartenza)
                    .HasForeignKey(e => e.ParcheggioPartenzaId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.ParcheggioDestinazione)
                    .WithMany(p => p.CorseDestinazione)
                    .HasForeignKey(e => e.ParcheggioDestinazioneId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Pagamento>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Importo).HasPrecision(10, 2);
                
                entity.HasOne(e => e.Corsa)
                    .WithOne(c => c.Pagamento)
                    .HasForeignKey<Pagamento>(e => e.CorsaId)
                    .OnDelete(DeleteBehavior.SetNull);
            });            // NUOVO: Configurazione Ricariche
            modelBuilder.Entity<Ricarica>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Importo).HasPrecision(10, 2);
                entity.Property(e => e.SaldoPrecedente).HasPrecision(10, 2);
                entity.Property(e => e.SaldoFinale).HasPrecision(10, 2);
                
                entity.HasOne(e => e.Utente)
                    .WithMany(u => u.Ricariche)
                    .HasForeignKey(e => e.UtenteId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // NUOVO: Configurazione Segnalazioni Manutenzione
            modelBuilder.Entity<SegnalazioneManutenzione>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Descrizione).HasMaxLength(500);
                
                entity.HasOne(e => e.Mezzo)
                    .WithMany()
                    .HasForeignKey(e => e.MezzoId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.Utente)
                    .WithMany()
                    .HasForeignKey(e => e.UtenteId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Corsa)
                    .WithMany()
                    .HasForeignKey(e => e.CorsaId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configurazione relazione Slot - AttuatoreSblocco (uno a uno)
            modelBuilder.Entity<Slot>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.AttuatoreSblocco)
                    .WithOne(a => a.Slot)
                    .HasForeignKey<Slot>(e => e.AttuatoreSbloccoId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                entity.HasOne(e => e.Parcheggio)
                    .WithMany(p => p.Slots)
                    .HasForeignKey(e => e.ParcheggioId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.Mezzo)
                    .WithOne()
                    .HasForeignKey<Slot>(e => e.MezzoId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AttuatoreSblocco>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SerialNumber).IsRequired().HasMaxLength(50);
            });
        }
    }
}