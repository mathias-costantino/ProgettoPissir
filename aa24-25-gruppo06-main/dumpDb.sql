PRAGMA foreign_keys=OFF;
BEGIN TRANSACTION;
CREATE TABLE IF NOT EXISTS "AttuatoriSblocco" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AttuatoriSblocco" PRIMARY KEY AUTOINCREMENT,
    "SerialNumber" TEXT NOT NULL,
    "Stato" INTEGER NOT NULL,
    "UltimaAttivazione" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS "AttuatoriSblocco" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AttuatoriSblocco" PRIMARY KEY AUTOINCREMENT,
    "SerialNumber" TEXT NOT NULL,
    "Stato" INTEGER NOT NULL,
    "UltimaAttivazione" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL
, MezzoId INTEGER NULL);
INSERT INTO AttuatoriSblocco VALUES(1,'ACT-BIKE-001',0,'2024-06-17 08:00:00','2024-06-17 08:00:00','2024-06-17 08:00:00',1);
INSERT INTO AttuatoriSblocco VALUES(2,'ACT-EBIKE-002',0,'2024-06-17 08:00:00','2024-06-17 08:00:00','2024-06-17 08:00:00',2);
INSERT INTO AttuatoriSblocco VALUES(3,'ACT-EBIKE-003',0,'2024-06-17 08:00:00','2024-06-17 08:00:00','2024-06-17 08:00:00',3);
INSERT INTO AttuatoriSblocco VALUES(4,'ACT-SCOOT-004',0,'2024-06-17 08:00:00','2024-06-17 08:00:00','2024-06-17 08:00:00',4);
INSERT INTO AttuatoriSblocco VALUES(5,'ACT-SCOOT-005',0,'2024-06-17 08:00:00','2024-06-17 08:00:00','2024-06-17 08:00:00',5);
INSERT INTO AttuatoriSblocco VALUES(6,'ACT-BIKE-006',0,'2024-06-17 08:00:00','2024-06-17 08:00:00','2024-06-17 08:00:00',6);
CREATE TABLE IF NOT EXISTS "Parcheggi" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Parcheggi" PRIMARY KEY AUTOINCREMENT,
    "Nome" TEXT NOT NULL,
    "Indirizzo" TEXT NOT NULL,
    "Capienza" INTEGER NOT NULL,
    "PostiLiberi" INTEGER NOT NULL,
    "PostiOccupati" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL
);
INSERT INTO Parcheggi VALUES(1,'Centro Storico','Piazza Castello 1',25,20,5,'2025-06-17 08:44:17.126396','2025-06-17 08:44:17.126397');
INSERT INTO Parcheggi VALUES(2,'Politecnico','Corso Duca Abruzzi 24',40,30,10,'2025-06-17 08:44:17.126636','2025-06-17 08:44:17.126636');
INSERT INTO Parcheggi VALUES(3,'Porta Nuova','Piazza Carlo Felice 1',30,25,5,'2025-06-17 08:44:17.126637','2025-06-17 08:44:17.126637');
CREATE TABLE IF NOT EXISTS "Utenti" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Utenti" PRIMARY KEY AUTOINCREMENT,
    "Email" TEXT NOT NULL,
    "Nome" TEXT NOT NULL,
    "Cognome" TEXT NOT NULL,
    "Password" TEXT NOT NULL,
    "Telefono" TEXT NULL,
    "Ruolo" INTEGER NOT NULL,
    "Credito" TEXT NOT NULL DEFAULT '0.0',
    "PuntiEco" INTEGER NOT NULL DEFAULT 0,
    "CreditoMinimo" TEXT NOT NULL DEFAULT '5.0',
    "Stato" INTEGER NOT NULL,
    "DataSospensione" TEXT NULL,
    "MotivoSospensione" TEXT NULL,
    "DataRegistrazione" TEXT NOT NULL,
    "RefreshToken" TEXT NULL,
    "RefreshTokenExpiryTime" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL
);
INSERT INTO Utenti VALUES(1,'admin@test.com','Admin','System','JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=',NULL,1,'0.0',0,'5.0',0,NULL,NULL,'2025-06-17 08:44:16.958759',NULL,NULL,'2025-06-17 08:44:16.91754','2025-06-17 08:44:16.91754');
INSERT INTO Utenti VALUES(2,'mario@test.com','Mario','Rossi','5gbjiw2MGbJM8O44CBgxYup81j/3kS27IrXoAyhrREY=','3331234567',0,'0.0',0,'5.0',0,NULL,NULL,'2025-06-17 08:44:16.958951',NULL,NULL,'2025-06-17 08:44:16.958822','2025-06-17 08:44:16.958822');
INSERT INTO Utenti VALUES(3,'lucia@test.com','Lucia','Verdi','5gbjiw2MGbJM8O44CBgxYup81j/3kS27IrXoAyhrREY=','3337654321',0,'0.0',0,'5.0',0,NULL,NULL,'2025-06-17 08:44:16.958963',NULL,NULL,'2025-06-17 08:44:16.958952','2025-06-17 08:44:16.958952');
CREATE TABLE IF NOT EXISTS "Ricariche" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Ricariche" PRIMARY KEY AUTOINCREMENT,
    "Importo" TEXT NOT NULL,
    "MetodoPagamento" INTEGER NOT NULL,
    "DataRicarica" TEXT NOT NULL,
    "TransactionId" TEXT NULL,
    "Stato" INTEGER NOT NULL,
    "UtenteId" INTEGER NOT NULL,
    "SaldoPrecedente" TEXT NOT NULL,
    "SaldoFinale" TEXT NOT NULL,
    "Note" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Ricariche_Utenti_UtenteId" FOREIGN KEY ("UtenteId") REFERENCES "Utenti" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Corse" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Corse" PRIMARY KEY AUTOINCREMENT,
    "Inizio" TEXT NOT NULL,
    "Fine" TEXT NULL,
    "DurataMinuti" INTEGER NOT NULL,
    "CostoTotale" TEXT NOT NULL,
    "Stato" INTEGER NOT NULL,
    "UtenteId" INTEGER NOT NULL,
    "MezzoId" INTEGER NOT NULL,
    "ParcheggioPartenzaId" INTEGER NOT NULL,
    "ParcheggioDestinazioneId" INTEGER NULL,
    "PuntiEcoAssegnati" INTEGER NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Corse_Parcheggi_ParcheggioDestinazioneId" FOREIGN KEY ("ParcheggioDestinazioneId") REFERENCES "Parcheggi" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Corse_Parcheggi_ParcheggioPartenzaId" FOREIGN KEY ("ParcheggioPartenzaId") REFERENCES "Parcheggi" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Corse_Utenti_UtenteId" FOREIGN KEY ("UtenteId") REFERENCES "Utenti" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Corse_Mezzi_MezzoId" FOREIGN KEY ("MezzoId") REFERENCES "Mezzi" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Pagamenti" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Pagamenti" PRIMARY KEY AUTOINCREMENT,
    "Importo" TEXT NOT NULL,
    "Stato" INTEGER NOT NULL,
    "Metodo" INTEGER NOT NULL,
    "DataPagamento" TEXT NOT NULL,
    "TransactionId" TEXT NULL,
    "UtenteId" INTEGER NOT NULL,
    "CorsaId" INTEGER NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Pagamenti_Corse_CorsaId" FOREIGN KEY ("CorsaId") REFERENCES "Corse" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Pagamenti_Utenti_UtenteId" FOREIGN KEY ("UtenteId") REFERENCES "Utenti" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Mezzi" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Mezzi" PRIMARY KEY AUTOINCREMENT,
    "Modello" TEXT NOT NULL,
    "Tipo" INTEGER NOT NULL,
    "IsElettrico" INTEGER NOT NULL,
    "Stato" INTEGER NOT NULL,
    "LivelloBatteria" INTEGER NULL,
    "TariffaPerMinuto" TEXT NOT NULL,
    "TariffaFissa" TEXT NOT NULL,
    "UltimaManutenzione" TEXT NULL,
    "ParcheggioId" INTEGER NULL,
    "SlotId" INTEGER NULL,
    "SlotId2" INTEGER NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Mezzi_Parcheggi_ParcheggioId" FOREIGN KEY ("ParcheggioId") REFERENCES "Parcheggi" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Mezzi_Slots_SlotId2" FOREIGN KEY ("SlotId2") REFERENCES "Slots" ("Id")
);
INSERT INTO Mezzi VALUES(1,'City Bike Classic',0,0,0,NULL,'0.15','1.0',NULL,1,NULL,NULL,'2025-06-17 08:44:17.407667','2025-06-17 08:44:17.407667');
INSERT INTO Mezzi VALUES(2,'E-Bike Urban Pro',1,1,0,95,'0.25','1.0',NULL,1,NULL,NULL,'2025-06-17 08:44:17.408353','2025-06-17 08:44:17.408353');
INSERT INTO Mezzi VALUES(3,'E-Bike Mountain',1,1,0,78,'0.3','1.0',NULL,2,NULL,NULL,'2025-06-17 08:44:17.4085','2025-06-17 08:44:17.4085');
INSERT INTO Mezzi VALUES(4,'Urban Scooter X1',2,1,0,82,'0.35','1.0',NULL,2,NULL,NULL,'2025-06-17 08:44:17.408501','2025-06-17 08:44:17.408501');
INSERT INTO Mezzi VALUES(5,'Eco Scooter Lite',2,1,0,67,'0.3','1.0',NULL,3,NULL,NULL,'2025-06-17 08:44:17.408502','2025-06-17 08:44:17.408502');
INSERT INTO Mezzi VALUES(6,'City Bike Sport',0,0,0,NULL,'0.18','1.0',NULL,3,NULL,NULL,'2025-06-17 08:44:17.408502','2025-06-17 08:44:17.408503');
CREATE TABLE IF NOT EXISTS "SegnalazioniManutenzione" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_SegnalazioniManutenzione" PRIMARY KEY AUTOINCREMENT,
    "MezzoId" INTEGER NOT NULL,
    "UtenteId" INTEGER NOT NULL,
    "CorsaId" INTEGER NULL,
    "Descrizione" TEXT NOT NULL,
    "Stato" INTEGER NOT NULL,
    "Priorita" INTEGER NOT NULL,
    "DataSegnalazione" TEXT NOT NULL,
    "DataRisoluzione" TEXT NULL,
    "NoteRisoluzione" TEXT NULL,
    "TecnicoId" INTEGER NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_SegnalazioniManutenzione_Corse_CorsaId" FOREIGN KEY ("CorsaId") REFERENCES "Corse" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_SegnalazioniManutenzione_Mezzi_MezzoId" FOREIGN KEY ("MezzoId") REFERENCES "Mezzi" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SegnalazioniManutenzione_Utenti_TecnicoId" FOREIGN KEY ("TecnicoId") REFERENCES "Utenti" ("Id"),
    CONSTRAINT "FK_SegnalazioniManutenzione_Utenti_UtenteId" FOREIGN KEY ("UtenteId") REFERENCES "Utenti" ("Id") ON DELETE RESTRICT
);
CREATE TABLE IF NOT EXISTS "SensoriBatteria" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_SensoriBatteria" PRIMARY KEY AUTOINCREMENT,
    "MezzoId" INTEGER NOT NULL,
    "LivelloBatteria" INTEGER NOT NULL,
    "Timestamp" TEXT NOT NULL,
    "IsAttivo" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_SensoriBatteria_Mezzi_MezzoId" FOREIGN KEY ("MezzoId") REFERENCES "Mezzi" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Slots" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Slots" PRIMARY KEY AUTOINCREMENT,
    "Numero" INTEGER NOT NULL,
    "Stato" INTEGER NOT NULL,
    "DataUltimoAggiornamento" TEXT NOT NULL,
    "ParcheggioId" INTEGER NOT NULL,
    "MezzoId" INTEGER NULL,
    "AttuatoreSbloccoId" INTEGER NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Slots_AttuatoriSblocco_AttuatoreSbloccoId" FOREIGN KEY ("AttuatoreSbloccoId") REFERENCES "AttuatoriSblocco" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Slots_Mezzi_MezzoId" FOREIGN KEY ("MezzoId") REFERENCES "Mezzi" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Slots_Parcheggi_ParcheggioId" FOREIGN KEY ("ParcheggioId") REFERENCES "Parcheggi" ("Id") ON DELETE CASCADE
);
DELETE FROM sqlite_sequence;
INSERT INTO sqlite_sequence VALUES('Parcheggi',3);
INSERT INTO sqlite_sequence VALUES('Utenti',3);
INSERT INTO sqlite_sequence VALUES('Mezzi',6);
INSERT INTO sqlite_sequence VALUES('AttuatoriSblocco',6);
CREATE INDEX "IX_Corse_MezzoId" ON "Corse" ("MezzoId");
CREATE INDEX "IX_Corse_ParcheggioDestinazioneId" ON "Corse" ("ParcheggioDestinazioneId");
CREATE INDEX "IX_Corse_ParcheggioPartenzaId" ON "Corse" ("ParcheggioPartenzaId");
CREATE INDEX "IX_Corse_UtenteId" ON "Corse" ("UtenteId");
CREATE INDEX "IX_Mezzi_ParcheggioId" ON "Mezzi" ("ParcheggioId");
CREATE INDEX "IX_Mezzi_SlotId2" ON "Mezzi" ("SlotId2");
CREATE UNIQUE INDEX "IX_Pagamenti_CorsaId" ON "Pagamenti" ("CorsaId");
CREATE INDEX "IX_Pagamenti_UtenteId" ON "Pagamenti" ("UtenteId");
CREATE INDEX "IX_Ricariche_UtenteId" ON "Ricariche" ("UtenteId");
CREATE INDEX "IX_SegnalazioniManutenzione_CorsaId" ON "SegnalazioniManutenzione" ("CorsaId");
CREATE INDEX "IX_SegnalazioniManutenzione_MezzoId" ON "SegnalazioniManutenzione" ("MezzoId");
CREATE INDEX "IX_SegnalazioniManutenzione_TecnicoId" ON "SegnalazioniManutenzione" ("TecnicoId");
CREATE INDEX "IX_SegnalazioniManutenzione_UtenteId" ON "SegnalazioniManutenzione" ("UtenteId");
CREATE INDEX "IX_SensoriBatteria_MezzoId" ON "SensoriBatteria" ("MezzoId");
CREATE UNIQUE INDEX "IX_Slots_AttuatoreSbloccoId" ON "Slots" ("AttuatoreSbloccoId");
CREATE UNIQUE INDEX "IX_Slots_MezzoId" ON "Slots" ("MezzoId");
CREATE INDEX "IX_Slots_ParcheggioId" ON "Slots" ("ParcheggioId");
CREATE UNIQUE INDEX "IX_Utenti_Email" ON "Utenti" ("Email");
CREATE INDEX IX_AttuatoriSblocco_MezzoId ON AttuatoriSblocco (MezzoId);
COMMIT;
