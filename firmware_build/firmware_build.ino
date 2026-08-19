#include <Keyboard.h>
#include <EEPROM.h>

// ============================================================
// JOYSTICK AXES
// ============================================================
const int xAxis = A0;
const int yAxis = A1;

const int joystickCenterX = 512;
const int joystickCenterY = 512;
const int threshold = 50;

// ============================================================
// BUTTON PINS
// ============================================================
const int button1 = 2;  // orange
const int button2 = 3;  // purple
const int button3 = 4;  // red
const int button4 = 5;  // green
const int button5 = 6;  // yellow
const int button6 = 7;  // orange

// ============================================================
// KEY MAPPINGS (Loaded from EEPROM)
// ============================================================
char KEY_UP    = 'w';
char KEY_DOWN  = 's';
char KEY_LEFT  = 'a';
char KEY_RIGHT = 'd';

char KEY_BUTTON1 = 'r';
char KEY_BUTTON2 = 't';
char KEY_BUTTON3 = 'f';
char KEY_BUTTON4 = 'c';
char KEY_BUTTON5 = ' ';
char KEY_BUTTON6 = 'g';

int MULTI_CLICK_BTN = 2; // Default to button 2. 0 = none.

const int EEPROM_MAGIC_ADDR = 11;
const byte EEPROM_MAGIC_VAL = 0x43; // Changed to 0x43 since structure changed

// ============================================================
// STATES
// ============================================================
bool prevBtn1 = false, prevBtn2 = false, prevBtn3 = false, prevBtn4 = false, prevBtn5 = false, prevBtn6 = false;
bool joyLeft = false, joyRight = false, joyUp = false, joyDown = false;

// ============================================================
// MULTI-CLICK
// ============================================================
int pressCount = 0;
unsigned long lastPressTime = 0;
const unsigned long MULTI_CLICK_TIMEOUT = 250;

void loadEEPROM() {
  if (EEPROM.read(EEPROM_MAGIC_ADDR) != EEPROM_MAGIC_VAL) {
    // Write defaults
    EEPROM.write(0, KEY_UP);
    EEPROM.write(1, KEY_DOWN);
    EEPROM.write(2, KEY_LEFT);
    EEPROM.write(3, KEY_RIGHT);
    EEPROM.write(4, KEY_BUTTON1);
    EEPROM.write(5, KEY_BUTTON3);
    EEPROM.write(6, KEY_BUTTON4);
    EEPROM.write(7, KEY_BUTTON5);
    EEPROM.write(8, KEY_BUTTON6);
    EEPROM.write(9, KEY_BUTTON2);
    EEPROM.write(10, MULTI_CLICK_BTN);
    EEPROM.write(EEPROM_MAGIC_ADDR, EEPROM_MAGIC_VAL);
  } else {
    // Load from EEPROM
    KEY_UP      = EEPROM.read(0);
    KEY_DOWN    = EEPROM.read(1);
    KEY_LEFT    = EEPROM.read(2);
    KEY_RIGHT   = EEPROM.read(3);
    KEY_BUTTON1 = EEPROM.read(4);
    KEY_BUTTON3 = EEPROM.read(5);
    KEY_BUTTON4 = EEPROM.read(6);
    KEY_BUTTON5 = EEPROM.read(7);
    KEY_BUTTON6 = EEPROM.read(8);
    KEY_BUTTON2 = EEPROM.read(9);
    MULTI_CLICK_BTN = EEPROM.read(10);
  }
}

void setup() {
  Serial.begin(115200);
  Keyboard.begin();
  Keyboard.releaseAll();

  pinMode(button1, INPUT_PULLUP);
  pinMode(button2, INPUT_PULLUP);
  pinMode(button3, INPUT_PULLUP);
  pinMode(button4, INPUT_PULLUP);
  pinMode(button5, INPUT_PULLUP);
  pinMode(button6, INPUT_PULLUP);

  loadEEPROM();
}

void processSerialCommands() {
  if (Serial.available() > 0) {
    String cmd = Serial.readStringUntil('\n');
    cmd.trim();
    if (cmd == "PING") {
      Serial.println("PONG");
    } 
    else if (cmd == "GETMAP") {
      Serial.print("MAP:");
      Serial.print(KEY_UP); Serial.print(",");
      Serial.print(KEY_DOWN); Serial.print(",");
      Serial.print(KEY_LEFT); Serial.print(",");
      Serial.print(KEY_RIGHT); Serial.print(",");
      Serial.print(KEY_BUTTON1); Serial.print(",");
      Serial.print(KEY_BUTTON3); Serial.print(",");
      Serial.print(KEY_BUTTON4); Serial.print(",");
      Serial.print(KEY_BUTTON5); Serial.print(",");
      Serial.print(KEY_BUTTON6); Serial.print(",");
      Serial.print(KEY_BUTTON2); Serial.print(",");
      Serial.print(MULTI_CLICK_BTN);
      Serial.println();
    }
    else if (cmd.startsWith("SETMAP:")) {
      String data = cmd.substring(7);
      if (data.length() >= 21) { // 11 chars + 10 commas = 21 chars
        KEY_UP = data.charAt(0);
        KEY_DOWN = data.charAt(2);
        KEY_LEFT = data.charAt(4);
        KEY_RIGHT = data.charAt(6);
        KEY_BUTTON1 = data.charAt(8);
        KEY_BUTTON3 = data.charAt(10);
        KEY_BUTTON4 = data.charAt(12);
        KEY_BUTTON5 = data.charAt(14);
        KEY_BUTTON6 = data.charAt(16);
        KEY_BUTTON2 = data.charAt(18);
        MULTI_CLICK_BTN = data.charAt(20) - '0';
        
        EEPROM.write(0, KEY_UP);
        EEPROM.write(1, KEY_DOWN);
        EEPROM.write(2, KEY_LEFT);
        EEPROM.write(3, KEY_RIGHT);
        EEPROM.write(4, KEY_BUTTON1);
        EEPROM.write(5, KEY_BUTTON3);
        EEPROM.write(6, KEY_BUTTON4);
        EEPROM.write(7, KEY_BUTTON5);
        EEPROM.write(8, KEY_BUTTON6);
        EEPROM.write(9, KEY_BUTTON2);
        EEPROM.write(10, MULTI_CLICK_BTN);
        Serial.println("OK");
      }
    }
  }
}

void handleButton(bool currentState, bool &prevState, char key, const char* label) {
  if (currentState != prevState) {
    if (currentState) {
      Keyboard.press(key);
      Serial.print("BTN:"); Serial.println(label);
    } else {
      Keyboard.release(key);
    }
    prevState = currentState;
  }
}

void handleMultiClick(bool currentState, bool &prevState, const char* label) {
  if (currentState != prevState) {
    if (currentState) {
      pressCount++;
      lastPressTime = millis();
      Serial.print("BTN:"); Serial.println(label);
    }
    prevState = currentState;
  }
}

void handleAnyButton(bool currentState, bool &prevState, char key, const char* label, int buttonIndex) {
  if (MULTI_CLICK_BTN == buttonIndex) {
    handleMultiClick(currentState, prevState, label);
  } else {
    handleButton(currentState, prevState, key, label);
  }
}

void loop() {
  processSerialCommands();

  int xReading = analogRead(xAxis);
  int yReading = analogRead(yAxis);

  bool b1 = (digitalRead(button1) == LOW);
  bool b2 = (digitalRead(button2) == LOW);
  bool b3 = (digitalRead(button3) == LOW);
  bool b4 = (digitalRead(button4) == LOW);
  bool b5 = (digitalRead(button5) == LOW);
  bool b6 = (digitalRead(button6) == LOW);

  // Joystick Down (x < center)
  if (xReading < joystickCenterX - threshold && !joyDown) {
    Keyboard.press(KEY_DOWN);
    joyDown = true;
    Serial.println("BTN:DOWN");
  } else if (xReading >= joystickCenterX - threshold && joyDown) {
    Keyboard.release(KEY_DOWN);
    joyDown = false;
  }
  // Joystick Up (x > center)
  if (xReading > joystickCenterX + threshold && !joyUp) {
    Keyboard.press(KEY_UP);
    joyUp = true;
    Serial.println("BTN:UP");
  } else if (xReading <= joystickCenterX + threshold && joyUp) {
    Keyboard.release(KEY_UP);
    joyUp = false;
  }
  // Joystick Left (y < center)
  if (yReading < joystickCenterY - threshold && !joyLeft) {
    Keyboard.press(KEY_LEFT);
    joyLeft = true;
    Serial.println("BTN:LEFT");
  } else if (yReading >= joystickCenterY - threshold && joyLeft) {
    Keyboard.release(KEY_LEFT);
    joyLeft = false;
  }
  // Joystick Right (y > center)
  if (yReading > joystickCenterY + threshold && !joyRight) {
    Keyboard.press(KEY_RIGHT);
    joyRight = true;
    Serial.println("BTN:RIGHT");
  } else if (yReading <= joystickCenterY + threshold && joyRight) {
    Keyboard.release(KEY_RIGHT);
    joyRight = false;
  }

  // Handle all buttons dynamically
  handleAnyButton(b1, prevBtn1, KEY_BUTTON1, "1", 1);
  handleAnyButton(b2, prevBtn2, KEY_BUTTON2, "2", 2);
  handleAnyButton(b3, prevBtn3, KEY_BUTTON3, "3", 3);
  handleAnyButton(b4, prevBtn4, KEY_BUTTON4, "4", 4);
  handleAnyButton(b5, prevBtn5, KEY_BUTTON5, "5", 5);
  handleAnyButton(b6, prevBtn6, KEY_BUTTON6, "6", 6);

  if (pressCount > 0 && (millis() - lastPressTime > MULTI_CLICK_TIMEOUT)) {
    int finalCount = pressCount > 10 ? 10 : pressCount;
    Keyboard.print(finalCount);
    pressCount = 0;
  }

  delay(1);
}