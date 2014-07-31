using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Raspberry.IO.GeneralPurpose;
using WSNUtil;

namespace SensorClient
{
    class PiezoBuzzer : IDisposable
    {
        public ProcessorPin Pin { get; private set; }
        public IGpioConnectionDriver Driver { get; private set; }
        public decimal DurationMsOn { get; private set; }
        public decimal DurationMsOff { get; private set; }
        private bool _Shutdown = false;
        private Semaphore _FinishDisposeSequence = new Semaphore(0,1);

        public PiezoBuzzer(int pin, decimal durationMsOn, decimal durationMsOff)
        {
            Pin = _IntToPin(pin).ToProcessor();
            DurationMsOn = durationMsOn;
            DurationMsOff = durationMsOff;

            Driver = GpioConnectionSettings.DefaultDriver;
            Driver.Allocate(Pin, PinDirection.Output);
        }

        public void Start()
        {
            if (_Shutdown)
                throw new ObjectDisposedException("Object has already been disposed, cannot call Start().");

            Thread T = new Thread(new ThreadStart(_StartWorker));
            T.Start();
        }

        private void _StartWorker()
        {
            while (_Shutdown == false)
            {
                Driver.Write(Pin, true);
                Raspberry.Timers.HighResolutionTimer.Sleep(DurationMsOn);
                Driver.Write(Pin, false);
                Raspberry.Timers.HighResolutionTimer.Sleep(DurationMsOff);
            }
            _FinishDisposeSequence.Release();
        }

        public void Stop()
        {
            _Shutdown = true;
            _FinishDisposeSequence.WaitOne();
        }

        public void Dispose()
        {
            Driver.Release(Pin);
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
    } //End Class
} //End Namespace
