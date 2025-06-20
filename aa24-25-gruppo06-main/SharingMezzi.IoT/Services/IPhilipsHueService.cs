using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SharingMezzi.IoT.Services
{
    /// <summary>
    /// Servizio per integrare con l'emulatore Philips Hue per visualizzare stato attuatori
    /// Ogni lampadina rappresenta un mezzo o uno slot del parcheggio
    /// </summary>
    public interface IPhilipsHueService
    {
        Task<bool> SetMezzoStatusAsync(int mezzoId, MezzoStatus status);
        Task<bool> SetSlotStatusAsync(int slotId, SlotStatus status);
        Task<bool> SetParcheggioAlertAsync(int parcheggioId, AlertType alert);
        Task<Dictionary<int, HueLightState>> GetAllLightsAsync();
        Task<bool> TestConnectionAsync();
        Task<bool> InitializeLightsAsync(int numMezzi, int numSlots);
    }

    public class PhilipsHueService : IPhilipsHueService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PhilipsHueService> _logger;
        private readonly string _baseUrl;
        private readonly string _username;

        // Mapping: LightId 1-50 = Mezzi, 51-100 = Slots, 101+ = Sistema
        private const int MEZZI_LIGHT_OFFSET = 1;
        private const int SLOT_LIGHT_OFFSET = 51;
        private const int SYSTEM_LIGHT_OFFSET = 101;

        public PhilipsHueService(HttpClient httpClient, ILogger<PhilipsHueService> logger, IConfiguration config)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = config.GetValue<string>("PhilipsHue:BaseUrl") ?? "http://localhost:8000";
            _username = config.GetValue<string>("PhilipsHue:Username") ?? "newdeveloper";
        }

        /// <summary>
        /// Imposta stato visuale per un mezzo tramite lampadina Hue
        /// </summary>
        public async Task<bool> SetMezzoStatusAsync(int mezzoId, MezzoStatus status)
        {
            try
            {
                var lightId = MEZZI_LIGHT_OFFSET + mezzoId - 1;
                var lightState = GetMezzoLightState(status);
                
                var url = $"{_baseUrl}/api/{_username}/lights/{lightId}/state";
                var json = JsonSerializer.Serialize(lightState);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Set mezzo {MezzoId} to {Status} (Light {LightId})", 
                        mezzoId, status, lightId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to set mezzo {MezzoId} light: {StatusCode}", 
                        mezzoId, response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting mezzo {MezzoId} status", mezzoId);
                return false;
            }
        }

        /// <summary>
        /// Imposta stato visuale per uno slot tramite lampadina Hue
        /// </summary>
        public async Task<bool> SetSlotStatusAsync(int slotId, SlotStatus status)
        {
            try
            {
                var lightId = SLOT_LIGHT_OFFSET + slotId - 1;
                var lightState = GetSlotLightState(status);
                
                var url = $"{_baseUrl}/api/{_username}/lights/{lightId}/state";
                var json = JsonSerializer.Serialize(lightState);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Set slot {SlotId} to {Status} (Light {LightId})", 
                        slotId, status, lightId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to set slot {SlotId} light: {StatusCode}", 
                        slotId, response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting slot {SlotId} status", slotId);
                return false;
            }
        }

        /// <summary>
        /// Imposta alert di sistema per un parcheggio
        /// </summary>
        public async Task<bool> SetParcheggioAlertAsync(int parcheggioId, AlertType alert)
        {
            try
            {
                var lightId = SYSTEM_LIGHT_OFFSET + parcheggioId;
                var lightState = GetAlertLightState(alert);
                
                var url = $"{_baseUrl}/api/{_username}/lights/{lightId}/state";
                var json = JsonSerializer.Serialize(lightState);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Set parcheggio {ParcheggioId} alert to {Alert} (Light {LightId})", 
                        parcheggioId, alert, lightId);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting parcheggio {ParcheggioId} alert", parcheggioId);
                return false;
            }
        }

        /// <summary>
        /// Ottiene stato di tutte le lampadine
        /// </summary>
        public async Task<Dictionary<int, HueLightState>> GetAllLightsAsync()
        {
            try
            {
                var url = $"{_baseUrl}/api/{_username}/lights";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var lights = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    
                    var result = new Dictionary<int, HueLightState>();
                    if (lights != null)
                    {
                        foreach (var light in lights)
                        {
                            if (int.TryParse(light.Key, out var lightId))
                            {
                                // Parse light state from JSON
                                result[lightId] = new HueLightState(); // Simplified
                            }
                        }
                    }
                    
                    return result;
                }
                
                return new Dictionary<int, HueLightState>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all lights");
                return new Dictionary<int, HueLightState>();
            }
        }

        /// <summary>
        /// Testa connessione all'emulatore Hue
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var url = $"{_baseUrl}/api/{_username}/lights";
                var response = await _httpClient.GetAsync(url);
                var isConnected = response.IsSuccessStatusCode;
                
                if (isConnected)
                {
                    _logger.LogInformation("Philips Hue emulator connection OK");
                }
                else
                {
                    _logger.LogWarning("Philips Hue emulator not reachable at {Url}", url);
                }
                
                return isConnected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Hue connection");
                return false;
            }
        }

        /// <summary>
        /// Inizializza le lampadine per mezzi e slot
        /// </summary>
        public async Task<bool> InitializeLightsAsync(int numMezzi, int numSlots)
        {
            _logger.LogInformation("Initializing {NumMezzi} mezzi lights and {NumSlots} slot lights", 
                numMezzi, numSlots);
            
            var success = true;
            
            // Inizializza lampadine mezzi (tutte spente all'inizio)
            for (int i = 1; i <= numMezzi; i++)
            {
                success &= await SetMezzoStatusAsync(i, MezzoStatus.Disponibile);
                await Task.Delay(100); // Evita stress sull'emulatore
            }
            
            // Inizializza lampadine slot (tutte verdi = libere)
            for (int i = 1; i <= numSlots; i++)
            {
                success &= await SetSlotStatusAsync(i, SlotStatus.Libero);
                await Task.Delay(100);
            }
            
            _logger.LogInformation(success ? 
                "Hue lights initialization completed" : 
                "Hue lights initialization completed with some errors");
            
            return success;
        }

        /// <summary>
        /// Converte stato mezzo in parametri lampadina Hue
        /// </summary>
        private object GetMezzoLightState(MezzoStatus status)
        {
            return status switch
            {
                MezzoStatus.Disponibile => new { on = true, hue = 25500, sat = 254, bri = 150 }, // Verde
                MezzoStatus.InUso => new { on = true, hue = 46920, sat = 254, bri = 200 }, // Blu
                MezzoStatus.Manutenzione => new { on = true, hue = 65535, sat = 254, bri = 200 }, // Rosso
                MezzoStatus.BatteriaBassa => new { on = true, hue = 12750, sat = 254, bri = 200, alert = "select" }, // Arancione lampeggiante
                MezzoStatus.Offline => new { on = false }, // Spenta
                _ => new { on = true, hue = 0, sat = 0, bri = 50 } // Bianco tenue
            };
        }

        /// <summary>
        /// Converte stato slot in parametri lampadina Hue
        /// </summary>
        private object GetSlotLightState(SlotStatus status)
        {
            return status switch
            {
                SlotStatus.Libero => new { on = true, hue = 25500, sat = 254, bri = 100 }, // Verde tenue
                SlotStatus.Occupato => new { on = true, hue = 65535, sat = 254, bri = 150 }, // Rosso
                SlotStatus.Riservato => new { on = true, hue = 12750, sat = 254, bri = 150 }, // Arancione
                SlotStatus.Manutenzione => new { on = true, hue = 65535, sat = 254, bri = 200, alert = "select" }, // Rosso lampeggiante
                _ => new { on = false } // Spenta
            };
        }

        /// <summary>
        /// Converte alert di sistema in parametri lampadina Hue
        /// </summary>
        private object GetAlertLightState(AlertType alert)
        {
            return alert switch
            {
                AlertType.Normal => new { on = true, hue = 25500, sat = 254, bri = 50 }, // Verde tenue
                AlertType.Warning => new { on = true, hue = 12750, sat = 254, bri = 200, alert = "select" }, // Arancione lampeggiante
                AlertType.Critical => new { on = true, hue = 65535, sat = 254, bri = 254, alert = "lselect" }, // Rosso lampeggiante intenso
                AlertType.Offline => new { on = false }, // Spenta
                _ => new { on = true, hue = 0, sat = 0, bri = 100 } // Bianco
            };
        }
    }

    // ===== ENUMS E DTO =====

    public enum MezzoStatus
    {
        Disponibile,
        InUso,
        Manutenzione,
        BatteriaBassa,
        Offline
    }

    public enum SlotStatus
    {
        Libero,
        Occupato,
        Riservato,
        Manutenzione
    }

    public enum AlertType
    {
        Normal,
        Warning,
        Critical,
        Offline
    }

    public class HueLightState
    {
        public bool On { get; set; }
        public int Hue { get; set; }
        public int Saturation { get; set; }
        public int Brightness { get; set; }
        public string Alert { get; set; } = "none";
        public bool Reachable { get; set; } = true;
    }
}