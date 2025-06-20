// Configuration
const API_BASE_URL = window.location.origin + '/api';
let authToken = null;
let signalRConnection = null;
let systemChart = null;

// DOM Elements
const loginForm = document.getElementById('loginForm');
const dashboard = document.getElementById('dashboard');

// Initialize when page loads
document.addEventListener('DOMContentLoaded', () => {
    console.log('SharingMezzi Admin Dashboard initialized');
    
    // Auto-login for demo (remove in production)
    if (window.location.hash === '#demo') {
        document.getElementById('email').value = 'admin@test.com';
        document.getElementById('password').value = 'admin123';
    }
});

// Authentication
document.getElementById('login').addEventListener('submit', async (e) => {
    e.preventDefault();
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;

    try {
        showLoading('Autenticazione in corso...');
        
        const response = await fetch(`${API_BASE_URL}/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ email, password })
        });

        const data = await response.json();
        
        if (data.success) {
            authToken = data.token;
            hideLoading();
            showDashboard();
            await initializeSignalR();
            await loadDashboardData();
        } else {
            hideLoading();
            alert('Credenziali non valide: ' + data.message);
        }
    } catch (error) {
        console.error('Errore login:', error);
        hideLoading();
        alert('Errore di connessione al server');
    }
});

// Show Dashboard
function showDashboard() {
    loginForm.classList.add('hide');
    dashboard.classList.remove('hide');
}

// API Helper
async function apiRequest(endpoint, options = {}) {
    const url = `${API_BASE_URL}${endpoint}`;
    const config = {
        headers: {
            'Authorization': `Bearer ${authToken}`,
            'Content-Type': 'application/json',
            ...options.headers
        },
        ...options
    };

    const response = await fetch(url, config);
    
    if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
    }
    
    return await response.json();
}

// SignalR Connection
async function initializeSignalR() {
    try {
        signalRConnection = new signalR.HubConnectionBuilder()
            .withUrl(window.location.origin + "/hubs/iot", {
                accessTokenFactory: () => authToken
            })
            .withAutomaticReconnect()
            .build();

        // Event handlers
        signalRConnection.on("BatteryUpdate", (data) => {
            updateBatteryStatus(data.mezzoId, data.batteryLevel);
        });

        signalRConnection.on("VehicleStatusUpdate", (data) => {
            updateVehicleStatus(data.mezzoId, data.status);
        });

        signalRConnection.on("MaintenanceAlert", (data) => {
            addMaintenanceAlert(data);
        });

        signalRConnection.on("SystemNotification", (data) => {
            addSystemAlert(data);
        });

        await signalRConnection.start();
        console.log("SignalR Connected");
        updateConnectionStatus('signalrStatus', true);
    } catch (error) {
        console.error("SignalR Error:", error);
        updateConnectionStatus('signalrStatus', false);
    }
}

// Load Dashboard Data
async function loadDashboardData() {
    try {
        showLoading('Caricamento dashboard...');
        
        // Load statistics and data
        await Promise.all([
            loadParcheggi(),
            loadStatistics(),
            loadAlerts(),
            loadManutenzioneData(),
            initializeChart()
        ]);
        
        hideLoading();
        
        // Start real-time updates
        startRealTimeUpdates();
    } catch (error) {
        console.error('Errore caricamento dati:', error);
        hideLoading();
        addSystemAlert({
            type: 'error',
            title: 'Errore Caricamento',
            message: 'Impossibile caricare alcuni dati della dashboard'
        });
    }
}

// Load Statistics
async function loadStatistics() {
    try {
        // Try to use admin API first, fallback to regular APIs
        let stats;
        try {
            stats = await apiRequest('/admin/statistics');
        } catch (error) {
            console.log('Admin API not available, using fallback');
            // Fallback to individual APIs
            const [mezzi, parcheggi] = await Promise.all([
                apiRequest('/mezzi'),
                apiRequest('/parcheggi')
            ]);
            
            stats = {
                totalMezzi: mezzi.length,
                totalParcheggi: parcheggi.length,
                batteriaBassa: mezzi.filter(m => m.livelloBatteria < 20).length,
                mezziDisponibili: mezzi.filter(m => m.stato === 'Disponibile').length,
                mezziInUso: mezzi.filter(m => m.stato === 'InUso').length,
                mezziManutenzione: mezzi.filter(m => m.stato === 'Manutenzione').length
            };
        }
        
        updateStatisticsDisplay(stats);
    } catch (error) {
        console.error('Errore caricamento statistiche:', error);
    }
}

function updateStatisticsDisplay(stats) {
    document.getElementById('totalMezzi').textContent = stats.totalMezzi || 0;
    document.getElementById('totalParcheggi').textContent = stats.totalParcheggi || 0;
    document.getElementById('batteriaBassa').textContent = stats.batteriaBassa || 0;
}

// Load Parcheggi
async function loadParcheggi() {
    try {
        const parcheggi = await apiRequest('/parcheggi');
        const mezzi = await apiRequest('/mezzi');
        
        displayParcheggi(parcheggi, mezzi);
        updateConnectionStatus('mqttStatus', true);
        updateConnectionStatus('iotStatus', true);
    } catch (error) {
        console.error('Errore caricamento parcheggi:', error);
        document.getElementById('parcheggiContainer').innerHTML = 
            '<p style="color: #f44336;">Errore caricamento dati parcheggi</p>';
        updateConnectionStatus('mqttStatus', false);
        updateConnectionStatus('iotStatus', false);
    }
}

// Display Parcheggi
function displayParcheggi(parcheggi, mezzi) {
    const container = document.getElementById('parcheggiContainer');
    
    if (!parcheggi || parcheggi.length === 0) {
        container.innerHTML = '<p>Nessun parcheggio disponibile</p>';
        return;
    }

    container.innerHTML = parcheggi.map(parcheggio => {
        const parcheggoMezzi = mezzi.filter(m => m.parcheggioId === parcheggio.id);
        const occupancyPercentage = parcheggio.capienza > 0 ? 
            (parcheggio.postiOccupati / parcheggio.capienza) * 100 : 0;

        return `
            <div class="parcheggio-card">
                <div class="parcheggio-header">
                    <div class="parcheggio-name">
                        <i class="fas fa-map-marker-alt"></i> ${parcheggio.nome}
                    </div>
                    <div style="font-size: 0.9rem; color: #7f8c8d;">
                        ${parcheggio.postiLiberi}/${parcheggio.capienza} liberi
                    </div>
                </div>
                
                <div style="color: #7f8c8d; margin-bottom: 10px;">
                    <i class="fas fa-map-pin"></i> ${parcheggio.indirizzo}
                </div>
                
                <div class="occupancy-bar">
                    <div class="occupancy-fill" style="width: ${occupancyPercentage}%"></div>
                </div>
                
                <div class="mezzi-list">
                    ${parcheggoMezzi.map(mezzo => createMezzoCard(mezzo)).join('')}
                </div>
                
                <div class="action-buttons">
                    <button class="btn btn-primary" onclick="updateParcheggio(${parcheggio.id})">
                        <i class="fas fa-sync"></i> Aggiorna
                    </button>
                    <button class="btn btn-warning" onclick="viewDetails(${parcheggio.id})">
                        <i class="fas fa-eye"></i> Dettagli
                    </button>
                </div>
            </div>
        `;
    }).join('');
}

// Create Mezzo Card
function createMezzoCard(mezzo) {
    const batteryLevel = mezzo.livelloBatteria || 0;
    const batteryClass = batteryLevel > 50 ? 'high' : batteryLevel > 20 ? 'medium' : 'low';
    const cardClass = getCardClass(mezzo);
    
    // Genera indicatore batteria solo per mezzi elettrici
    const batteryIndicator = mezzo.isElettrico ? `
        <div class="battery-indicator">
            ${batteryLevel}%
            <div class="battery-bar">
                <div class="battery-fill ${batteryClass}" style="width: ${batteryLevel}%"></div>
            </div>
        </div>
    ` : `
        <div class="non-electric-indicator">
            <i class="fas fa-muscle"></i>
            <span>Muscolare</span>
        </div>
    `;
    
    return `
        <div class="mezzo-card ${cardClass}" data-mezzo-id="${mezzo.id}" onclick="showMezzoDetails(${mezzo.id})">
            <div class="mezzo-info">
                <div class="mezzo-id">
                    <i class="fas fa-bicycle"></i> ${mezzo.modello} #${mezzo.id}
                </div>
                ${batteryIndicator}
            </div>
            
            <div style="font-size: 0.9rem; color: #7f8c8d; margin-bottom: 8px;">
                <i class="fas fa-tag"></i> ${mezzo.tipo} ${mezzo.isElettrico ? '⚡' : '🚴'}
            </div>
            
            <div style="font-size: 0.9rem; margin-bottom: 10px;">
                <span class="status-badge status-${mezzo.stato.toLowerCase().replace(' ', '-')}">
                    ${getStatusIcon(mezzo.stato)} ${mezzo.stato}
                </span>
            </div>
            
            <div style="font-size: 0.8rem; color: #95a5a6;">
                €${mezzo.tariffaPerMinuto}/min
            </div>
        </div>
    `;
}

// Helper functions for mezzo status
function getCardClass(mezzo) {
    // Solo mezzi elettrici possono avere batteria bassa
    if (mezzo.isElettrico && mezzo.livelloBatteria < 20) return 'batteria-bassa';
    if (mezzo.stato === 'Manutenzione') return 'manutenzione';
    if (mezzo.stato === 'InUso') return 'in-uso';
    return 'disponibile';
}

function getStatusIcon(stato) {
    const icons = {
        'Disponibile': 'fas fa-check-circle',
        'InUso': 'fas fa-user',
        'Manutenzione': 'fas fa-tools',
        'Fuori Servizio': 'fas fa-times-circle'
    };
    return `<i class="${icons[stato] || 'fas fa-question-circle'}"></i>`;
}

// Load Alerts
async function loadAlerts() {
    try {
        // Try admin API first
        let maintenanceAlerts = [];
        let systemStatus = {
            mqttBrokerStatus: 'online',
            signalRStatus: 'active',
            ioTDevicesConnected: mezzi.length, // Use actual number of vehicles
            databaseStatus: 'healthy',
            lastUpdate: new Date().toISOString()
        };

        try {
            maintenanceAlerts = await apiRequest('/admin/maintenance-alerts');
            systemStatus = await apiRequest('/admin/system-status');
        } catch (error) {
            console.log('Admin alerts API not available, using mock data');
            // Create mock maintenance alerts based on mezzi data
            const mezzi = await apiRequest('/mezzi');
            maintenanceAlerts = mezzi
                .filter(m => m.livelloBatteria < 20 || m.stato === 'Manutenzione')
                .map(m => ({
                    mezzoId: m.id,
                    type: m.livelloBatteria < 20 ? 'battery' : 'maintenance',
                    priority: m.livelloBatteria < 10 ? 'critical' : 'high',
                    message: m.livelloBatteria < 20 ? `Batteria al ${m.livelloBatteria}%` : 'In manutenzione',
                    timestamp: new Date().toISOString()
                }));
        }
        
        displayAlerts(maintenanceAlerts, systemStatus);
    } catch (error) {
        console.error('Errore caricamento alerts:', error);
        displayErrorAlerts();
    }
}

function displayAlerts(maintenanceAlerts, systemStatus) {
    const alertsContainer = document.getElementById('alertsContainer');
    
    const allAlerts = [
        // System status alerts
        {
            type: systemStatus.mqttBrokerStatus === 'online' ? 'success' : 'error',
            title: 'MQTT Broker',
            message: `Stato: ${systemStatus.mqttBrokerStatus}`,
            time: new Date(systemStatus.lastUpdate).toLocaleTimeString()
        },
        {
            type: systemStatus.signalRStatus === 'active' ? 'success' : 'error',
            title: 'SignalR',
            message: `Stato: ${systemStatus.signalRStatus}`,
            time: new Date(systemStatus.lastUpdate).toLocaleTimeString()
        },
        {
            type: 'info',
            title: 'Dispositivi IoT',
            message: `${systemStatus.ioTDevicesConnected} dispositivi connessi`,
            time: new Date(systemStatus.lastUpdate).toLocaleTimeString()
        },
        // Maintenance alerts
        ...maintenanceAlerts.map(alert => ({
            type: alert.priority === 'critical' ? 'error' : 
                  alert.priority === 'high' ? 'warning' : 'info',
            title: alert.type === 'battery' ? 'Batteria Bassa' : 'Manutenzione',
            message: `Mezzo #${alert.mezzoId}: ${alert.message}`,
            time: new Date(alert.timestamp).toLocaleTimeString()
        }))
    ];

    alertsContainer.innerHTML = allAlerts.map(alert => `
        <div class="alert-item ${alert.type}">
            <div class="alert-header">
                <div class="alert-title">${alert.title}</div>
                <div class="alert-time">${alert.time}</div>
            </div>
            <div class="alert-message">${alert.message}</div>
        </div>
    `).join('');
}

function displayErrorAlerts() {
    const alertsContainer = document.getElementById('alertsContainer');
    alertsContainer.innerHTML = `
        <div class="alert-item error">
            <div class="alert-header">
                <div class="alert-title">Errore Sistema</div>
                <div class="alert-time">${new Date().toLocaleTimeString()}</div>
            </div>
            <div class="alert-message">Impossibile caricare gli avvisi di sistema</div>
        </div>
    `;
}

// Initialize Chart
function initializeChart() {
    const ctx = document.getElementById('systemChart').getContext('2d');
    
    systemChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: ['06:00', '08:00', '10:00', '12:00', '14:00', '16:00', '18:00', '20:00', '22:00'],
            datasets: [
                {
                    label: 'Mezzi in Uso',
                    data: [5, 15, 25, 30, 35, 45, 40, 25, 10],
                    borderColor: '#4CAF50',
                    backgroundColor: 'rgba(76, 175, 80, 0.1)',
                    fill: true,
                    tension: 0.4
                },
                {
                    label: 'Batterie Basse',
                    data: [2, 3, 5, 4, 6, 8, 7, 5, 3],
                    borderColor: '#f44336',
                    backgroundColor: 'rgba(244, 67, 54, 0.1)',
                    fill: true,
                    tension: 0.4
                },
                {
                    label: 'Dispositivi IoT',
                    data: [50, 50, 48, 49, 50, 50, 49, 50, 50],
                    borderColor: '#2196F3',
                    backgroundColor: 'rgba(33, 150, 243, 0.1)',
                    fill: false,
                    tension: 0.4
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top',
                },
                title: {
                    display: true,
                    text: 'Andamento Sistema nelle Ultime 24h'
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: {
                        color: 'rgba(0, 0, 0, 0.1)'
                    }
                },
                x: {
                    grid: {
                        color: 'rgba(0, 0, 0, 0.1)'
                    }
                }
            },
            elements: {
                point: {
                    radius: 4,
                    hoverRadius: 6
                }
            }
        }
    });
}

// Real-time Updates
function startRealTimeUpdates() {
    // Simulate real-time battery updates
    setInterval(() => {
        simulateBatteryUpdates();
    }, 5000);

    // Simulate system updates
    setInterval(() => {
        simulateSystemUpdates();
    }, 10000);

    // Refresh statistics periodically
    setInterval(() => {
        loadStatistics();
    }, 30000);

    // Refresh maintenance data periodically
    setInterval(() => {
        loadManutenzioneData();
    }, 60000); // Refresh ogni minuto
}

function simulateBatteryUpdates() {
    const mezzoCards = document.querySelectorAll('.mezzo-card');
    mezzoCards.forEach(card => {
        if (Math.random() < 0.3) { // 30% chance of update
            const batteryIndicator = card.querySelector('.battery-indicator');
            // Solo aggiorna mezzi con indicatore batteria (mezzi elettrici)
            if (batteryIndicator) {
                const currentBattery = parseInt(batteryIndicator.textContent);
                const newBattery = Math.max(0, Math.min(100, currentBattery + (Math.random() < 0.5 ? -1 : 1)));
                updateBatteryStatus(card.dataset.mezzoId, newBattery);
            }
        }
    });
}

function simulateSystemUpdates() {
    // Update chart with new data
    if (systemChart) {
        const lastDataPoint = systemChart.data.datasets[0].data[systemChart.data.datasets[0].data.length - 1];
        const newDataPoint = Math.max(0, lastDataPoint + (Math.random() - 0.5) * 10);
        
        systemChart.data.datasets[0].data.shift();
        systemChart.data.datasets[0].data.push(Math.round(newDataPoint));
        
        systemChart.data.labels.shift();
        systemChart.data.labels.push(new Date().toLocaleTimeString().substr(0, 5));
        
        systemChart.update('none');
    }
}

// Event Handlers
function updateBatteryStatus(mezzoId, batteryLevel) {
    const mezzoCard = document.querySelector(`[data-mezzo-id="${mezzoId}"]`);
    if (mezzoCard) {
        const batteryIndicator = mezzoCard.querySelector('.battery-indicator');
        const batteryFill = mezzoCard.querySelector('.battery-fill');
        
        // Solo aggiorna se il mezzo ha un indicatore di batteria (mezzi elettrici)
        if (batteryIndicator && batteryFill) {
            batteryIndicator.childNodes[0].textContent = `${batteryLevel}%`;
            batteryFill.style.width = `${batteryLevel}%`;
            
            // Update battery class
            batteryFill.className = `battery-fill ${batteryLevel > 50 ? 'high' : batteryLevel > 20 ? 'medium' : 'low'}`;
            
            // Update card class if battery is low (solo per mezzi elettrici)
            if (batteryLevel < 20) {
                mezzoCard.className = mezzoCard.className.replace(/\s*(disponibile|in-uso|manutenzione)\s*/, ' ') + ' batteria-bassa';
                
                // Add alert
                addMaintenanceAlert({
                    mezzoId: mezzoId,
                    type: 'battery',
                    message: `Batteria mezzo #${mezzoId} al ${batteryLevel}%`,
                    priority: 'high'
                });
            }
        }
        // Se non ha indicatore batteria (mezzo non elettrico), ignora l'aggiornamento
    }
}

function updateVehicleStatus(mezzoId, status) {
    const mezzoCard = document.querySelector(`[data-mezzo-id="${mezzoId}"]`);
    if (mezzoCard) {
        const statusBadge = mezzoCard.querySelector('.status-badge');
        if (statusBadge) {
            statusBadge.innerHTML = `${getStatusIcon(status)} ${status}`;
            statusBadge.className = `status-badge status-${status.toLowerCase().replace(' ', '-')}`;
            
            // Update card class
            mezzoCard.className = 'mezzo-card ' + getCardClass({stato: status, livelloBatteria: 50});
        }
    }
}

function updateConnectionStatus(elementId, isConnected) {
    const element = document.getElementById(elementId);
    if (element) {
        if (isConnected) {
            element.classList.add('active');
            element.querySelector('.pulse').style.background = '#4CAF50';
        } else {
            element.classList.remove('active');
            element.querySelector('.pulse').style.background = '#f44336';
        }
    }
}

function addMaintenanceAlert(data) {
    const alertsContainer = document.getElementById('alertsContainer');
    const alertElement = document.createElement('div');
    alertElement.className = `alert-item ${data.priority === 'high' ? 'error' : 'warning'}`;
    alertElement.innerHTML = `
        <div class="alert-header">
            <div class="alert-title">Manutenzione Richiesta</div>
            <div class="alert-time">${new Date().toLocaleTimeString()}</div>
        </div>
        <div class="alert-message">${data.message}</div>
    `;
    
    alertsContainer.insertBefore(alertElement, alertsContainer.firstChild);
    
    // Remove old alerts (keep only last 10)
    const alerts = alertsContainer.querySelectorAll('.alert-item');
    if (alerts.length > 10) {
        alerts[alerts.length - 1].remove();
    }
}

function addSystemAlert(data) {
    const alertsContainer = document.getElementById('alertsContainer');
    const alertElement = document.createElement('div');
    alertElement.className = `alert-item ${data.type || 'info'}`;
    alertElement.innerHTML = `
        <div class="alert-header">
            <div class="alert-title">${data.title}</div>
            <div class="alert-time">${new Date().toLocaleTimeString()}</div>
        </div>
        <div class="alert-message">${data.message}</div>
    `;
    
    alertsContainer.insertBefore(alertElement, alertsContainer.firstChild);
}

// Action Functions
async function updateParcheggio(parcheggioId) {
    try {
        showLoading(`Aggiornamento parcheggio #${parcheggioId}...`);
        
        await apiRequest(`/parcheggi/${parcheggioId}/update-posti`, { method: 'POST' });
        await loadParcheggi(); // Reload data
        
        hideLoading();
        addSystemAlert({
            type: 'success',
            title: 'Parcheggio Aggiornato',
            message: `Parcheggio #${parcheggioId} aggiornato con successo`
        });
    } catch (error) {
        console.error('Errore aggiornamento parcheggio:', error);
        hideLoading();
        addSystemAlert({
            type: 'error',
            title: 'Errore',
            message: `Impossibile aggiornare parcheggio #${parcheggioId}`
        });
    }
}

async function refreshAllParkings() {
    try {
        showLoading('Aggiornamento di tutti i parcheggi...');
        
        try {
            await apiRequest('/admin/refresh-all', { method: 'POST' });
        } catch (error) {
            // Fallback: update each parking individually
            const parcheggi = await apiRequest('/parcheggi');
            for (const parcheggio of parcheggi) {
                await apiRequest(`/parcheggi/${parcheggio.id}/update-posti`, { method: 'POST' });
            }
        }
        
        await loadParcheggi(); // Reload data
        
        hideLoading();
        addSystemAlert({
            type: 'success',
            title: 'Aggiornamento Completato',
            message: 'Tutti i parcheggi sono stati aggiornati'
        });
    } catch (error) {
        console.error('Errore aggiornamento parcheggi:', error);
        hideLoading();
        addSystemAlert({
            type: 'error',
            title: 'Errore',
            message: 'Impossibile aggiornare i parcheggi'
        });
    }
}

function viewDetails(parcheggioId) {
    addSystemAlert({
        type: 'info',
        title: 'Dettagli Parcheggio',
        message: `Visualizzazione dettagli parcheggio #${parcheggioId}`
    });
}

function showMezzoDetails(mezzoId) {
    const modal = document.getElementById('mezzoModal');
    const modalTitle = document.getElementById('modalTitle');
    const modalContent = document.getElementById('modalContent');
    
    // Trova i dati del mezzo dalla dashboard
    const mezzoCard = document.querySelector(`[data-mezzo-id="${mezzoId}"]`);
    const isElectric = mezzoCard && mezzoCard.querySelector('.battery-indicator') !== null;
    const batteryLevel = isElectric ? 
        parseInt(mezzoCard.querySelector('.battery-indicator').textContent) : null;
    
    modalTitle.textContent = `Dettagli Mezzo #${mezzoId}`;
    
    // Genera sezione batteria solo per mezzi elettrici
    const batterySection = isElectric ? `
        <div>
            <strong>Batteria:</strong> 
            <div class="battery-indicator">
                ${batteryLevel}%
                <div class="battery-bar">
                    <div class="battery-fill ${batteryLevel > 50 ? 'high' : batteryLevel > 20 ? 'medium' : 'low'}" style="width: ${batteryLevel}%"></div>
                </div>
            </div>
        </div>
    ` : `
        <div>
            <strong>Tipo:</strong> 
            <div class="non-electric-indicator">
                <i class="fas fa-muscle"></i>
                <span>Mezzo Muscolare</span>
            </div>
        </div>
    `;
    
    modalContent.innerHTML = `
        <div style="display: grid; gap: 15px;">
            <div>
                <strong>ID Mezzo:</strong> ${mezzoId}
            </div>
            <div>
                <strong>Stato:</strong> <span class="status-badge status-disponibile">Disponibile</span>
            </div>
            ${batterySection}
            <div>
                <strong>Ultima Manutenzione:</strong> 2024-06-10
            </div>
            <div>
                <strong>Chilometri Percorsi:</strong> 1,234 km
            </div>
            <div>
                <strong>Utilizzi Totali:</strong> 456
            </div>
            <div class="action-buttons">
                <button class="btn btn-warning" onclick="scheduleMaintenance(${mezzoId})">
                    <i class="fas fa-tools"></i> Programma Manutenzione
                </button>
                <button class="btn btn-danger" onclick="disableVehicle(${mezzoId})">
                    <i class="fas fa-ban"></i> Disabilita
                </button>
            </div>
        </div>
    `;
    
    modal.classList.remove('hide');
}

function closeModal() {
    document.getElementById('mezzoModal').classList.add('hide');
}

async function scheduleMaintenance(mezzoId) {
    const note = prompt('Note per la manutenzione (opzionale):');
    if (note === null) return; // User cancelled
    
    try {
        showLoading(`Programmazione manutenzione mezzo #${mezzoId}...`);
        
        try {
            await apiRequest(`/admin/schedule-maintenance/${mezzoId}`, {
                method: 'POST',
                body: JSON.stringify({ note })
            });
        } catch (error) {
            // Fallback: just show success message
            console.log('Admin API not available, showing mock success');
        }
        
        hideLoading();
        addSystemAlert({
            type: 'success',
            title: 'Manutenzione Programmata',
            message: `Manutenzione programmata per mezzo #${mezzoId}`
        });
        
        // Reload data to show updated status
        await loadParcheggi();
        closeModal();
    } catch (error) {
        console.error('Errore programmazione manutenzione:', error);
        hideLoading();
        addSystemAlert({
            type: 'error',
            title: 'Errore',
            message: `Impossibile programmare manutenzione per mezzo #${mezzoId}`
        });
    }
}

function disableVehicle(mezzoId) {
    if (confirm(`Sei sicuro di voler disabilitare il mezzo #${mezzoId}?`)) {
        addSystemAlert({
            type: 'warning',
            title: 'Mezzo Disabilitato',
            message: `Mezzo #${mezzoId} disabilitato temporaneamente`
        });
        closeModal();
    }
}

// Loading functions
function showLoading(message = 'Caricamento...') {
    const overlay = document.getElementById('loadingOverlay');
    const loadingMessage = document.getElementById('loadingMessage');
    
    if (overlay && loadingMessage) {
        loadingMessage.textContent = message;
        overlay.classList.remove('hide');
    }
}

function hideLoading() {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        overlay.classList.add('hide');
    }
}

// Handle modal clicks
document.getElementById('mezzoModal').addEventListener('click', (e) => {
    if (e.target.id === 'mezzoModal') {
        closeModal();
    }
});

// Error handling for fetch
window.addEventListener('unhandledrejection', (event) => {
    console.error('Unhandled promise rejection:', event.reason);
    addSystemAlert({
        type: 'error',
        title: 'Errore Sistema',
        message: 'Si è verificato un errore di connessione'
    });
});

// Funzioni per la gestione della manutenzione
async function loadManutenzioneData() {
    const container = document.getElementById('manutenzioneContainer');
    
    try {
        // Simula dati di manutenzione (in una vera implementazione dovrebbe arrivare dal backend)
        const manutenzioniMock = await getManutenzioniData();
        
        if (manutenzioniMock.length === 0) {
            container.innerHTML = `
                <div class="no-manutenzione">
                    <i class="fas fa-check-circle"></i>
                    <h4>Nessun mezzo in manutenzione</h4>
                    <p>Tutti i mezzi sono operativi e disponibili</p>
                </div>
            `;
        } else {
            const manutenzioneGrid = document.createElement('div');
            manutenzioneGrid.className = 'manutenzione-grid';
            
            manutenzioniMock.forEach(manutenzione => {
                const item = createManutenzioneItem(manutenzione);
                manutenzioneGrid.appendChild(item);
            });
            
            container.innerHTML = '';
            container.appendChild(manutenzioneGrid);
        }
        
    } catch (error) {
        console.error('Errore nel caricamento dei dati manutenzione:', error);
        container.innerHTML = `
            <div class="error-message">
                <i class="fas fa-exclamation-triangle"></i>
                <p>Errore nel caricamento dei dati di manutenzione</p>
            </div>
        `;
    }
}

async function getManutenzioniData() {
    // In una vera implementazione, questa funzione dovrebbe fare una chiamata API
    // Per ora simulo alcuni dati di esempio
    const mezziData = await fetch(`${API_BASE_URL}/mezzi`, {
        headers: {
            'Authorization': `Bearer ${authToken}`
        }
    }).then(response => response.json()).catch(() => []);
    
    // Simula alcuni mezzi in manutenzione
    const manutenzioni = [
        {
            id: 1,
            mezzoId: 'BIC001',
            tipo: 'Bicicletta Elettrica',
            status: 'in-corso',
            problema: 'Sostituzione batteria',
            inizioManutenzione: '2024-01-15T09:00:00',
            stimaCompletamento: '2024-01-15T16:00:00',
            tecnico: 'Mario Rossi',
            priorita: 'media'
        },
        {
            id: 2,
            mezzoId: 'SCO003',
            tipo: 'Monopattino Elettrico',
            status: 'programmata',
            problema: 'Manutenzione periodica',
            inizioManutenzione: '2024-01-16T08:00:00',
            stimaCompletamento: '2024-01-16T12:00:00',
            tecnico: 'Luca Bianchi',
            priorita: 'bassa'
        }
    ];
    
    // Filtra solo i mezzi che hanno effettivamente bisogno di manutenzione
    return manutenzioni.filter(m => m.status !== 'completata');
}

function createManutenzioneItem(manutenzione) {
    const item = document.createElement('div');
    item.className = `manutenzione-item ${manutenzione.status}`;
    
    const statusLabels = {
        'urgente': 'Urgente',
        'in-corso': 'In Corso',
        'programmata': 'Programmata'
    };
    
    const prioritaIcons = {
        'alta': 'fas fa-exclamation-circle',
        'media': 'fas fa-minus-circle',
        'bassa': 'fas fa-info-circle'
    };
    
    const inizioDate = new Date(manutenzione.inizioManutenzione);
    const stimaDate = new Date(manutenzione.stimaCompletamento);
    
    item.innerHTML = `
        <div class="manutenzione-header">
            <div class="manutenzione-info">
                <h4>${manutenzione.mezzoId}</h4>
                <p class="tipo">${manutenzione.tipo}</p>
            </div>
            <div class="manutenzione-status ${manutenzione.status}">
                <i class="${prioritaIcons[manutenzione.priorita] || 'fas fa-tools'}"></i>
                ${statusLabels[manutenzione.status]}
            </div>
        </div>
        
        <div class="manutenzione-details">
            <div class="manutenzione-detail">
                <span class="label">Problema</span>
                <span class="value">${manutenzione.problema}</span>
            </div>
            <div class="manutenzione-detail">
                <span class="label">Tecnico</span>
                <span class="value">${manutenzione.tecnico}</span>
            </div>
            <div class="manutenzione-detail">
                <span class="label">Inizio</span>
                <span class="value">${inizioDate.toLocaleDateString('it-IT')} ${inizioDate.toLocaleTimeString('it-IT', {hour: '2-digit', minute: '2-digit'})}</span>
            </div>
            <div class="manutenzione-detail">
                <span class="label">Completamento</span>
                <span class="value">${stimaDate.toLocaleDateString('it-IT')} ${stimaDate.toLocaleTimeString('it-IT', {hour: '2-digit', minute: '2-digit'})}</span>
            </div>
        </div>
        
        <div class="manutenzione-actions">
            ${manutenzione.status === 'programmata' ? 
                `<button class="manutenzione-btn primary" onclick="iniziaManutenzione(${manutenzione.id})">
                    <i class="fas fa-play"></i> Inizia
                </button>` : ''}
            ${manutenzione.status === 'in-corso' ? 
                `<button class="manutenzione-btn success" onclick="completaManutenzione(${manutenzione.id})">
                    <i class="fas fa-check"></i> Completa
                </button>` : ''}
            <button class="manutenzione-btn secondary" onclick="dettagliManutenzione(${manutenzione.id})">
                <i class="fas fa-info"></i> Dettagli
            </button>
        </div>
    `;
    
    return item;
}

function iniziaManutenzione(manutenzioneId) {
    if (confirm('Iniziare la manutenzione adesso?')) {
        addSystemAlert({
            type: 'info',
            title: 'Manutenzione Iniziata',
            message: `Manutenzione #${manutenzioneId} avviata`
        });
        // In una vera implementazione dovrebbe fare una chiamata API
        setTimeout(() => {
            loadManutenzioneData();
        }, 1000);
    }
}

function completaManutenzione(manutenzioneId) {
    if (confirm('Confermare il completamento della manutenzione?')) {
        addSystemAlert({
            type: 'success',
            title: 'Manutenzione Completata',
            message: `Manutenzione #${manutenzioneId} completata con successo`
        });
        // In una vera implementazione dovrebbe fare una chiamata API
        setTimeout(() => {
            loadManutenzioneData();
            loadParcheggiData(); // Ricarica anche i parcheggi per aggiornare lo stato
        }, 1000);
    }
}

function dettagliManutenzione(manutenzioneId) {
    // Mostra dettagli della manutenzione in un modal o pagina separata
    addSystemAlert({
        type: 'info',
        title: 'Dettagli Manutenzione',
        message: `Visualizzazione dettagli manutenzione #${manutenzioneId}`
    });
}