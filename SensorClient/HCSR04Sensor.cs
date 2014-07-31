using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Raspberry.IO.GeneralPurpose;
using WSNUtil;

namespace SensorClient
{
    class HCSR04Sensor : IDisposable
    {
        public ProcessorPin TriggerPin { get; private set; }
        public ProcessorPin EchoPin { get; private set; }
        public IGpioConnectionDriver Driver { get; private set; }
        public float MaxDistance { get; private set; }
        private static readonly float _TEMPERATURE = Variables.GetSensorClientTemperature(Variables.DefaultINILocation); //In Celcius. Needed for speed of sound calculation
        public static readonly float SPEED_OF_SOUND = 331.3f + 0.606f * _TEMPERATURE; //Measured in meters per second

        public HCSR04Sensor(int echoPin, int triggerPin, float maxDistance)
        {
            TriggerPin = _IntToPin(triggerPin).ToProcessor();
            EchoPin = _IntToPin(echoPin).ToProcessor();
            MaxDistance = maxDistance;

            Driver = GpioConnectionSettings.DefaultDriver;
            Driver.Allocate(TriggerPin, PinDirection.Output);
            Driver.Allocate(EchoPin, PinDirection.Input);
        }

        public float GetMeasurement()
        {
            Driver.Write(TriggerPin, true);
            Raspberry.Timers.HighResolutionTimer.Sleep(0.01m); //Argument is a decimal in MILLIseconds.
            Driver.Write(TriggerPin, false);

            try
            {
                decimal Timeout = (decimal)(2 * (MaxDistance / 100) / SPEED_OF_SOUND);
                Driver.Wait(EchoPin, true, Timeout);
            }
            catch (TimeoutException ex)
            {
                return -1f;
            }
            DateTime StartHigh = DateTime.Now; //Get DateTime first before any computations take up time
            try
            {
                Driver.Wait(EchoPin, false, 30m); //The timeout almost never occurs here when setup correctly, it is only included for very rare circumstances. A timeout of 30 gives a maximum range of roughly 6 meters.
            }
            catch (TimeoutException ex) 
            {
                //Console.WriteLine("Unexpected timeout occured in SensorInterface.GetMeasurement(). High-to-low wait should not timeout. Check that your pins are numbered correctly. This error may rarely occur even if everything is OK.");
                Console.Write("uem: ");
                return -1f;
            }
            DateTime EndHigh = DateTime.Now;

            float Distance = (float)(EndHigh - StartHigh).TotalSeconds * SPEED_OF_SOUND / 2; //Result in meters. Calculations are as per the official HC-SR04 documentation
            Distance *= 100; //Convert to cm
            if (Distance > MaxDistance)
                return -1f;
            return Distance;
        }

        private ConnectorPin _IntToPin(int pinNumber) //Converts pin numbers to corresponding enums as per this image: http://elinux.org/File:GPIOs.png
        {
            switch (pinNumber)
            {
                case 2:
                    return ConnectorPin.P1Pin03;
                case 3:
                    return ConnectorPin.P1Pin05;
                case 4:
                    return ConnectorPin.P1Pin07;
                case 14:
                    return ConnectorPin.P1Pin08;
                case 15:
                    return ConnectorPin.P1Pin10;
                case 17:
                    return ConnectorPin.P1Pin11;
                case 18:
                    return ConnectorPin.P1Pin12;
                case 27:
                    return ConnectorPin.P1Pin13;
                case 22:
                    return ConnectorPin.P1Pin15;
                case 23:
                    return ConnectorPin.P1Pin16;
                case 24:
                    return ConnectorPin.P1Pin18;
                case 10:
                    return ConnectorPin.P1Pin19;
                case 9:
                    return ConnectorPin.P1Pin21;
                case 25:
                    return ConnectorPin.P1Pin22;
                case 11:
                    return ConnectorPin.P1Pin23;
                case 8:
                    return ConnectorPin.P1Pin24;
                case 7:
                    return ConnectorPin.P1Pin26;
                default:
                    throw new ArgumentException("Invalid pin number specified");
            }
        }

        public void Dispose()
        {
            Driver.Release(TriggerPin);
            Driver.Release(EchoPin);
        }
    }
}
