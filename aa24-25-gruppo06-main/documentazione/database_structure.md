# SharingMezzi - Struttura Database

## Tabelle Principali

### 1. Utenti
Gestisce gli utenti del sistema (clienti e amministratori).

**Campi principali:**
- `Id` (PK) - Identificatore univoco
- `Email` (UNIQUE) - Email di accesso
- `Nome, Cognome` - Dati anagrafici
- `Password` - Password hashata
- `Ruolo` - 0=Utente, 1=Admin
- `Credito` - Saldo disponibile (DECIMAL)
- `PuntiEco` - Punti ecologici accumulati
- `Stato` - 0=Attivo, 1=Sospeso, 2=Disabilitato

### 2. Mezzi
Gestisce biciclette e scooter del sistema.

**Campi principali:**
- `Id` (PK) - Identificatore univoco
- `Modello` - Modello del mezzo
- `Tipo` - 0=BiciMuscolare, 1=BiciElettrica, 2=MonopattinoElettrico
- `IsElettrico` - Boolean per mezzi elettrici
- `Stato` - 0=Disponibile, 1=Occupato, 2=Manutenzione, 3=Guasto
- `LivelloBatteria` - Percentuale batteria (solo elettrici)
- `TariffaPerMinuto` - Costo al minuto (DECIMAL)
- `TariffaFissa` - Costo fisso di attivazione (DECIMAL)
- `ParcheggioId` (FK) - Parcheggio corrente

### 3. Parcheggi
Gestisce le stazioni di parcheggio.

**Campi principali:**
- `Id` (PK) - Identificatore univoco
- `Nome` - Nome del parcheggio
- `Indirizzo` - Indirizzo fisico
- `Capienza` - Numero totale di posti
- `PostiLiberi` - Posti attualmente liberi
- `PostiOccupati` - Posti attualmente occupati

### 4. Corse
Registra le corse effettuate dagli utenti.

**Campi principali:**
- `Id` (PK) - Identificatore univoco
- `UtenteId` (FK) - Utente che ha effettuato la corsa
- `MezzoId` (FK) - Mezzo utilizzato
- `Inizio` - Timestamp inizio corsa
- `Fine` - Timestamp fine corsa (nullable)
- `DurataMinuti` - Durata in minuti
- `CostoTotale` - Costo finale (DECIMAL)
- `Stato` - 0=InCorso, 1=Completata, 2=CompletataConDebito, 3=Annullata
- `ParcheggioPartenzaId` (FK) - Parcheggio di partenza
- `ParcheggioDestinazioneId` (FK) - Parcheggio di arrivo
- `PuntiEcoAssegnati` - Punti eco assegnati

### 5. SegnalazioniManutenzione
Gestisce le segnalazioni di problemi sui mezzi.

**Campi principali:**
- `Id` (PK) - Identificatore univoco
- `MezzoId` (FK) - Mezzo segnalato
- `UtenteId` (FK) - Utente che ha segnalato
- `CorsaId` (FK) - Corsa durante la quale è avvenuta la segnalazione
- `Descrizione` - Descrizione del problema
- `Stato` - 0=Aperta, 1=InLavorazione, 2=Risolta, 3=Respinta
- `Priorita` - 0=Bassa, 1=Media, 2=Alta, 3=Critica
- `DataSegnalazione` - Data/ora segnalazione
- `TecnicoId` (FK) - Tecnico assegnato (nullable)

### 6. Ricariche
Gestisce le ricariche di credito degli utenti.

**Campi principali:**
- `Id` (PK) - Identificatore univoco
- `UtenteId` (FK) - Utente che ha ricaricato
- `Importo` - Importo ricaricato (DECIMAL)
- `MetodoPagamento` - 0=CartaCredito, 1=PayPal, 2=Bonifico
- `DataRicarica` - Data/ora ricarica
- `Stato` - 0=Pendente, 1=Completata, 2=Fallita, 3=Rimborsata

### 7. Pagamenti
Gestisce i pagamenti per le corse.

**Campi principali:**
- `Id` (PK) - Identificatore univoco
- `UtenteId` (FK) - Utente che ha pagato
- `CorsaId` (FK) - Corsa pagata (nullable)
- `Importo` - Importo pagato (DECIMAL)
- `Metodo` - Metodo di pagamento
- `Stato` - Stato del pagamento

### 8. Slots
Gestisce i singoli slot nei parcheggi.

**Campi principali:**
- `Id` (PK) - Identificatore univoco
- `ParcheggioId` (FK) - Parcheggio di appartenenza
- `Numero` - Numero slot
- `Stato` - 0=Libero, 1=Occupato, 2=FuoriServizio
- `MezzoId` (FK) - Mezzo attualmente nello slot (nullable)

### 9. AttuatoriSblocco
Gestisce i meccanismi di sblocco/blocco dei mezzi.

**Campi principali:**
- `Id` (PK) - Identificatore univoco
- `SerialNumber` - Numero seriale attuatore (UNIQUE)
- `Stato` - 0=Attivo, 1=Inattivo, 2=Errore
- `UltimaAttivazione` - Timestamp ultima attivazione
- `MezzoId` (FK) - Mezzo associato all'attuatore
- `CreatedAt` - Data creazione
- `UpdatedAt` - Data ultimo aggiornamento

**Associazioni attuatori-mezzi:**
- ACT-BIKE-001 → City Bike Classic (ID: 1)
- ACT-EBIKE-002 → E-Bike Urban Pro (ID: 2) 
- ACT-EBIKE-003 → E-Bike Mountain (ID: 3)
- ACT-SCOOT-004 → Urban Scooter X1 (ID: 4)
- ACT-SCOOT-005 → Eco Scooter Lite (ID: 5)
- ACT-BIKE-006 → City Bike Sport (ID: 6)

### 10. SensoriBatteria
Gestisce i dati dei sensori di batteria per mezzi elettrici.

**Campi principali:**
- `Id` (PK) - Identificatore univoco
- `MezzoId` (FK) - Mezzo monitorato
- `LivelloBatteria` - Percentuale batteria (0-100)
- `Timestamp` - Data/ora rilevazione
- `IsAttivo` - Sensore attivo/inattivo
- `CreatedAt` - Data creazione
- `UpdatedAt` - Data ultimo aggiornamento

## Relazioni Principali

### Foreign Keys
- `Mezzi.ParcheggioId` → `Parcheggi.Id`
- `Corse.UtenteId` → `Utenti.Id`
- `Corse.MezzoId` → `Mezzi.Id`
- `Corse.ParcheggioPartenzaId` → `Parcheggi.Id`
- `Corse.ParcheggioDestinazioneId` → `Parcheggi.Id`
- `Slots.ParcheggioId` → `Parcheggi.Id`
- `Slots.MezzoId` → `Mezzi.Id`
- `AttuatoriSblocco.MezzoId` → `Mezzi.Id` 
- `SensoriBatteria.MezzoId` → `Mezzi.Id`
- `Pagamenti.UtenteId` → `Utenti.Id`
- `Pagamenti.CorsaId` → `Corse.Id`
- `Ricariche.UtenteId` → `Utenti.Id`
- `SegnalazioniManutenzione.MezzoId` → `Mezzi.Id`
- `SegnalazioniManutenzione.UtenteId` → `Utenti.Id`



