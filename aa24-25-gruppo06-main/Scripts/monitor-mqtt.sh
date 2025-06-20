echo " MQTT Real-time Monitor"

echo " Monitoring all SharingMezzi MQTT topics..."
echo " Press Ctrl+C to stop"
echo ""

# Controlla se mosquitto_sub è disponibile
if ! command -v mosquitto_sub &> /dev/null; then
    echo " mosquitto_sub not found!"
    echo " Install with: sudo apt-get install mosquitto-clients"
    exit 1
fi

# Monitor con timestamp
mosquitto_sub -h localhost -p 1883 -t "parking/+/sensori/#" | while read line; do
    echo "[$(date '+%H:%M:%S')]  SENSOR: $line"
done &

mosquitto_sub -h localhost -p 1883 -t "parking/+/stato_mezzi/#" | while read line; do
    echo "[$(date '+%H:%M:%S')]  COMMAND: $line"  
done &

mosquitto_sub -h localhost -p 1883 -t "mobishare/sistema/#" | while read line; do
    echo "[$(date '+%H:%M:%S')]  SYSTEM: $line"
done &

# Monitor generale per altri messaggi
mosquitto_sub -h localhost -p 1883 -t "parking/+/mezzi" | while read line; do
    echo "[$(date '+%H:%M:%S')]  MEZZI: $line"
done &

# Mantieni monitoring attivo
wait