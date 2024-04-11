#include "I2Cdev.h"
#include "MPU6050.h"
#include "Wire.h"

// define the calibration offsets obtained from the calibration process
#define ACCEL_X_OFFSET -478
#define ACCEL_Y_OFFSET 709
#define ACCEL_Z_OFFSET 728
#define GYRO_X_OFFSET 9
#define GYRO_Y_OFFSET -121
#define GYRO_Z_OFFSET -36

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
const float scaling_factor = 0.2; 

void setup() {
  Wire.begin();
  Serial.begin(9600);

  mpu.initialize();
  
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

  Serial.println(mapped_value);

  delay(10);
}
