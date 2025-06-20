using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharingMezzi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSegnalazioneManutenzione : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttuatoriSblocco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Stato = table.Column<int>(type: "INTEGER", nullable: false),
                    UltimaAttivazione = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttuatoriSblocco", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parcheggi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Indirizzo = table.Column<string>(type: "TEXT", nullable: false),
                    Capienza = table.Column<int>(type: "INTEGER", nullable: false),
                    PostiLiberi = table.Column<int>(type: "INTEGER", nullable: false),
                    PostiOccupati = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parcheggi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utenti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Cognome = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    Telefono = table.Column<string>(type: "TEXT", nullable: true),
                    Ruolo = table.Column<int>(type: "INTEGER", nullable: false),
                    Credito = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    PuntiEco = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CreditoMinimo = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false, defaultValue: 5.00m),
                    Stato = table.Column<int>(type: "INTEGER", nullable: false),
                    DataSospensione = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MotivoSospensione = table.Column<string>(type: "TEXT", nullable: true),
                    DataRegistrazione = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utenti", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ricariche",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Importo = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    MetodoPagamento = table.Column<int>(type: "INTEGER", nullable: false),
                    DataRicarica = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<string>(type: "TEXT", nullable: true),
                    Stato = table.Column<int>(type: "INTEGER", nullable: false),
                    UtenteId = table.Column<int>(type: "INTEGER", nullable: false),
                    SaldoPrecedente = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    SaldoFinale = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ricariche", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ricariche_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Corse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Inizio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Fine = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DurataMinuti = table.Column<int>(type: "INTEGER", nullable: false),
                    CostoTotale = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Stato = table.Column<int>(type: "INTEGER", nullable: false),
                    UtenteId = table.Column<int>(type: "INTEGER", nullable: false),
                    MezzoId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParcheggioPartenzaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParcheggioDestinazioneId = table.Column<int>(type: "INTEGER", nullable: true),
                    PuntiEcoAssegnati = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Corse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Corse_Parcheggi_ParcheggioDestinazioneId",
                        column: x => x.ParcheggioDestinazioneId,
                        principalTable: "Parcheggi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Corse_Parcheggi_ParcheggioPartenzaId",
                        column: x => x.ParcheggioPartenzaId,
                        principalTable: "Parcheggi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Corse_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pagamenti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Importo = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Stato = table.Column<int>(type: "INTEGER", nullable: false),
                    Metodo = table.Column<int>(type: "INTEGER", nullable: false),
                    DataPagamento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<string>(type: "TEXT", nullable: true),
                    UtenteId = table.Column<int>(type: "INTEGER", nullable: false),
                    CorsaId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagamenti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagamenti_Corse_CorsaId",
                        column: x => x.CorsaId,
                        principalTable: "Corse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Pagamenti_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mezzi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Modello = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    IsElettrico = table.Column<bool>(type: "INTEGER", nullable: false),
                    Stato = table.Column<int>(type: "INTEGER", nullable: false),
                    LivelloBatteria = table.Column<int>(type: "INTEGER", nullable: true),
                    TariffaPerMinuto = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    TariffaFissa = table.Column<decimal>(type: "TEXT", nullable: false),
                    UltimaManutenzione = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ParcheggioId = table.Column<int>(type: "INTEGER", nullable: true),
                    SlotId = table.Column<int>(type: "INTEGER", nullable: true),
                    SlotId2 = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mezzi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mezzi_Parcheggi_ParcheggioId",
                        column: x => x.ParcheggioId,
                        principalTable: "Parcheggi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SegnalazioniManutenzione",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MezzoId = table.Column<int>(type: "INTEGER", nullable: false),
                    UtenteId = table.Column<int>(type: "INTEGER", nullable: false),
                    CorsaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Descrizione = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Stato = table.Column<int>(type: "INTEGER", nullable: false),
                    Priorita = table.Column<int>(type: "INTEGER", nullable: false),
                    DataSegnalazione = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataRisoluzione = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NoteRisoluzione = table.Column<string>(type: "TEXT", nullable: true),
                    TecnicoId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SegnalazioniManutenzione", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SegnalazioniManutenzione_Corse_CorsaId",
                        column: x => x.CorsaId,
                        principalTable: "Corse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SegnalazioniManutenzione_Mezzi_MezzoId",
                        column: x => x.MezzoId,
                        principalTable: "Mezzi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SegnalazioniManutenzione_Utenti_TecnicoId",
                        column: x => x.TecnicoId,
                        principalTable: "Utenti",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SegnalazioniManutenzione_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SensoriBatteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MezzoId = table.Column<int>(type: "INTEGER", nullable: false),
                    LivelloBatteria = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsAttivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensoriBatteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensoriBatteria_Mezzi_MezzoId",
                        column: x => x.MezzoId,
                        principalTable: "Mezzi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Slots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    Stato = table.Column<int>(type: "INTEGER", nullable: false),
                    DataUltimoAggiornamento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ParcheggioId = table.Column<int>(type: "INTEGER", nullable: false),
                    MezzoId = table.Column<int>(type: "INTEGER", nullable: true),
                    AttuatoreSbloccoId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Slots_AttuatoriSblocco_AttuatoreSbloccoId",
                        column: x => x.AttuatoreSbloccoId,
                        principalTable: "AttuatoriSblocco",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Slots_Mezzi_MezzoId",
                        column: x => x.MezzoId,
                        principalTable: "Mezzi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Slots_Parcheggi_ParcheggioId",
                        column: x => x.ParcheggioId,
                        principalTable: "Parcheggi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Corse_MezzoId",
                table: "Corse",
                column: "MezzoId");

            migrationBuilder.CreateIndex(
                name: "IX_Corse_ParcheggioDestinazioneId",
                table: "Corse",
                column: "ParcheggioDestinazioneId");

            migrationBuilder.CreateIndex(
                name: "IX_Corse_ParcheggioPartenzaId",
                table: "Corse",
                column: "ParcheggioPartenzaId");

            migrationBuilder.CreateIndex(
                name: "IX_Corse_UtenteId",
                table: "Corse",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Mezzi_ParcheggioId",
                table: "Mezzi",
                column: "ParcheggioId");

            migrationBuilder.CreateIndex(
                name: "IX_Mezzi_SlotId2",
                table: "Mezzi",
                column: "SlotId2");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamenti_CorsaId",
                table: "Pagamenti",
                column: "CorsaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pagamenti_UtenteId",
                table: "Pagamenti",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Ricariche_UtenteId",
                table: "Ricariche",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_SegnalazioniManutenzione_CorsaId",
                table: "SegnalazioniManutenzione",
                column: "CorsaId");

            migrationBuilder.CreateIndex(
                name: "IX_SegnalazioniManutenzione_MezzoId",
                table: "SegnalazioniManutenzione",
                column: "MezzoId");

            migrationBuilder.CreateIndex(
                name: "IX_SegnalazioniManutenzione_TecnicoId",
                table: "SegnalazioniManutenzione",
                column: "TecnicoId");

            migrationBuilder.CreateIndex(
                name: "IX_SegnalazioniManutenzione_UtenteId",
                table: "SegnalazioniManutenzione",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_SensoriBatteria_MezzoId",
                table: "SensoriBatteria",
                column: "MezzoId");

            migrationBuilder.CreateIndex(
                name: "IX_Slots_AttuatoreSbloccoId",
                table: "Slots",
                column: "AttuatoreSbloccoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Slots_MezzoId",
                table: "Slots",
                column: "MezzoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Slots_ParcheggioId",
                table: "Slots",
                column: "ParcheggioId");

            migrationBuilder.CreateIndex(
                name: "IX_Utenti_Email",
                table: "Utenti",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Corse_Mezzi_MezzoId",
                table: "Corse",
                column: "MezzoId",
                principalTable: "Mezzi",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Mezzi_Slots_SlotId2",
                table: "Mezzi",
                column: "SlotId2",
                principalTable: "Slots",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Slots_Mezzi_MezzoId",
                table: "Slots");

            migrationBuilder.DropTable(
                name: "Pagamenti");

            migrationBuilder.DropTable(
                name: "Ricariche");

            migrationBuilder.DropTable(
                name: "SegnalazioniManutenzione");

            migrationBuilder.DropTable(
                name: "SensoriBatteria");

            migrationBuilder.DropTable(
                name: "Corse");

            migrationBuilder.DropTable(
                name: "Utenti");

            migrationBuilder.DropTable(
                name: "Mezzi");

            migrationBuilder.DropTable(
                name: "Slots");

            migrationBuilder.DropTable(
                name: "AttuatoriSblocco");

            migrationBuilder.DropTable(
                name: "Parcheggi");
        }
    }
}
