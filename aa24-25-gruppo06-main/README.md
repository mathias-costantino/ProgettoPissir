# AA24-25-Gruppo06 - SharingMezzi

Componenti del gruppo:

Ayoub Mahfoud 20044074

Souhail Nmili 20044037

Mathias Costantino 20043922

## Descrizione del Progetto

**SharingMezzi** è un sistema completo di sharing di mezzi elettrici e bici, sviluppato con architettura Clean Architecture. Il sistema permette la gestione di biciclette e scooter elettrici distribuiti in parcheggi della città, con funzionalità di prenotazione, utilizzo, pagamento e monitoraggio IoT.

### Caratteristiche Principali

-  **Gestione Mezzi**: Biciclette muscolari ed elettriche, scooter
- ️ **Parcheggi Intelligenti**: Sistema di slot con monitoraggio occupazione
-  **Sistema di Pagamento**: Crediti, ricariche, tariffe dinamiche
-  **Manutenzione**: Segnalazioni automatiche e manuali
- **IoT Integration**: Sensori batteria, attuatori sblocco, MQTT
- **Multi-utente**: Clienti e amministratori
- **Admin Dashboard**: Interfaccia web per gestione sistema(demo)
- **Interfaccia console**: Interfaccia da terminale nella folder SharingMezzi.Client.Console per testare tutto

## Struttura del Repository

```
SharingMezziFinal/
├── SharingMezzi.Core/              # Domain Layer - Entità e logica business
│   ├── Entities/                   # Modelli del dominio
│   ├── Services/                   # Servizi applicativi
│   ├── DTOs/                       # Data Transfer Objects
│   └── Interfaces/                 # Interfacce
├── SharingMezzi.Infrastructure/    # Infrastructure Layer - Database e servizi esterni
│   ├── Database/                   # Repository e DbContext
│   ├── Migrations/                 # Migrazioni Entity Framework
│   ├── Mqtt/                       # Broker MQTT e servizi IoT
│   └── Services/                   # Servizi di autenticazione
├── SharingMezzi.Api/              # API REST Layer
│   ├── Controllers/                # Controller API REST
│   ├── wwwroot/                    # Admin Dashboard Web
│   └── sharingmezzi.db            # Database SQLite
├── SharingMezzi.IoT/              # Simulazione Dispositivi IoT
│   ├── Clients/                    # Client IoT simulati
│   └── Services/                   # Servizi IoT (Philips Hue, etc.)
├── SharingMezzi.Client.Console/   # Client Console per Test
└── documentazione/                # Documentazione Tecnica
    ├── database_structure.md      # Struttura database
    ├── api_documentation.md       # Documentazione API
    └── architecture_overview.md   # Panoramica architettura
```

## Come Avviare il Sistema

### Prerequisiti
- .NET 9.0 SDK
- Visual Studio 
- SQLite3

### 1. Avvio API REST e Admin Dashboard
```bash
cd SharingMezzi.Api
dotnet run
```
- API REST: `https://localhost:5000`
- Admin Dashboard: `https://localhost:5000/admin`


### 2. Client Console per Test
```bash
cd SharingMezzi.Client.Console
dotnet run
```

### 3. Build Completo
```bash
dotnet build SharingMezzi.sln
```

## Utenti di Test

Il sistema viene inizializzato con i seguenti utenti:

| Email | Password | Ruolo | Credito |
|-------|----------|-------|---------|
| admin@sharingmezzi.it | admin123 | Admin | €100 |
| marco.rossi@email.it | password123 | Utente | €50 |
| lucia.verdi@email.it | password123 | Utente | €30 |

## Tecnologie Utilizzate

- **Backend**: .NET 9.0, ASP.NET Core Web API
- **Database**: SQLite con Entity Framework Core
- **IoT**: MQTT Protocol MQTTnet
- **Frontend**: HTML5, CSS3, JavaScript
- **Architettura**: Clean Architecture, CQRS Pattern
- **Autenticazione**: JWT Bearer Tokens

## Features Implementate

 Sistema di autenticazione JWT  
 CRUD completo per tutte le entità  
 Gestione corse (prenotazione → utilizzo → conclusione)  
 Sistema di pagamenti e ricariche  
 Monitoraggio IoT (sensori batteria, attuatori)  
 Admin dashboard web responsive  
 Broker MQTT per comunicazione IoT  
 Segnalazioni manutenzione automatiche  
 Gestione parcheggi e slot  
 Sistema di punti ecologici  
 Client console per testing  
