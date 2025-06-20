import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

class SharingMezziNotificationClient {
    constructor(baseUrl = 'https://localhost:5000') {
        this.connection = null;
        this.baseUrl = baseUrl;
        this.userId = null;
        this.isAdmin = false;
        this.subscribedParkings = new Set();
    }

    /**
     * Inizializza connessione SignalR
     */
    async connect(userId, isAdmin = false) {
        this.userId = userId;
        this.isAdmin = isAdmin;

        try {
            this.connection = new HubConnectionBuilder()
                .withUrl(`${this.baseUrl}/notificationHub`, {
                    withCredentials: true
                })
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .configureLogging(LogLevel.Information)
                .build();

            // Setup event handlers
            this.setupEventHandlers();

            // Avvia connessione
            await this.connection.start();
            console.log('Connected to SharingMezzi notifications');

            // Entra nei gruppi appropriati
            if (this.userId) {
                await this.joinUserGroup(this.userId);
            }

            if (this.isAdmin) {
                await this.joinAdminGroup();
            }

            return true;
        } catch (error) {
            console.error(' Error connecting to SignalR:', error);
            return false;
        }
    }

    /**
     * Configura i gestori degli eventi
     */
    setupEventHandlers() {
        if (!this.connection) return;

        // Notifiche corse
        this.connection.on('RideNotification', (notification) => {
            this.handleRideNotification(notification);
        });

        // Notifiche credito
        this.connection.on('CreditNotification', (notification) => {
            this.handleCreditNotification(notification);
        });

        // Alert di sistema (per admin)
        this.connection.on('SystemAlert', (alert) => {
            this.handleSystemAlert(alert);
        });

        // Aggiornamenti parcheggio
        this.connection.on('ParkingUpdate', (update) => {
            this.handleParkingUpdate(update);
        });

        // Stato sistema generale
        this.connection.on('SystemStatus', (status) => {
            this.handleSystemStatus(status);
        });

        // Eventi di connessione
        this.connection.onreconnecting((error) => {
            console.warn(' Reconnecting to notifications...', error);
            this.showConnectionStatus('Riconnessione...', 'warning');
        });

        this.connection.onreconnected((connectionId) => {
            console.log(' Reconnected to notifications:', connectionId);
            this.showConnectionStatus('Connesso', 'success');
        });

        this.connection.onclose((error) => {
            console.error(' Connection closed:', error);
            this.showConnectionStatus('Disconnesso', 'error');
        });
    }

    /**
     * Entra nel gruppo utente
     */
    async joinUserGroup(userId) {
        if (this.connection) {
            await this.connection.invoke('JoinUserGroup', userId);
            console.log(` Joined user group for user ${userId}`);
        }
    }

    /**
     * Entra nel gruppo amministratori
     */
    async joinAdminGroup() {
        if (this.connection) {
            await this.connection.invoke('JoinAdminGroup');
            console.log('👨 Joined admin group');
        }
    }

    /**
     * Monitora un parcheggio specifico
     */
    async subscribeToParkingUpdates(parkingId) {
        if (this.connection && !this.subscribedParkings.has(parkingId)) {
            await this.connection.invoke('JoinParkingGroup', parkingId);
            this.subscribedParkings.add(parkingId);
            console.log(` Subscribed to parking ${parkingId} updates`);
        }
    }

    /**
     * Gestisce notifiche delle corse
     */
    handleRideNotification(notification) {
        console.log('🚴 Ride notification:', notification);

        const message = this.createNotificationElement(
            notification.Type === 'ride_started' ? '🚴‍♂️' : '🏁',
            notification.Message,
            notification.Type === 'ride_started' ? 'success' : 'info'
        );

        this.showNotification(message);

        // Aggiorna UI specifica se necessario
        if (notification.Type === 'ride_ended') {
            this.updateRideHistory(notification);
            this.updateUserBalance(notification.Cost);
        }
    }

    /**
     * Gestisce notifiche di credito
     */
    handleCreditNotification(notification) {
        console.log('💰 Credit notification:', notification);

        const icon = notification.Type === 'credit_recharged' ? '💰' : '⚠️';
        const type = notification.Type === 'credit_recharged' ? 'success' : 'warning';

        const message = this.createNotificationElement(
            icon,
            notification.Message,
            type
        );

        this.showNotification(message);

        // Aggiorna saldo in UI
        if (notification.NewBalance !== undefined) {
            this.updateUserBalance(notification.NewBalance);
        }
    }

    /**
     * Gestisce alert di sistema (solo admin)
     */
    handleSystemAlert(alert) {
        if (!this.isAdmin) return;

        console.log(' System alert:', alert);

        const urgencyIcons = {
            low: '💙',
            normal: '💚',
            medium: '💛',
            high: '🧡',
            critical: '🔴'
        };

        const message = this.createNotificationElement(
            urgencyIcons[alert.Priority] || '🔔',
            alert.Message,
            alert.Priority === 'critical' ? 'error' : 'warning'
        );

        this.showNotification(message, true); // Persistent per admin

        // Aggiorna dashboard admin
        this.updateAdminDashboard(alert);
    }

    /**
     * Gestisce aggiornamenti parcheggio
     */
    handleParkingUpdate(update) {
        console.log('🅿️ Parking update:', update);

        if (update.Type === 'parking_status_update') {
            this.updateParkingDisplay(update.ParkingId, update.PostiLiberi, update.PostiOccupati);
        }

        if (update.Type === 'low_battery' || update.Type === 'vehicle_error') {
            this.updateVehicleStatus(update.MezzoId, update.Type);
        }
    }

    /**
     * Gestisce stato sistema generale
     */
    handleSystemStatus(status) {
        console.log(' System status:', status);
        this.updateSystemStatusDisplay(status);
    }

    /**
     * Crea elemento notifica HTML
     */
    createNotificationElement(icon, message, type = 'info') {
        const colors = {
            success: '#10B981',
            info: '#3B82F6',
            warning: '#F59E0B',
            error: '#EF4444'
        };

        return `
            <div class="notification notification-${type}" style="
                background: ${colors[type]}15;
                border: 1px solid ${colors[type]}40;
                border-radius: 8px;
                padding: 12px 16px;
                margin: 8px 0;
                display: flex;
                align-items: center;
                gap: 12px;
            ">
                <span style="font-size: 20px;">${icon}</span>
                <span style="color: ${colors[type]}; font-weight: 500;">${message}</span>
                <button onclick="this.parentElement.remove()" style="
                    margin-left: auto;
                    background: none;
                    border: none;
                    font-size: 18px;
                    cursor: pointer;
                    color: ${colors[type]};
                ">×</button>
            </div>
        `;
    }

    /**
     * Mostra notifica nell'UI
     */
    showNotification(html, persistent = false) {
        let container = document.getElementById('notifications-container');
        
        if (!container) {
            container = document.createElement('div');
            container.id = 'notifications-container';
            container.style.cssText = `
                position: fixed;
                top: 20px;
                right: 20px;
                z-index: 10000;
                max-width: 400px;
            `;
            document.body.appendChild(container);
        }

        const notification = document.createElement('div');
        notification.innerHTML = html;
        container.appendChild(notification);

        // Auto-remove dopo 5 secondi se non persistente
        if (!persistent) {
            setTimeout(() => {
                if (notification.parentElement) {
                    notification.remove();
                }
            }, 5000);
        }
    }

    /**
     * Mostra stato connessione
     */
    showConnectionStatus(status, type) {
        const statusElement = document.getElementById('connection-status');
        if (statusElement) {
            statusElement.textContent = status;
            statusElement.className = `status status-${type}`;
        }
    }

    /**
     * Aggiorna display parcheggio
     */
    updateParkingDisplay(parkingId, postiLiberi, postiOccupati) {
        const parkingElement = document.querySelector(`[data-parking="${parkingId}"]`);
        if (parkingElement) {
            const freeSpots = parkingElement.querySelector('.free-spots');
            const occupiedSpots = parkingElement.querySelector('.occupied-spots');
            
            if (freeSpots) freeSpots.textContent = postiLiberi;
            if (occupiedSpots) occupiedSpots.textContent = postiOccupati;
            
            // Cambia colore basato su disponibilità
            parkingElement.classList.toggle('low-availability', postiLiberi <= 2);
        }
    }

    /**
     * Aggiorna saldo utente
     */
    updateUserBalance(newBalance) {
        const balanceElement = document.getElementById('user-balance');
        if (balanceElement) {
            balanceElement.textContent = `€${newBalance.toFixed(2)}`;
        }
    }

    /**
     * Disconnette dal hub
     */
    async disconnect() {
        if (this.connection) {
            await this.connection.stop();
            console.log(' Disconnected from notifications');
        }
    }
}

// Esporta per uso globale
window.SharingMezziNotifications = SharingMezziNotificationClient;
