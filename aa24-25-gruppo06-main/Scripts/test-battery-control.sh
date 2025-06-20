#!/bin/bash

# Script di test per il controllo batteria nel sistema SharingMezzi
# Testa l'intera sequenza di avvio corsa con controllo batteria MQTT

API_BASE="http://localhost:5000/api"
TOKEN=""
MEZZO_ID_ELETTRICO=3  # Assumo che il mezzo 3 sia elettrico
UTENTE_ID=2  # Mario Rossi ha ID 2 e credito sufficiente

echo "🔋 SharingMezzi - Test Controllo Batteria"
echo "========================================"
echo ""

# Funzione per ottenere il token JWT
get_auth_token() {
    echo "🔐 Ottenimento token di autenticazione..."
    
    RESPONSE=$(curl -s -k -X POST "${API_BASE}/auth/login" \
        -H "Content-Type: application/json" \
        -d '{
            "email": "mario@test.com",
            "password": "user123"
        }')
    
    TOKEN=$(echo $RESPONSE | jq -r '.token // empty')
    
    if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
        echo "❌ Errore nel login: $RESPONSE"
        exit 1
    fi
    
    echo "✅ Token ottenuto con successo"
    echo ""
}

# Test stato batteria
test_battery_status() {
    echo "🔋 Test 1: Controllo stato batteria"
    echo "-----------------------------------"
    
    curl -s -k -X GET "${API_BASE}/battery/all" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" | jq '.'
    
    echo ""
}

# Test controllo batteria specifico
test_specific_battery() {
    echo "🔋 Test 2: Controllo batteria mezzo specifico (ID: $MEZZO_ID_ELETTRICO)"
    echo "------------------------------------------------------------"
    
    curl -s -k -X GET "${API_BASE}/battery/$MEZZO_ID_ELETTRICO" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" | jq '.'
    
    echo ""
}

# Test impostazione batteria bassa
test_set_low_battery() {
    echo "🔧 Test 3: Impostazione batteria bassa (15%) per test"
    echo "---------------------------------------------------"
    
    curl -s -k -X POST "${API_BASE}/battery/$MEZZO_ID_ELETTRICO/set-level" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d '{"level": 15}' | jq '.'
    
    echo ""
}

# Test avvio corsa con batteria bassa
test_ride_start_low_battery() {
    echo "❌ Test 4: Tentativo avvio corsa con batteria bassa (dovrebbe fallire)"
    echo "--------------------------------------------------------------------"
    
    curl -s -k -X POST "${API_BASE}/corse/avvia" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "{
            \"utenteId\": $UTENTE_ID,
            \"mezzoId\": $MEZZO_ID_ELETTRICO
        }" | jq '.'
    
    echo ""
}

# Test impostazione batteria sufficiente
test_set_good_battery() {
    echo "🔧 Test 5: Impostazione batteria sufficiente (80%) per test"
    echo "---------------------------------------------------------"
    
    curl -s -k -X POST "${API_BASE}/battery/$MEZZO_ID_ELETTRICO/set-level" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d '{"level": 80}' | jq '.'
    
    echo ""
}

# Test avvio corsa con batteria sufficiente
test_ride_start_good_battery() {
    echo "✅ Test 6: Avvio corsa con batteria sufficiente (dovrebbe riuscire)"
    echo "-----------------------------------------------------------------"
    
    RIDE_RESPONSE=$(curl -s -k -X POST "${API_BASE}/corse/avvia" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "{
            \"utenteId\": $UTENTE_ID,
            \"mezzoId\": $MEZZO_ID_ELETTRICO
        }")
    
    echo $RIDE_RESPONSE | jq '.'
    
    # Estrai CorsaId per il test successivo
    CORSA_ID=$(echo $RIDE_RESPONSE | jq -r '.corsaId // empty')
    echo ""
}

# Test sequenza completa diagnostica
test_diagnostic_sequence() {
    echo "🧪 Test 7: Test sequenza completa diagnostica"
    echo "---------------------------------------------"
    
    curl -s -k -X POST "${API_BASE}/diagnostics/test-full-sequence/$MEZZO_ID_ELETTRICO?utenteId=$UTENTE_ID" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" | jq '.'
    
    echo ""
}

# Test mezzi con batteria bassa
test_low_battery_vehicles() {
    echo "⚠️  Test 8: Mezzi con batteria bassa"
    echo "-----------------------------------"
    
    curl -s -k -X GET "${API_BASE}/battery/low-battery" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" | jq '.'
    
    echo ""
}

# Test stato MQTT generale
test_mqtt_health() {
    echo "📡 Test 9: Stato generale sistema MQTT"
    echo "--------------------------------------"
    
    curl -s -k -X GET "${API_BASE}/diagnostics/mqtt-health" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" | jq '.'
    
    echo ""
}

# Test terminazione corsa (se esiste)
test_end_ride() {
    if [ ! -z "$CORSA_ID" ] && [ "$CORSA_ID" != "null" ]; then
        echo "🏁 Test 10: Terminazione corsa (ID: $CORSA_ID)"
        echo "---------------------------------------------"
        
        curl -s -k -X PUT "${API_BASE}/corse/$CORSA_ID/termina" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d '{
                "parcheggioDestinazioneId": 1,
                "segnalaManutenzione": false
            }' | jq '.'
        
        echo ""
    fi
}

# Verifica che jq sia installato
if ! command -v jq &> /dev/null; then
    echo "❌ Errore: jq non è installato. Installalo con: brew install jq (macOS) o apt-get install jq (Linux)"
    exit 1
fi

# Esegui tutti i test
echo "🚀 Avvio test controllo batteria..."
echo ""

get_auth_token
test_battery_status
test_specific_battery
test_set_low_battery
test_ride_start_low_battery
test_set_good_battery
test_ride_start_good_battery
test_diagnostic_sequence
test_low_battery_vehicles
test_mqtt_health
test_end_ride

echo "🎉 Test completati!"
echo ""
echo "📋 Comandi curl di esempio per test manuali:"
echo "============================================"
echo ""
echo "# Login e ottenimento token:"
echo "curl -k -X POST '${API_BASE}/auth/login' -H 'Content-Type: application/json' -d '{\"email\":\"mario@test.com\",\"password\":\"user123\"}'"
echo ""
echo "# Controllo stato batteria:"
echo "curl -k -X GET '${API_BASE}/battery/all' -H 'Authorization: Bearer \$TOKEN'"
echo ""
echo "# Avvio corsa con controllo batteria:"
echo "curl -k -X POST '${API_BASE}/corse/avvia' -H 'Authorization: Bearer \$TOKEN' -H 'Content-Type: application/json' -d '{\"utenteId\":1,\"mezzoId\":3}'"
echo ""
echo "# Test sequenza completa:"
echo "curl -k -X POST '${API_BASE}/diagnostics/test-full-sequence/3?utenteId=1' -H 'Authorization: Bearer \$TOKEN'"
echo ""
