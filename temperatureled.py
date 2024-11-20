from flask import Flask, jsonify
import Adafruit_DHT
import RPi.GPIO as GPIO
import threading
import time

# Vypnutí varování GPIO
GPIO.setwarnings(False)

# Nastavení režimu číslování pinů
GPIO.setmode(GPIO.BCM)

# Konstanty
DHT_SENSOR = Adafruit_DHT.DHT11
DHT_PIN = 26  # GPIO26 pro teploměr
GREEN_LED_PIN = 22  # GPIO22 pro zelenou LED
RED_LED_PIN = 24  # GPIO24 pro červenou LED
COLOR_LED_PIN = 18  # GPIO18 pro barevnou LED
TEMPERATURE_THRESHOLD = 21  # Prahová teplota
HUMIDITY_THRESHOLD = 65  # Prahová vlhkost

# Inicializace GPIO
GPIO.setup(GREEN_LED_PIN, GPIO.OUT)
GPIO.setup(RED_LED_PIN, GPIO.OUT)
GPIO.setup(COLOR_LED_PIN, GPIO.OUT)

# Globální proměnné
current_temperature = None
current_humidity = None
led_status = {"green": "vypnuto", "red": "vypnuto", "color": "vypnuto"}  # Stav LED

# Flask aplikace
app = Flask(__name__)

# Funkce pro čtení teploty a vlhkosti
def read_temp_humidity():
    global current_temperature, current_humidity, led_status
    while True:
        humidity, temperature = Adafruit_DHT.read(DHT_SENSOR, DHT_PIN)
        if humidity is not None and temperature is not None:
            current_temperature = temperature
            current_humidity = humidity
            print(f"Teplota: {temperature:.1f} °C, Vlhkost: {humidity:.1f} %")

            # Logika pro COLOR_LED_PIN
            if temperature <= 19:
                GPIO.output(COLOR_LED_PIN, GPIO.HIGH)
                led_status["color"] = "zapnuto"
            else:
                GPIO.output(COLOR_LED_PIN, GPIO.LOW)
                led_status["color"] = "vypnuto"

            # Logika pro zelenou a červenou LED
            if temperature == TEMPERATURE_THRESHOLD:
                led_status["green"] = "vypnuto"
                led_status["red"] = "zapnuto"
            elif temperature < TEMPERATURE_THRESHOLD:
                led_status["green"] = "zapnuto"
                led_status["red"] = "vypnuto"
            else:
                led_status["green"] = "vypnuto"
                led_status["red"] = "vypnuto"

            # Logika pro vlhkost
            if humidity > HUMIDITY_THRESHOLD:
                if led_status["green"] == "zapnuto":
                    GPIO.output(GREEN_LED_PIN, GPIO.HIGH)
                    time.sleep(0.5)
                    GPIO.output(GREEN_LED_PIN, GPIO.LOW)
                    time.sleep(0.5)
                elif led_status["red"] == "zapnuto":
                    GPIO.output(RED_LED_PIN, GPIO.HIGH)
                    time.sleep(0.5)
                    GPIO.output(RED_LED_PIN, GPIO.LOW)
                    time.sleep(0.5)
            else:
                # Normální režim, pokud je vlhkost <= 62
                if led_status["green"] == "zapnuto":
                    GPIO.output(GREEN_LED_PIN, GPIO.HIGH)
                    GPIO.output(RED_LED_PIN, GPIO.LOW)
                elif led_status["red"] == "zapnuto":
                    GPIO.output(GREEN_LED_PIN, GPIO.LOW)
                    GPIO.output(RED_LED_PIN, GPIO.HIGH)
                else:
                    GPIO.output(GREEN_LED_PIN, GPIO.LOW)
                    GPIO.output(RED_LED_PIN, GPIO.LOW)
        else:
            print("Chyba při čtení ze senzoru!")
        time.sleep(2)  # Interval čtení

# Endpoint pro získání teploty, vlhkosti a stavu LED
@app.route('/data', methods=['GET'])
def get_data():
    if current_temperature is not None and current_humidity is not None:
        return jsonify({
            "temperature": current_temperature,
            "humidity": current_humidity,
            "led_status": led_status,
            "explanation": (
                "Červená LED svítí, pokud je teplota 22 °C. "
                "Zelená LED svítí, pokud je teplota 21 °C nebo nižší. "
                "Color LED svítí, pokud je teplota 20 °C nebo vyšší. "
                "Pokud je vlhkost nad 62 %, LED bliká přerušovaně, pokud svítí."
            )
        })
    else:
        return jsonify({"error": "Chyba při čtení ze senzoru!"}), 500

# Hlavní část programu
if __name__ == '__main__':
    try:
        # Spuštění čtení senzoru v samostatném vlákně
        sensor_thread = threading.Thread(target=read_temp_humidity)
        sensor_thread.daemon = True
        sensor_thread.start()

        # Spuštění Flask serveru na jiném portu
        app.run(host='0.0.0.0', port=5001)
    except KeyboardInterrupt:
        print("Program ukončen uživatelem")
    finally:
        GPIO.output(GREEN_LED_PIN, GPIO.LOW)
        GPIO.output(RED_LED_PIN, GPIO.LOW)
        GPIO.output(COLOR_LED_PIN, GPIO.LOW)
        GPIO.cleanup()
        print("Všechny LED byly vypnuty a GPIO piny uvolněny.")
