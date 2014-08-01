**SensorSuite**
===========

This is my implementation of my 2014 Honours research topic at Deakin University, "Tracking moving objects in Sensor Networks". This program aims to use quantity-over-quality inexpensive sensor hardware (HC-SR04 ultrasonic sensors) in combination with inexpensive sensor nodes (Raspberry Pi model B, models A and B+ should function as well but are untested) in order to generate a state estimate of the sensed environment.

This repository contains suite of .NET programs to operate a distributed sensor network, currently designed for HC-SR04 sensors attached to any number of Raspberry Pis. In addition to a class library (WSNUtil), the programs included are:

* SensorClient - Controls a sensor, which acts as a client to the SensorServer. One instance of this program should be run per SENSOR, meaning a RPi that has multiple sensors should have as many instances of the SensorClient executable, each pointing towards their own .INI file (which most importantly here defines the echo and trigger GPIO pin numbers to use). Deploying SensorClient instances can be made more convenient by use of a short bash script noted below.

* SensorServer - Aggregates the measurements from all of the SensorClients, computes a state estimate, and forwards the information to the DisplayServer. This program can be, and often is, run on the same machine as the DisplayServer.

* DisplayServer - Draws the state based on the sensor CSV data as well as the aggregated information from the SensorServer. Unlike the other two applications the DisplayServer provides a GUI by use of WinForms.

**SensorClient Deployment bash script**
===========
    IP="YourSensorClientIP"

    echo -n 'Deleting existing files... '
    sshpass -p YourPassword ssh pi@$IP "rm -r ~/BaselineSolutionSCP"
    echo Done

    echo -n 'Copying in new files... '
    sshpass -p YourPassword scp -r SolutionFilePath pi@$IP:/home/pi/BaselineSolutionSCP
    echo Done

    echo Executing Program...
    sshpass -p YourPassword ssh pi@$IP "cd /home/pi/BaselineSolutionSCP/SensorClient/bin/Debug ; sudo mono ./SensorClient.exe $1"
===========
