## Getting Started

To use this code, you'll need to ensure that the required libraries are installed in your Arduino environment. Follow these steps:

1. Copy the two libraries included (or directly move them if you'd prefer that):
    - `MPU6050`
    - `I2Cdev`

2. Paste them into your Arduino's libraries folder. The location of this folder depends on your operating system:
    - **Windows:** `Documents/Arduino/libraries`
    - **Mac:** `Documents/Arduino/libraries`
    - **Linux:** `~/Arduino/libraries`

## Changing MPU6050 Offsets

This is a very important step that needs to be done before your MPU6050 can be used properly.

1. Go to this link: https://github.com/blinkmaker/Improved-MPU6050-calibration/blob/master/MPU6050_offset_calibration_UPDATED.ino

2. Copy the code and upload it to your Arduino

3. Replace the offsets in the ArduinoCode.ino file with the ones you got from thr calibration.