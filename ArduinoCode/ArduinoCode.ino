#include "I2Cdev.h"
#include "MPU6050.h"
#include "Wire.h"
#include <LiquidCrystal.h>

// define the calibration offsets obtained from the calibration process
#define ACCEL_X_OFFSET -1446
#define ACCEL_Y_OFFSET -3389
#define ACCEL_Z_OFFSET 1394
#define GYRO_X_OFFSET 126
#define GYRO_Y_OFFSET -92
#define GYRO_Z_OFFSET -23

MPU6050 mpu;

// initialize offsets function
void setOffset() {
  mpu.setXAccelOffset(ACCEL_X_OFFSET);
  mpu.setYAccelOffset(ACCEL_Y_OFFSET);
  mpu.setZAccelOffset(ACCEL_Z_OFFSET);
  mpu.setXGyroOffset(GYRO_X_OFFSET);
  mpu.setYGyroOffset(GYRO_Y_OFFSET);
  mpu.setZGyroOffset(GYRO_Z_OFFSET);
}

const float angle_threshold = 5.0;
const float scaling_factor = 0.1;
const int joystickY = A0;

// LCD pins
const int rs = 8;
const int en = 7;
const int d4 = 6;
const int d5 = 5;
const int d6 = 4;
const int d7 = 3;

// initialize LCD
LiquidCrystal lcd(rs, en, d4, d5, d6, d7);

void setup() {
  Wire.begin();
  Serial.begin(9600);

  mpu.initialize();
  pinMode(joystickY, INPUT);

  lcd.begin(16, 2);
  
  // set the calibration offsets
  setOffset();
}

void loop() {
  int16_t ax, ay, az;
  int16_t gx, gy, gz;
  
  mpu.getMotion6(&ax, &ay, &az, &gx, &gy, &gz);

  // convert raw data to angles
  float accel_angle = atan2(-ay, az) * RAD_TO_DEG;
  float gyro_rate = gx / 131.0; // 131.0 is a sensitivity scale factor obtained from the MPU6050 datasheet
  
  // apply complementary filter
  float dt = 0.01; // time interval between each sensor reading (in seconds)
  float gyro_angle = gyro_rate * dt;
  float filtered_angle = 0.98 * (accel_angle + gyro_angle) + 0.02 * accel_angle;

  // map filtered angle to range [-1, 1]
  float mapped_value = (filtered_angle * scaling_factor) / angle_threshold;

  // ensure mapped value is within [-1, 1] range
  mapped_value = max(-1.0f, min(1.0f, mapped_value));

  // map value between [-1, 1]
  float joyStickYValue = map(analogRead(joystickY) + 4, 0, 1023, 100, -100) / 100.0;
  
  String place = recvSerial();

  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print(place);

  String data = String(mapped_value) + "," + String(joyStickYValue);

  Serial.println(data);

  delay(20);
}

String recvSerial() {
  String place = "GET MOVIN";

  if (Serial.available()) {
    int serialData = Serial.read() - '0'; // convert to int
    
    switch (serialData) {
      case 1:
        place = "1ST";
        break;
      case 2:
        place = "2ND";
        break;
      case 3:
        place = "3RD";
        break;
      case 4:
        place = "4TH";
        break;
    }
  }

  if (place != "GET MOVIN") {
    return "YOU ARE: " + place;
  }

  return place;
}