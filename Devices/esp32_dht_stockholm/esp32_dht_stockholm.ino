#include "includes.h"
#include "config.h"
#include "securities.h"

void setup() {
  initSerial();
  initWifi();   
  initDHT(); 
  registerSensorConn();   
  initEpochTime();
  generateTimeTable();
  
}

void loop() {
  if(connectToHub){
    currentMillis = millis();
    checkWifiStatus();
    sendDhtMessage();
    Esp32MQTTClient_Check();  
    delay(10);   
  }

}
