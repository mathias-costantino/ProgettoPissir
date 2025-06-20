using System.Text.Json;
using SharingMezzi.Core.DTOs;
using System.Net.Http.Headers;

namespace SharingMezzi.Client.Console
{
    class Program
    {
        private static readonly HttpClient httpClient = new();
        private static readonly string API_BASE = "http://localhost:5000/api";
        private static string? authToken = null;
        private static UtenteDto? currentUser = null;

        static async Task Main(string[] args)
        {
            System.Console.Clear();
            System.Console.WriteLine("=== SharingMezzi - Sistema di Gestione Mezzi ===");
            System.Console.WriteLine();

            httpClient.DefaultRequestHeaders.Add("User-Agent", "SharingMezzi-Client/1.0");

            while (true)
            {
                if (authToken == null)
                {
                    await ShowLoginMenu();
                }
                else
                {
                    await ShowMainMenu();
                }
            }
        }

        // === MENU MANAGEMENT ===
        static async Task ShowLoginMenu()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== ACCESSO SISTEMA === ");
            System.Console.WriteLine();
            System.Console.WriteLine("1. Accedi");
            System.Console.WriteLine("2. Registrati");
            System.Console.WriteLine("3. Info Sistema");
            System.Console.WriteLine("0. Esci");
            System.Console.WriteLine();
            System.Console.Write("Scegli un'opzione: ");

            var choice = System.Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await Login();
                        break;
                    case "2":
                        await Register();
                        break;
                    case "3":
                        await StatusSistema();
                        break;
                    case "0":
                        System.Console.WriteLine("Arrivederci!");
                        Environment.Exit(0);
                        break;
                    default:
                        System.Console.WriteLine("Opzione non valida");
                        await WaitKey();
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
                await WaitKey();
            }
        }

        static async Task ShowMainMenu()
        {
            System.Console.Clear();
            System.Console.WriteLine($"=== BENVENUTO {currentUser?.Nome?.ToUpper()} === Credito: €{currentUser?.Credito:F2}");
            System.Console.WriteLine();

            // Verifica se l'utente è un amministratore
            if (currentUser?.Ruolo == "Amministratore")
            {
                await ShowAdminMenu();
            }
            else
            {
                await ShowUserMenu();
            }
        }

        static async Task ShowUserMenu()
        {
            System.Console.WriteLine("=== MENU UTENTE ===");
            System.Console.WriteLine();
            System.Console.WriteLine("1. Visualizza mezzi disponibili");
            System.Console.WriteLine("2. Visualizza parcheggi");
            System.Console.WriteLine("3. Inizia corsa");
            System.Console.WriteLine("4. Termina corsa");
            System.Console.WriteLine("5. Storico corse");
            System.Console.WriteLine("6. Ricarica credito");
            System.Console.WriteLine("7. Converti punti eco in credito");
            System.Console.WriteLine("8. Test comandi MQTT");
            System.Console.WriteLine("9. Status sistema");
            System.Console.WriteLine("10. Profilo utente");
            System.Console.WriteLine("0. Logout");
            System.Console.WriteLine();
            System.Console.Write("Scegli un'opzione: ");

            var choice = System.Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await ListMezziDisponibili();
                        break;
                    case "2":
                        await ListParcheggi();
                        break;
                    case "3":
                        await IniziaCorsa();
                        break;
                    case "4":
                        await TerminaCorsa();
                        break;
                    case "5":
                        await StoricoCorse();
                        break;
                    case "6":
                        await RicaricaCredito();
                        break;
                    case "7":
                        await ConvertiPuntiEco();
                        break;
                    case "8":
                        await TestMqttCommands();
                        break;
                    case "9":
                        await StatusSistema();
                        break;
                    case "10":
                        await ProfiloUtente();
                        break;
                    case "0":
                        await Logout();
                        break;
                    default:
                        System.Console.WriteLine("Opzione non valida");
                        await WaitKey();
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
                await WaitKey();
            }
        }

        static async Task ShowAdminMenu()
        {
            System.Console.WriteLine("=== MENU AMMINISTRATORE ===");
            System.Console.WriteLine();
            System.Console.WriteLine("1. Visualizza tutti gli utenti");
            System.Console.WriteLine("2. Gestisci utenti sospesi");
            System.Console.WriteLine("3. Crea nuovo parcheggio");
            System.Console.WriteLine("4. Crea nuovo mezzo");
            System.Console.WriteLine("5. Gestione manutenzione mezzi");
            System.Console.WriteLine("6. Ripara mezzi in manutenzione");
            System.Console.WriteLine("7. Visualizza mezzi disponibili");
            System.Console.WriteLine("8. Visualizza parcheggi");
            System.Console.WriteLine("9. Status sistema");
            System.Console.WriteLine("10. Statistiche admin");
            System.Console.WriteLine("0. Logout");
            System.Console.WriteLine();
            System.Console.Write("Scegli un'opzione: ");

            var choice = System.Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await AdminListAllUsers();
                        break;
                    case "2":
                        await AdminManageSuspendedUsers();
                        break;
                    case "3":
                        await AdminCreateParking();
                        break;
                    case "4":
                        await AdminCreateVehicle();
                        break;
                    case "5":
                        await AdminManageVehicleMaintenance();
                        break;
                    case "6":
                        await AdminRepairVehicles();
                        break;
                    case "7":
                        await ListMezziDisponibili();
                        break;
                    case "8":
                        await ListParcheggi();
                        break;
                    case "9":
                        await StatusSistema();
                        break;
                    case "10":
                        await AdminStatistics();
                        break;
                    case "0":
                        await Logout();
                        break;
                    default:
                        System.Console.WriteLine("Opzione non valida");
                        await WaitKey();
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
                await WaitKey();
            }
        }

        // === AUTHENTICATION ===
        static async Task Login()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== LOGIN === ");
            System.Console.WriteLine();

            System.Console.Write("Email: ");
            var email = System.Console.ReadLine();

            System.Console.Write("Password: ");
            var password = ReadPassword();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                System.Console.WriteLine("Email e password sono obbligatori");
                await WaitKey();
                return;
            }

            var loginRequest = new LoginDto { Email = email, Password = password };
            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync($"{API_BASE}/auth/login", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = JsonSerializer.Deserialize<AuthResultDto>(result, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    if (loginResponse?.Success == true)
                    {
                        authToken = loginResponse.Token;
                        currentUser = loginResponse.User;

                        // Aggiorna header di autorizzazione
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                        // Aggiorna i dati dell'utente dal server per assicurarsi di avere i valori più recenti
                        await AggiornaCreditoUtente();

                        System.Console.WriteLine($"Login effettuato con successo! Benvenuto {currentUser?.Nome}");
                        await WaitKey();
                    }
                    else
                    {
                        System.Console.WriteLine($"Login fallito: {loginResponse?.Message}");
                        await WaitKey();
                    }
                }
                else
                {
                    System.Console.WriteLine($"Login fallito: {result}");
                    await WaitKey();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore durante il login: {ex.Message}");
                await WaitKey();
            }
        }

        static async Task Register()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== REGISTRAZIONE NUOVO UTENTE ===");
            System.Console.WriteLine();

            try
            {
                // Raccolta dati utente
                System.Console.Write("Nome: ");
                var nome = System.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(nome))
                {
                    System.Console.WriteLine("Il nome è obbligatorio");
                    await WaitKey();
                    return;
                }

                System.Console.Write("Cognome: ");
                var cognome = System.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(cognome))
                {
                    System.Console.WriteLine("Il cognome è obbligatorio");
                    await WaitKey();
                    return;
                }

                System.Console.Write("Email: ");
                var email = System.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                {
                    System.Console.WriteLine("Email non valida");
                    await WaitKey();
                    return;
                }

                System.Console.Write("Password (minimo 6 caratteri): ");
                var password = ReadPassword();
                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                {
                    System.Console.WriteLine("La password deve essere di almeno 6 caratteri");
                    await WaitKey();
                    return;
                }

                System.Console.Write("Conferma password: ");
                var confermaPassword = ReadPassword();
                if (password != confermaPassword)
                {
                    System.Console.WriteLine("Le password non corrispondono");
                    await WaitKey();
                    return;
                }

                System.Console.Write("Telefono (opzionale): ");
                var telefono = System.Console.ReadLine();

                // Crea oggetto RegisterDto
                var registerRequest = new RegisterDto
                {
                    Nome = nome,
                    Cognome = cognome,
                    Email = email!,
                    Password = password,
                    ConfirmPassword = confermaPassword,
                    Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono
                };

                var json = JsonSerializer.Serialize(registerRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                System.Console.WriteLine("Registrazione in corso...");

                var response = await httpClient.PostAsync($"{API_BASE}/auth/register", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var registerResponse = JsonSerializer.Deserialize<AuthResultDto>(result, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    if (registerResponse?.Success == true)
                    {
                        System.Console.WriteLine();
                        System.Console.WriteLine("Registrazione completata con successo!");
                        System.Console.WriteLine($"Benvenuto {registerResponse.User?.Nome}!");
                        System.Console.WriteLine();
                        System.Console.WriteLine("Ora puoi effettuare il login con le tue credenziali.");
                        await WaitKey();
                    }
                    else
                    {
                        System.Console.WriteLine($"Registrazione fallita: {registerResponse?.Message}");
                        await WaitKey();
                    }
                }
                else
                {
                    // Prova a parsare eventuali errori di validazione
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<dynamic>(result);
                        System.Console.WriteLine($"Registrazione fallita: {result}");
                    }
                    catch
                    {
                        System.Console.WriteLine($"Registrazione fallita. Codice errore: {response.StatusCode}");
                    }
                    await WaitKey();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore durante la registrazione: {ex.Message}");
                await WaitKey();
            }
        }

        static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        static async Task Logout()
        {
            authToken = null;
            currentUser = null;
            httpClient.DefaultRequestHeaders.Authorization = null;
            System.Console.WriteLine("Logout effettuato con successo!");
            await WaitKey();
        }

        static string ReadPassword()
        {
            var password = "";
            ConsoleKeyInfo key;
            do
            {
                key = System.Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    System.Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password[0..^1];
                    System.Console.Write("\b \b");
                }
            }
            while (key.Key != ConsoleKey.Enter);
            System.Console.WriteLine();
            return password;
        }

        // === MAIN FEATURES ===
        static async Task ListMezziDisponibili()
        {
            System.Console.Clear();
            await MostraMezziDisponibili();
            await WaitKey();
        }

        static async Task MostraMezziDisponibili()
        {
            System.Console.WriteLine("=== MEZZI DISPONIBILI === ");
            System.Console.WriteLine();

            try
            {
                var response = await httpClient.GetAsync($"{API_BASE}/mezzi/disponibili");
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Errore nel recuperare i mezzi: {json}");
                    return;
                }

                var mezzi = JsonSerializer.Deserialize<MezzoDto[]>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (mezzi?.Length > 0)
                {
                    System.Console.WriteLine("┌─────┬─────────────────┬──────────┬───────────┬──────────┐");
                    System.Console.WriteLine("│ ID  │ Modello         │ Tipo     │ Tariffa   │ Batteria │");
                    System.Console.WriteLine("├─────┼─────────────────┼──────────┼───────────┼──────────┤");

                    foreach (var mezzo in mezzi)
                    {
                        var battery = mezzo.IsElettrico ? $"{mezzo.LivelloBatteria}%" : "N/A";
                        var tariffa = $"€{mezzo.TariffaPerMinuto:F2}/min";
                        
                        System.Console.WriteLine($"│ {mezzo.Id,-3} │ {mezzo.Modello,-15} │ {mezzo.Tipo,-8} │ {tariffa,-9} │ {battery,-8} │");
                    }
                    System.Console.WriteLine("└─────┴─────────────────┴──────────┴───────────┴──────────┘");
                }
                else
                {
                    System.Console.WriteLine("Nessun mezzo disponibile al momento");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }
        }

        static async Task ListParcheggi()
        {
            System.Console.Clear();
            await MostraParcheggi();
            await WaitKey();
        }

        static async Task MostraParcheggi()
        {
            System.Console.WriteLine("=== PARCHEGGI === ");
            System.Console.WriteLine();

            try
            {
                var response = await httpClient.GetAsync($"{API_BASE}/parcheggi");
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Errore nel recuperare i parcheggi: {json}");
                    return;
                }

                var parcheggi = JsonSerializer.Deserialize<ParcheggioDto[]>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (parcheggi?.Length > 0)
                {
                    foreach (var parcheggio in parcheggi)
                    {
                        var disponibilita = $"{parcheggio.PostiLiberi}/{parcheggio.Capienza}";
                        var status = parcheggio.PostiLiberi > 0 ? "[DISPONIBILE]" : "[PIENO]";
                        
                        System.Console.WriteLine($"{status} {parcheggio.Id}. {parcheggio.Nome}");
                        System.Console.WriteLine($"   Indirizzo: {parcheggio.Indirizzo}");
                        System.Console.WriteLine($"   Posti liberi: {disponibilita}");
                        System.Console.WriteLine($"   Mezzi presenti: {parcheggio.Mezzi.Count}");
                        System.Console.WriteLine();
                    }
                }
                else
                {
                    System.Console.WriteLine("Nessun parcheggio trovato");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }
        }

        static async Task IniziaCorsa()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== INIZIA CORSA === ");
            System.Console.WriteLine();

            try
            {
                // Mostra mezzi disponibili
                await MostraMezziDisponibili();
                
                System.Console.Write("Inserisci ID del mezzo: ");
                if (!int.TryParse(System.Console.ReadLine(), out var mezzoId))
                {
                    System.Console.WriteLine("ID mezzo non valido");
                    await WaitKey();
                    return;
                }

                var comando = new IniziaCorsa { UtenteId = currentUser!.Id, MezzoId = mezzoId };
                var json = JsonSerializer.Serialize(comando);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{API_BASE}/corse/inizia", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var corsa = JsonSerializer.Deserialize<CorsaDto>(result, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    System.Console.WriteLine("Corsa iniziata con successo!");
                    System.Console.WriteLine($"   ID Corsa: {corsa?.Id}");
                    System.Console.WriteLine($"   Inizio: {corsa?.Inizio:dd/MM/yyyy HH:mm:ss}");
                    System.Console.WriteLine($"   Mezzo: {mezzoId}");
                    System.Console.WriteLine();
                    System.Console.WriteLine("Buon viaggio! Ricorda di terminare la corsa quando arrivi a destinazione.");
                }
                else
                {
                    System.Console.WriteLine($"Errore nell'iniziare la corsa: {result}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }

            await WaitKey();
        }

        static async Task TerminaCorsa()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== TERMINA CORSA === ");
            System.Console.WriteLine();

            try
            {
                // Mostra corse attive dell'utente
                await MostraCorsaAttiva();

                System.Console.Write("Inserisci ID della corsa da terminare: ");
                if (!int.TryParse(System.Console.ReadLine(), out var corsaId))
                {
                    System.Console.WriteLine("ID corsa non valido");
                    await WaitKey();
                    return;
                }

                // Mostra parcheggi disponibili
                await MostraParcheggi();

                System.Console.Write("Inserisci ID del parcheggio di destinazione: ");
                if (!int.TryParse(System.Console.ReadLine(), out var parcheggioId))
                {
                    System.Console.WriteLine("ID parcheggio non valido");
                    await WaitKey();
                    return;
                }

                // Chiedi se vuole segnalare problemi di manutenzione
                System.Console.WriteLine();
                System.Console.WriteLine("=== SEGNALAZIONE MANUTENZIONE === ");
                System.Console.Write("Vuoi segnalare problemi di manutenzione per questo mezzo? (s/n): ");
                var segnalaInput = System.Console.ReadLine()?.ToLower();
                var segnalaManutenzione = segnalaInput == "s" || segnalaInput == "si" || segnalaInput == "y" || segnalaInput == "yes";

                string? descrizioneManutenzione = null;
                if (segnalaManutenzione)
                {
                    System.Console.WriteLine("Descrivi il problema riscontrato:");
                    descrizioneManutenzione = System.Console.ReadLine();
                    
                    if (string.IsNullOrEmpty(descrizioneManutenzione))
                    {
                        System.Console.WriteLine("La descrizione è obbligatoria per la segnalazione");
                        await WaitKey();
                        return;
                    }
                }

                var terminaCorsa = new TerminaCorsa 
                { 
                    ParcheggioDestinazioneId = parcheggioId,
                    SegnalaManutenzione = segnalaManutenzione,
                    DescrizioneManutenzione = descrizioneManutenzione
                };

                var json = JsonSerializer.Serialize(terminaCorsa);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync($"{API_BASE}/corse/{corsaId}/termina", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var corsa = JsonSerializer.Deserialize<CorsaDto>(result, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    System.Console.WriteLine("Corsa terminata con successo!");
                    System.Console.WriteLine($"   Durata: {corsa?.DurataMinuti} minuti");
                    System.Console.WriteLine($"   Costo totale: €{corsa?.CostoTotale:F2}");
                    
                    // NUOVO: Mostra punti eco guadagnati
                    if (corsa?.PuntiEcoAssegnati > 0)
                    {
                        System.Console.WriteLine($"   Punti Eco guadagnati: {corsa.PuntiEcoAssegnati}");
                        System.Console.WriteLine("   Hai usato una bici muscolare - ottima scelta ecologica!");
                        
                        if (corsa.PuntiEcoAssegnati >= 100)
                        {
                            decimal creditoConvertibile = corsa.PuntiEcoAssegnati.Value / 100m;
                            System.Console.WriteLine($"   Puoi convertire {corsa.PuntiEcoAssegnati} punti in €{creditoConvertibile:F2} di credito!");
                        }
                    }
                    else if (corsa?.MezzoTipo == "BiciMuscolare")
                    {
                        System.Console.WriteLine("   Punti Eco: Nessun punto assegnato (corsa troppo breve)");
                    }
                    else
                    {
                        System.Console.WriteLine("   Mezzo elettrico utilizzato - nessun punto eco assegnato");
                        System.Console.WriteLine("   Usa bici muscolari per guadagnare punti eco!");
                    }
                    
                    if (segnalaManutenzione)
                    {
                        System.Console.WriteLine("   Segnalazione manutenzione inviata!");
                        System.Console.WriteLine("   Il mezzo è stato messo in manutenzione");
                    }

                    // Aggiorna il credito dell'utente
                    await AggiornaCreditoUtente();
                    
                    System.Console.WriteLine();
                    System.Console.WriteLine($"   Nuovo credito: €{currentUser?.Credito:F2}");
                    System.Console.WriteLine($"   Punti Eco totali: {currentUser?.PuntiEco}");
                }
                else
                {
                    System.Console.WriteLine($"Errore nel terminare la corsa: {result}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }

            await WaitKey();
        }

        static async Task MostraCorsaAttiva()
        {
            try
            {
                var response = await httpClient.GetAsync($"{API_BASE}/corse/utente/{currentUser!.Id}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var corse = JsonSerializer.Deserialize<CorsaDto[]>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    var corsaAttiva = corse?.FirstOrDefault(c => c.Stato == "InCorso");
                    if (corsaAttiva != null)
                    {
                        System.Console.WriteLine("Corsa attiva:");
                        System.Console.WriteLine($"   ID: {corsaAttiva.Id}");
                        System.Console.WriteLine($"   Mezzo: {corsaAttiva.MezzoId}");
                        System.Console.WriteLine($"   Inizio: {corsaAttiva.Inizio:dd/MM/yyyy HH:mm:ss}");
                        System.Console.WriteLine();
                    }
                    else
                    {
                        System.Console.WriteLine("Non hai corse attive");
                        System.Console.WriteLine();
                    }
                }
            }
            catch
            {
                // Ignora errori nella visualizzazione delle corse attive
            }
        }

        static async Task StoricoCorse()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== STORICO CORSE === ");
            System.Console.WriteLine();

            try
            {
                var response = await httpClient.GetAsync($"{API_BASE}/corse/utente/{currentUser!.Id}");
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Errore nel recuperare lo storico: {json}");
                    await WaitKey();
                    return;
                }

                var corse = JsonSerializer.Deserialize<CorsaDto[]>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (corse?.Length > 0)
                {
                    System.Console.WriteLine("┌──────┬──────────────────────┬────────┬──────────────┬──────────┐");
                    System.Console.WriteLine("│ ID   │ Data e Ora           │ Durata │ Costo        │ Stato    │");
                    System.Console.WriteLine("├──────┼──────────────────────┼────────┼──────────────┼──────────┤");

                    foreach (var corsa in corse.OrderByDescending(c => c.Inizio))
                    {
                        var durata = corsa.Fine.HasValue ? $"{corsa.DurataMinuti} min" : "In corso";
                        var costo = corsa.CostoTotale > 0 ? $"€{corsa.CostoTotale:F2}" : "N/A";
                        var dataOra = corsa.Inizio.ToString("dd/MM/yy HH:mm");

                        System.Console.WriteLine($"│ {corsa.Id,-4} │ {dataOra,-20} │ {durata,-6} │ {costo,-12} │ {corsa.Stato,-8} │");
                    }
                    System.Console.WriteLine("└──────┴──────────────────────┴────────┴──────────────┴──────────┘");
                }
                else
                {
                    System.Console.WriteLine("Non hai ancora effettuato corse");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }

            await WaitKey();
        }

        static async Task RicaricaCredito()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== RICARICA CREDITO === ");
            System.Console.WriteLine();
            System.Console.WriteLine($"Credito attuale: €{currentUser?.Credito:F2}");
            System.Console.WriteLine();

            System.Console.Write("Inserisci l'importo da ricaricare (€5-€500, es: 10.50 o 10,50): ");
            var importoInput = System.Console.ReadLine()?.Replace('.', ','); // Normalizza il separatore decimale
            if (!decimal.TryParse(importoInput, out var importo) || importo < 5 || importo > 500)
            {
                System.Console.WriteLine("Importo non valido. Deve essere tra €5 e €500. Usa formato: 10,50 o 10.50");
                await WaitKey();
                return;
            }

            try
            {
                var ricaricaRequest = new RicaricaCreditoDto 
                { 
                    UtenteId = currentUser!.Id,
                    Importo = importo,
                    MetodoPagamento = "CartaCredito"
                };
                var json = JsonSerializer.Serialize(ricaricaRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{API_BASE}/user/ricarica-credito", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var ricaricaResponse = JsonSerializer.Deserialize<RicaricaResponseDto>(result, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    System.Console.WriteLine($"Ricarica effettuata con successo!");
                    System.Console.WriteLine($"   Importo ricaricato: €{importo:F2}");
                    System.Console.WriteLine($"   Transaction ID: {ricaricaResponse?.TransactionId}");
                    
                    // Aggiorna il credito dell'utente
                    await AggiornaCreditoUtente();
                    
                    System.Console.WriteLine($"   Nuovo credito: €{currentUser?.Credito:F2}");
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<JsonElement>(result);
                    var errorMessage = errorResponse.TryGetProperty("message", out var messageProp) 
                        ? messageProp.GetString() 
                        : "Errore sconosciuto";
                    System.Console.WriteLine($"Errore nella ricarica: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }

            await WaitKey();
        }

        static async Task ProfiloUtente()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== PROFILO UTENTE === ");
            System.Console.WriteLine();

            try
            {
                // Aggiorna informazioni profilo dal server
                await AggiornaCreditoUtente();

                if (currentUser != null)
                {
                    System.Console.WriteLine($"ID: {currentUser.Id}");
                    System.Console.WriteLine($"Nome: {currentUser.Nome} {currentUser.Cognome}");
                    System.Console.WriteLine($"Email: {currentUser.Email}");
                    if (!string.IsNullOrEmpty(currentUser.Telefono))
                    {
                        System.Console.WriteLine($"Telefono: {currentUser.Telefono}");
                    }
                    System.Console.WriteLine($"Credito: €{currentUser.Credito:F2}");
                    System.Console.WriteLine($"Punti Eco: {currentUser.PuntiEco}");
                    System.Console.WriteLine($"Stato: {currentUser.Stato}");
                    System.Console.WriteLine($"Ruolo: {currentUser.Ruolo}");
                    System.Console.WriteLine($"Data registrazione: {currentUser.DataRegistrazione:dd/MM/yyyy}");
                    
                    if (currentUser.DataSospensione != null)
                    {
                        System.Console.WriteLine($"Sospeso dal: {currentUser.DataSospensione:dd/MM/yyyy}");
                        System.Console.WriteLine($"Motivo: {currentUser.MotivoSospensione}");
                    }

                    System.Console.WriteLine();
                    System.Console.WriteLine("Info Punti Eco:");
                    System.Console.WriteLine("   • 1 punto per ogni minuto di utilizzo bici muscolare");
                    System.Console.WriteLine("   • +10 punti bonus ogni 30 minuti di utilizzo");
                    System.Console.WriteLine("   • 100 punti = €1 di credito");
                    
                    if (currentUser.PuntiEco >= 100)
                    {
                        decimal creditoPossibile = currentUser.PuntiEco / 100m;
                        System.Console.WriteLine($"   Puoi convertire: {(int)(creditoPossibile * 100)} punti = €{creditoPossibile:F2}");
                    }
                }
                else
                {
                    System.Console.WriteLine("Informazioni utente non disponibili");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore nel caricamento profilo: {ex.Message}");
            }

            await WaitKey();
        }

        static async Task ConvertiPuntiEco()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== CONVERSIONE PUNTI ECO === ");
            System.Console.WriteLine();

            try
            {
                // Aggiorna informazioni utente
                await AggiornaCreditoUtente();

                if (currentUser?.PuntiEco > 0)
                {
                    System.Console.WriteLine($"Punti Eco disponibili: {currentUser.PuntiEco}");
                    System.Console.WriteLine($"Credito attuale: €{currentUser.Credito:F2}");
                    System.Console.WriteLine();
                    System.Console.WriteLine("Tasso di conversione: 100 punti = €1.00");
                    
                    if (currentUser.PuntiEco < 100)
                    {
                        System.Console.WriteLine("Hai bisogno di almeno 100 punti per la conversione");
                        System.Console.WriteLine($"   Ti servono ancora {100 - currentUser.PuntiEco} punti");
                        System.Console.WriteLine();
                        System.Console.WriteLine("Come guadagnare punti eco:");
                        System.Console.WriteLine("   • Usa bici muscolari: 1 punto per minuto");
                        System.Console.WriteLine("   • Bonus: +10 punti ogni 30 minuti");
                        await WaitKey();
                        return;
                    }

                    decimal creditoMassimo = currentUser.PuntiEco / 100m;
                    int puntiMassimi = (int)(creditoMassimo * 100);
                    
                    System.Console.WriteLine($"Puoi convertire: {puntiMassimi} punti = €{creditoMassimo:F2}");
                    System.Console.WriteLine();
                    
                    System.Console.Write("Inserisci il numero di punti da convertire (multipli di 100): ");
                    if (!int.TryParse(System.Console.ReadLine(), out var puntiDaConvertire) || 
                        puntiDaConvertire < 100 || 
                        puntiDaConvertire > currentUser.PuntiEco ||
                        puntiDaConvertire % 100 != 0)
                    {
                        System.Console.WriteLine("Numero di punti non valido!");
                        System.Console.WriteLine("   Deve essere almeno 100 e multiplo di 100");
                        await WaitKey();
                        return;
                    }

                    decimal creditoDaAggiungere = puntiDaConvertire / 100m;
                    
                    System.Console.WriteLine();
                    System.Console.WriteLine($"Riepilogo conversione:");
                    System.Console.WriteLine($"   Punti da convertire: {puntiDaConvertire}");
                    System.Console.WriteLine($"   Credito da aggiungere: €{creditoDaAggiungere:F2}");
                    System.Console.WriteLine($"   Punti rimanenti: {currentUser.PuntiEco - puntiDaConvertire}");
                    System.Console.WriteLine($"   Nuovo credito totale: €{currentUser.Credito + creditoDaAggiungere:F2}");
                    System.Console.WriteLine();
                    
                    System.Console.Write("Confermi la conversione? (s/n): ");
                    var conferma = System.Console.ReadLine()?.ToLower();
                    
                    if (conferma != "s" && conferma != "si" && conferma != "y" && conferma != "yes")
                    {
                        System.Console.WriteLine("Conversione annullata");
                        await WaitKey();
                        return;
                    }

                    // Chiama l'API per la conversione
                    var conversioneRequest = new ConvertiPuntiDto { PuntiDaConvertire = puntiDaConvertire };
                    var json = JsonSerializer.Serialize(conversioneRequest);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync($"{API_BASE}/user/converti-punti", content);
                    var result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        System.Console.WriteLine("Conversione completata con successo!");
                        
                        // Aggiorna i dati dell'utente
                        await AggiornaCreditoUtente();
                        
                        System.Console.WriteLine($"   Punti convertiti: {puntiDaConvertire}");
                        System.Console.WriteLine($"   Credito aggiunto: €{creditoDaAggiungere:F2}");
                        System.Console.WriteLine($"   Nuovo credito: €{currentUser?.Credito:F2}");
                        System.Console.WriteLine($"   Punti rimanenti: {currentUser?.PuntiEco}");
                    }
                    else
                    {
                        var errorResponse = JsonSerializer.Deserialize<JsonElement>(result);
                        var errorMessage = errorResponse.TryGetProperty("message", out var messageProp) 
                            ? messageProp.GetString() 
                            : "Errore sconosciuto";
                        System.Console.WriteLine($"Errore nella conversione: {errorMessage}");
                    }
                }
                else
                {
                    System.Console.WriteLine("Non hai punti eco da convertire");
                    System.Console.WriteLine();
                    System.Console.WriteLine("Come guadagnare punti eco:");
                    System.Console.WriteLine("   • Usa bici muscolari: 1 punto per minuto");
                    System.Console.WriteLine("   • Bonus: +10 punti ogni 30 minuti");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }

            await WaitKey();
        }

        static async Task TestMqttCommands()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== TEST COMANDI MQTT === ");
            System.Console.WriteLine();
            System.Console.WriteLine("1. Sblocca mezzo");
            System.Console.WriteLine("2. Blocca mezzo");
            System.Console.WriteLine("3. Controllo LED slot");
            System.Console.WriteLine("4. Status MQTT");
            System.Console.WriteLine("0. Torna al menu");
            System.Console.WriteLine();
            System.Console.Write("Scegli un'opzione: ");

            var choice = System.Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        System.Console.Write("ID Mezzo: ");
                        if (int.TryParse(System.Console.ReadLine(), out var mezzoId))
                        {
                            var response = await httpClient.PostAsync($"{API_BASE}/mqtt/unlock/{mezzoId}", null);
                            var result = await response.Content.ReadAsStringAsync();
                            System.Console.WriteLine(response.IsSuccessStatusCode ? "Comando inviato" : $"Errore: {result}");
                        }
                        break;

                    case "2":
                        System.Console.Write("ID Mezzo: ");
                        if (int.TryParse(System.Console.ReadLine(), out var mezzoId2))
                        {
                            var response = await httpClient.PostAsync($"{API_BASE}/mqtt/lock/{mezzoId2}", null);
                            var result = await response.Content.ReadAsStringAsync();
                            System.Console.WriteLine(response.IsSuccessStatusCode ? "Comando inviato" : $"Errore: {result}");
                        }
                        break;

                    case "3":
                        System.Console.Write("ID Slot: ");
                        if (int.TryParse(System.Console.ReadLine(), out var slotId))
                        {
                            System.Console.Write("Colore (green/red/yellow/blue): ");
                            var color = System.Console.ReadLine() ?? "green";

                            var ledCommand = new { Color = color, Pattern = "solid" };
                            var json = JsonSerializer.Serialize(ledCommand);
                            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                            var response = await httpClient.PostAsync($"{API_BASE}/mqtt/led/{slotId}", content);
                            var result = await response.Content.ReadAsStringAsync();
                            System.Console.WriteLine(response.IsSuccessStatusCode ? "Comando inviato" : $"Errore: {result}");
                        }
                        break;

                    case "4":
                        var statusResponse = await httpClient.GetAsync($"{API_BASE}/mqtt/status");
                        var statusResult = await statusResponse.Content.ReadAsStringAsync();
                        System.Console.WriteLine($"Status MQTT: {statusResult}");
                        break;

                    case "0":
                        return;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }

            await WaitKey();
        }

        static async Task StatusSistema()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== STATUS SISTEMA === ");
            System.Console.WriteLine();

            try
            {
                // Test API
                var apiResponse = await httpClient.GetAsync($"{API_BASE}/mezzi");
                System.Console.WriteLine($"API Status: {(apiResponse.IsSuccessStatusCode ? "Connesso" : "Disconnesso")}");

                // Test MQTT
                try
                {
                    var mqttResponse = await httpClient.GetAsync($"{API_BASE}/mqtt/status");
                    if (mqttResponse.IsSuccessStatusCode)
                    {
                        var mqttStatus = await mqttResponse.Content.ReadAsStringAsync();
                        System.Console.WriteLine($"MQTT Status: {mqttStatus}");
                    }
                    else
                    {
                        System.Console.WriteLine("MQTT Status: Non disponibile");
                    }
                }
                catch
                {
                    System.Console.WriteLine("MQTT Status: Errore di connessione");
                }

                // Statistiche generali
                System.Console.WriteLine();
                System.Console.WriteLine("=== STATISTICHE === ");

                var mezziResponse = await httpClient.GetAsync($"{API_BASE}/mezzi");
                if (mezziResponse.IsSuccessStatusCode)
                {
                    var mezziJson = await mezziResponse.Content.ReadAsStringAsync();
                    var mezzi = JsonSerializer.Deserialize<MezzoDto[]>(mezziJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    System.Console.WriteLine($"Mezzi totali: {mezzi?.Length ?? 0}");
                }

                var parcheggiwResponse = await httpClient.GetAsync($"{API_BASE}/parcheggi");
                if (parcheggiwResponse.IsSuccessStatusCode)
                {
                    var parcheggwJson = await parcheggiwResponse.Content.ReadAsStringAsync();
                    var parcheggi = JsonSerializer.Deserialize<ParcheggioDto[]>(parcheggwJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    System.Console.WriteLine($"Parcheggi totali: {parcheggi?.Length ?? 0}");
                }

                System.Console.WriteLine($"Timestamp: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore nel controllo status: {ex.Message}");
            }

            await WaitKey();
        }

        // === ADMIN METHODS ===
        static async Task AdminListAllUsers()
        {
            await AdminListAllUsersWithSuspend();
        }

        static async Task AdminListAllUsersWithSuspend()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== TUTTI GLI UTENTI ===");
            System.Console.WriteLine();

            try
            {
                var response = await httpClient.GetAsync($"{API_BASE}/admin/users");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var utenti = JsonSerializer.Deserialize<UtenteDto[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (utenti != null && utenti.Length > 0)
                    {
                        foreach (var utente in utenti)
                        {
                            System.Console.WriteLine($"ID: {utente.Id} | {utente.Nome} {utente.Cognome}");
                            System.Console.WriteLine($"Email: {utente.Email} | Ruolo: {utente.Ruolo}");
                            System.Console.WriteLine($"Stato: {utente.Stato} | Credito: €{utente.Credito:F2}");
                            if (utente.DataSospensione != null)
                            {
                                System.Console.WriteLine($"Sospeso dal: {utente.DataSospensione:dd/MM/yyyy} - Motivo: {utente.MotivoSospensione}");
                            }
                            System.Console.WriteLine("───────────────────────────");
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("Nessun utente trovato");
                    }
                }
                else
                {
                    System.Console.WriteLine($"Errore nel recupero utenti: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }

            await WaitKey();
        }

        static async Task AdminManageSuspendedUsers()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== GESTIONE UTENTI SOSPESI ===");
            System.Console.WriteLine();

            try
            {
                var response = await httpClient.GetAsync($"{API_BASE}/admin/users/suspended");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var utentiSospesi = JsonSerializer.Deserialize<UtenteDto[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (utentiSospesi != null && utentiSospesi.Length > 0)
                    {
                        System.Console.WriteLine("Utenti attualmente sospesi:");
                        System.Console.WriteLine();

                        foreach (var utente in utentiSospesi)
                        {
                            System.Console.WriteLine($"ID: {utente.Id} | {utente.Nome} {utente.Cognome}");
                            System.Console.WriteLine($"Email: {utente.Email}");
                            System.Console.WriteLine($"Sospeso dal: {utente.DataSospensione:dd/MM/yyyy}");
                            System.Console.WriteLine($"Motivo: {utente.MotivoSospensione}");
                            System.Console.WriteLine("───────────────────────────");
                        }

                        System.Console.WriteLine();
                        System.Console.Write("Inserisci ID utente da sbloccare (0 per tornare al menu): ");
                        if (int.TryParse(System.Console.ReadLine(), out int userId) && userId > 0)
                        {
                            await AdminUnblockUser(userId);
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("Nessun utente sospeso al momento");
                    }
                }
                else
                {
                    System.Console.WriteLine($"Errore nel recupero utenti sospesi: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }

            await WaitKey();
        }

        static async Task AdminUnblockUser(int userId)
        {
            System.Console.Write("Inserisci note per lo sblocco: ");
            var note = System.Console.ReadLine() ?? "";

            var request = new { UtenteId = userId, Note = note };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync($"{API_BASE}/admin/users/{userId}/unblock", content);
                if (response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine("Utente sbloccato con successo!");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Console.WriteLine($"Errore nello sblocco: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }
        }

        static async Task AdminCreateParking()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== CREA NUOVO PARCHEGGIO ===");
            System.Console.WriteLine();

            try
            {
                System.Console.Write("Nome parcheggio: ");
                var nome = System.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(nome))
                {
                    System.Console.WriteLine("Nome obbligatorio!");
                    await WaitKey();
                    return;
                }

                System.Console.Write("Indirizzo: ");
                var indirizzo = System.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(indirizzo))
                {
                    System.Console.WriteLine("Indirizzo obbligatorio!");
                    await WaitKey();
                    return;
                }

                System.Console.Write("Capienza (numero di posti): ");
                if (!int.TryParse(System.Console.ReadLine(), out int capienza) || capienza <= 0)
                {
                    System.Console.WriteLine("Capienza non valida!");
                    await WaitKey();
                    return;
                }

                var request = new { Nome = nome, Indirizzo = indirizzo, Capienza = capienza };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{API_BASE}/admin/parking", content);
                if (response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine("Parcheggio creato con successo!");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Console.WriteLine($"Errore nella creazione: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }

            await WaitKey();
        }

        static async Task AdminCreateVehicle()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== CREA NUOVO MEZZO ===");
            System.Console.WriteLine();

            try
            {
                System.Console.Write("Modello del mezzo: ");
                var modello = System.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(modello))
                {
                    System.Console.WriteLine("Modello obbligatorio!");
                    await WaitKey();
                    return;
                }

                System.Console.WriteLine("Tipo di mezzo:");
                System.Console.WriteLine("1. BiciMuscolare");
                System.Console.WriteLine("2. BiciElettrica");
                System.Console.WriteLine("3. Monopattino");
                System.Console.Write("Scegli (1-3): ");
                
                var tipoChoice = System.Console.ReadLine();
                string tipo;
                bool isElettrico;
                
                switch (tipoChoice)
                {
                    case "1":
                        tipo = "BiciMuscolare";
                        isElettrico = false;
                        break;
                    case "2":
                        tipo = "BiciElettrica";
                        isElettrico = true;
                        break;
                    case "3":
                        tipo = "Monopattino";
                        isElettrico = true;
                        break;
                    default:
                        System.Console.WriteLine("Scelta non valida!");
                        await WaitKey();
                        return;
                }

                System.Console.Write("Tariffa per minuto (€, es: 0.25 o 0,25): ");
                var tariffaPerMinutoInput = System.Console.ReadLine()?.Replace('.', ','); // Normalizza il separatore decimale
                if (!decimal.TryParse(tariffaPerMinutoInput, out decimal tariffaPerMinuto) || tariffaPerMinuto <= 0)
                {
                    System.Console.WriteLine("Tariffa non valida! Usa formato: 0,25 o 0.25");
                    await WaitKey();
                    return;
                }

                System.Console.Write("Tariffa fissa di attivazione (€, default 1.00 - es: 1,50 o 1.50): ");
                var tariffaInput = System.Console.ReadLine()?.Replace('.', ','); // Normalizza il separatore decimale
                decimal tariffaFissa = 1.00m;
                if (!string.IsNullOrWhiteSpace(tariffaInput))
                {
                    if (!decimal.TryParse(tariffaInput, out tariffaFissa) || tariffaFissa < 0)
                    {
                        System.Console.WriteLine("Tariffa fissa non valida, usando default 1.00€");
                        tariffaFissa = 1.00m;
                    }
                }

                System.Console.Write("ID Parcheggio (opzionale): ");
                int? parcheggioId = null;
                var parcheggioInput = System.Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(parcheggioInput) && int.TryParse(parcheggioInput, out int pId))
                {
                    parcheggioId = pId;
                }

                var request = new 
                { 
                    Modello = modello, 
                    Tipo = tipo, 
                    IsElettrico = isElettrico, 
                    TariffaPerMinuto = tariffaPerMinuto,
                    TariffaFissa = tariffaFissa,
                    ParcheggioId = parcheggioId 
                };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{API_BASE}/admin/vehicles", content);
                if (response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine("Mezzo creato con successo!");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Console.WriteLine($"Errore nella creazione: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }

            await WaitKey();
        }

        static async Task AdminRepairVehicles()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== RIPARA MEZZI IN MANUTENZIONE ===");
            System.Console.WriteLine();

            try
            {
                // Prima mostra i mezzi in manutenzione
                var mezziResponse = await httpClient.GetAsync($"{API_BASE}/mezzi");
                if (mezziResponse.IsSuccessStatusCode)
                {
                    var mezziJson = await mezziResponse.Content.ReadAsStringAsync();
                    var mezzi = JsonSerializer.Deserialize<MezzoDto[]>(mezziJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    var mezziInManutenzione = mezzi?.Where(m => m.Stato == "Manutenzione").ToArray();
                    
                    if (mezziInManutenzione != null && mezziInManutenzione.Length > 0)
                    {
                        System.Console.WriteLine("Mezzi attualmente in manutenzione:");
                        System.Console.WriteLine();

                        foreach (var mezzo in mezziInManutenzione)
                        {
                            System.Console.WriteLine($"ID: {mezzo.Id} | {mezzo.Modello} ({mezzo.Tipo})");
                            System.Console.WriteLine($"Ultima manutenzione: {mezzo.UltimaManutenzione?.ToString("dd/MM/yyyy") ?? "Mai"}");
                            System.Console.WriteLine("───────────────────────────");
                        }

                        System.Console.WriteLine();
                        System.Console.Write("Inserisci ID mezzo da riparare (0 per tornare al menu): ");
                        if (int.TryParse(System.Console.ReadLine(), out int mezzoId) && mezzoId > 0)
                        {
                            System.Console.Write("Note riparazione: ");
                            var noteRiparazione = System.Console.ReadLine() ?? "";

                            var request = new { MezzoId = mezzoId, NoteRiparazione = noteRiparazione };
                            var json = JsonSerializer.Serialize(request);
                            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                            var response = await httpClient.PostAsync($"{API_BASE}/admin/vehicles/{mezzoId}/repair", content);
                            if (response.IsSuccessStatusCode)
                            {
                                System.Console.WriteLine("Mezzo riparato con successo!");
                            }
                            else
                            {
                                var errorContent = await response.Content.ReadAsStringAsync();
                                System.Console.WriteLine($"Errore nella riparazione: {errorContent}");
                            }
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("Nessun mezzo attualmente in manutenzione");
                    }
                }
                else
                {
                    System.Console.WriteLine($"Errore nel recupero mezzi: {mezziResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }

            await WaitKey();
        }

        static async Task AdminStatistics()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== STATISTICHE AMMINISTRATORE ===");
            System.Console.WriteLine();

            try
            {
                var response = await httpClient.GetAsync($"{API_BASE}/admin/statistics");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var stats = JsonSerializer.Deserialize<JsonElement>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    System.Console.WriteLine("=== STATISTICHE SISTEMA ===");
                    System.Console.WriteLine();
                    System.Console.WriteLine($"Mezzi totali: {stats.GetProperty("totalMezzi").GetInt32()}");
                    System.Console.WriteLine($"Mezzi disponibili: {stats.GetProperty("mezziDisponibili").GetInt32()}");
                    System.Console.WriteLine($"Mezzi in uso: {stats.GetProperty("mezziInUso").GetInt32()}");
                    System.Console.WriteLine($"Mezzi in manutenzione: {stats.GetProperty("mezziManutenzione").GetInt32()}");
                    System.Console.WriteLine($"Parcheggi totali: {stats.GetProperty("totalParcheggi").GetInt32()}");
                    System.Console.WriteLine($"Mezzi con batteria bassa: {stats.GetProperty("batteriaBassa").GetInt32()}");
                    System.Console.WriteLine($"Corse attive: {stats.GetProperty("corseAttive").GetInt32()}");
                    System.Console.WriteLine();
                    System.Console.WriteLine($"Ultimo aggiornamento: {DateTime.Parse(stats.GetProperty("ultimoAggiornamento").GetString()!):dd/MM/yyyy HH:mm:ss}");
                }
                else
                {
                    System.Console.WriteLine($"Errore nel recupero statistiche: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }

            await WaitKey();
        }

        static async Task AdminSuspendUser(int userId)
        {
            System.Console.Write("Inserisci motivo della sospensione: ");
            var motivo = System.Console.ReadLine();
            if (string.IsNullOrWhiteSpace(motivo))
            {
                System.Console.WriteLine("Motivo obbligatorio!");
                return;
            }

            var request = new { UtenteId = userId, Motivo = motivo };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync($"{API_BASE}/admin/users/{userId}/suspend", content);
                if (response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine("Utente sospeso con successo!");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Console.WriteLine($"Errore nella sospensione: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }
        }

        // === HELPER METHODS ===
        static Task WaitKey()
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Premi un tasto per continuare...");
            System.Console.ReadKey();
            return Task.CompletedTask;
        }

        static async Task AggiornaCreditoUtente()
        {
            try
            {
                var response = await httpClient.GetAsync($"{API_BASE}/user/profile");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var utente = JsonSerializer.Deserialize<UtenteDto>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    
                    if (utente != null)
                    {
                        currentUser = utente;
                    }
                }
            }
            catch
            {
                // Ignora errori nell'aggiornamento del credito
            }
        }

        // === MAINTENANCE METHODS ===
        static async Task AdminManageVehicleMaintenance()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== GESTIONE MANUTENZIONE MEZZI ===");
            System.Console.WriteLine();
            System.Console.WriteLine("1. Visualizza mezzi disponibili");
            System.Console.WriteLine("2. Metti mezzo in manutenzione");
            System.Console.WriteLine("3. Visualizza mezzi in manutenzione");
            System.Console.WriteLine("0. Torna al menu admin");
            System.Console.WriteLine();
            System.Console.Write("Scegli un'opzione: ");

            var choice = System.Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await ShowAvailableVehiclesForMaintenance();
                        break;
                    case "2":
                        await SetVehicleInMaintenance();
                        break;
                    case "3":
                        await ShowVehiclesInMaintenance();
                        break;
                    case "0":
                        return;
                    default:
                        System.Console.WriteLine("Opzione non valida");
                        await WaitKey();
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
                await WaitKey();
            }
        }

        static async Task ShowAvailableVehiclesForMaintenance()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== MEZZI DISPONIBILI ===");
            System.Console.WriteLine();

            try
            {
                var response = await httpClient.GetAsync($"{API_BASE}/mezzi");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var mezzi = JsonSerializer.Deserialize<MezzoDto[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    var mezziDisponibili = mezzi?.Where(m => m.Stato == "Disponibile").ToArray();
                    
                    if (mezziDisponibili != null && mezziDisponibili.Length > 0)
                    {
                        System.Console.WriteLine("Mezzi disponibili che possono essere messi in manutenzione:");
                        System.Console.WriteLine();

                        foreach (var mezzo in mezziDisponibili)
                        {
                            System.Console.WriteLine($"ID: {mezzo.Id} | {mezzo.Modello} ({mezzo.Tipo})");
                            System.Console.WriteLine($"Stato: {mezzo.Stato} | Batteria: {mezzo.LivelloBatteria?.ToString() ?? "N/A"}%");
                            System.Console.WriteLine($"Ultima manutenzione: {mezzo.UltimaManutenzione?.ToString("dd/MM/yyyy") ?? "Mai"}");
                            System.Console.WriteLine("───────────────────────────");
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("Nessun mezzo disponibile");
                    }
                }
                else
                {
                    System.Console.WriteLine($"Errore nel recupero mezzi: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }

            await WaitKey();
        }

        static async Task SetVehicleInMaintenance()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== METTI MEZZO IN MANUTENZIONE ===");
            System.Console.WriteLine();

            try
            {
                // Prima mostra i mezzi disponibili
                var response = await httpClient.GetAsync($"{API_BASE}/mezzi");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var mezzi = JsonSerializer.Deserialize<MezzoDto[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    var mezziDisponibili = mezzi?.Where(m => m.Stato == "Disponibile").ToArray();
                    
                    if (mezziDisponibili != null && mezziDisponibili.Length > 0)
                    {
                        System.Console.WriteLine("Mezzi disponibili:");
                        System.Console.WriteLine();

                        foreach (var mezzo in mezziDisponibili)
                        {
                            System.Console.WriteLine($"ID: {mezzo.Id} | {mezzo.Modello} ({mezzo.Tipo})");
                            System.Console.WriteLine($"Ultima manutenzione: {mezzo.UltimaManutenzione?.ToString("dd/MM/yyyy") ?? "Mai"}");
                            System.Console.WriteLine("───────────────────────────");
                        }

                        System.Console.WriteLine();
                        System.Console.Write("Inserisci ID mezzo da mettere in manutenzione (0 per annullare): ");
                        if (int.TryParse(System.Console.ReadLine(), out int mezzoId) && mezzoId > 0)
                        {
                            System.Console.Write("Note manutenzione: ");
                            var note = System.Console.ReadLine() ?? "";

                            var request = new { Note = note };
                            var jsonContent = JsonSerializer.Serialize(request);
                            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                            var maintenanceResponse = await httpClient.PostAsync($"{API_BASE}/admin/vehicles/{mezzoId}/maintenance", content);
                            if (maintenanceResponse.IsSuccessStatusCode)
                            {
                                System.Console.WriteLine("Mezzo messo in manutenzione con successo!");
                            }
                            else
                            {
                                var errorContent = await maintenanceResponse.Content.ReadAsStringAsync();
                                System.Console.WriteLine($"Errore: {errorContent}");
                            }
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("Nessun mezzo disponibile da mettere in manutenzione");
                    }
                }
                else
                {
                    System.Console.WriteLine($"Errore nel recupero mezzi: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }

            await WaitKey();
        }

        static async Task ShowVehiclesInMaintenance()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== MEZZI IN MANUTENZIONE ===");
            System.Console.WriteLine();

            try
            {
                var response = await httpClient.GetAsync($"{API_BASE}/admin/vehicles/maintenance");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var mezziManutenzione = JsonSerializer.Deserialize<MezzoMaintenanceDto[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (mezziManutenzione != null && mezziManutenzione.Length > 0)
                    {
                        System.Console.WriteLine("Mezzi attualmente in manutenzione:");
                        System.Console.WriteLine();

                        foreach (var mezzo in mezziManutenzione)
                        {
                            System.Console.WriteLine($"ID: {mezzo.Id} | {mezzo.Modello} ({mezzo.Tipo})");
                            System.Console.WriteLine($"Stato: {mezzo.Stato}");
                            System.Console.WriteLine($"Parcheggio: {mezzo.ParcheggioNome}");
                            System.Console.WriteLine($"Ultima manutenzione: {mezzo.UltimaManutenzione?.ToString("dd/MM/yyyy") ?? "Mai"}");
                            if (mezzo.LivelloBatteria.HasValue)
                            {
                                System.Console.WriteLine($"Batteria: {mezzo.LivelloBatteria}%");
                            }
                            System.Console.WriteLine("───────────────────────────");
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("Nessun mezzo attualmente in manutenzione");
                    }
                }
                else
                {
                    System.Console.WriteLine($"Errore nel recupero mezzi in manutenzione: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Errore: {ex.Message}");
            }

            await WaitKey();
        }
    }

    // === DTO CLASSES ===
    public class LoginDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class RegisterDto
    {
        public string Nome { get; set; } = "";
        public string Cognome { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
        public string? Telefono { get; set; }
    }

    public class AuthResultDto
    {
        public bool Success { get; set; }
        public string Token { get; set; } = "";
        public string Message { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
        public UtenteDto? User { get; set; }
    }

    public class TerminaCorsa
    {
        public int ParcheggioDestinazioneId { get; set; }
        public bool SegnalaManutenzione { get; set; } = false;
        public string? DescrizioneManutenzione { get; set; }
    }

    public class RicaricaCreditoDto
    {
        public int UtenteId { get; set; }
        public decimal Importo { get; set; }
        public string MetodoPagamento { get; set; } = "CartaCredito";
    }

    public class RicaricaResponseDto
    {
        public bool Success { get; set; }
        public decimal NuovoCredito { get; set; }
        public string? Message { get; set; }
        public string? TransactionId { get; set; }
    }

    public class ConvertiPuntiDto
    {
        public int PuntiDaConvertire { get; set; }
    }

    public class MezzoMaintenanceDto
    {
        public int Id { get; set; }
        public string Modello { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Stato { get; set; } = "";
        public string ParcheggioNome { get; set; } = "";
        public DateTime? UltimaManutenzione { get; set; }
        public int? LivelloBatteria { get; set; }
    }
}
