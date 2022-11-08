using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using DalSemi.OneWire;
using DalSemi.OneWire.Adapter;
using DalSemi.Utils;

using Ibutton_CS.DeviceFunctions;
using Ibutton_CS.HardwareMap;

namespace Ibutton_CS.Container
{
    public class myContainer
    {

        Device myDevice = new Device();
        byte[] newMissionReg = null;

        public void myContainer_StopMission()
        {
            PortAdapter portAdapter = null;

            try
            {
                portAdapter = AccessProvider.GetAdapter("{DS9490}", "USB1");
                byte[] deviceAddress = new byte[8];

                portAdapter.BeginExclusive(true);

                portAdapter.SetSearchAllDevices();
                portAdapter.TargetAllFamilies();

                portAdapter.Speed = OWSpeed.SPEED_REGULAR;
                
                // Get Device Address
                if(portAdapter.GetFirstDevice(deviceAddress, 0))
                {
                    int deviceIndex = 0;
                    do
                    {
                        Console.WriteLine($"DEVICE INDEX: {deviceIndex}");
                        deviceIndex++;
                         
                        for(int i = deviceAddress.Length - 1; i >= 0; i--)
                        {
                            if(i != deviceAddress.Length - 1)
                            {

                                Console.Write(":");
                            }

                            Console.Write("{0:X2}", deviceAddress[i]);
                        };

                        Console.WriteLine();

                        if (deviceAddress[0].ToString() == "83")
                        {
                            // StopMission(portAdapter);
                            // ClearMemoryLog(portAdapter);
                            StartNewMission(portAdapter);
                            // ReadResultPage(portAdapter, 0);
                        }

                    } while (portAdapter.GetNextDevice(deviceAddress, 0));

                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if(portAdapter != null)
                {
                    portAdapter.EndExclusive();
                }
            }
        }

        public void ClearMemoryLog(PortAdapter portAdapter)
        {
            byte[] buffer = new byte[20];
            Console.WriteLine();
            Console.WriteLine("_____________________ Start ClearMemory _____________________");

            buffer[0] = 0x66;
            buffer[1] = 0x0A;
            buffer[2] = 0x96;
            buffer[3] = 0x01;
            
            // dummy password
            buffer[4] = 0xFF;
            buffer[5] = 0xFF;
            buffer[6] = 0xFF;
            buffer[7] = 0xFF;
            buffer[8] = 0xFF;
            buffer[9] = 0xFF;
            buffer[10] = 0xFF;
            buffer[11] = 0xFF;

            // release byte
            buffer[12] = 0xFF;
            buffer[13] = 0xFF;

            try
            {
                portAdapter.DataBlock(buffer, 0, 14);
                if (CRC16.Compute(buffer, 0, 14, 0) != 0x0000B001)
                {
                    // throw new Exception("Invalid CRC16 read from device.");
                    throw new Exception("Invalid CRC16 read from device, block " + Convert.ToHexString(buffer));
                }
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

            portAdapter.StartPowerDelivery(OWPowerStart.CONDITION_AFTER_BYTE);
            int result = portAdapter.GetByte();

            Thread.Sleep(1500);

            portAdapter.SetPowerNormal();
            int cnt = 0;

            // Read result byte
            do
            {
                result = (byte)portAdapter.GetByte();

            } while (result != 0xAA && result != 0x55 && (cnt++ < 50));

            if ((result != 0xAA) && (result != 0x55))
            {
                Console.WriteLine($"result: {result}");
                throw new Exception(
                   "OneWireContainer53-Clear Memory failed. Return Code " + Convert.ToString((byte)result));
            }
            else{
                Console.WriteLine("Resultado: {0:X2}", result);
                Console.WriteLine("Log de Missão limpo com sucesso!");
            }

            Console.WriteLine("_____________________ End ClearMemory _____________________");
            Console.WriteLine();
        }

        public void StartNewMission(PortAdapter portAdapter)
        {

            try
            {
                // stop previous mission
                // StopMission(portAdapter);

                newMissionReg = myDevice.ReadDevice(newMissionReg, portAdapter);
                bool SUTA = isStartUponTemperatureAlarmEnable(newMissionReg);

                Console.WriteLine($"SUTA IS: " +  (SUTA ? "ENABLE" : "DISABLE"));

                double missionResolution = GetMissionResolution(0, newMissionReg);

                Console.WriteLine($"RESOLUTION IS: {missionResolution}");

                for (int i = 0; i < newMissionReg.Length - 1; i++)
                {
                    Console.Write("{0:X2}-", newMissionReg[i]);
                };

                // Clear memory does not preserver Mission Control Register (0x0213)
                // ClearMemoryLog(portAdapter);

                for(int i = 0; i < newMissionReg.Length - 1; i++)
                {
                    newMissionReg[i] = 0x00;
                }

                if(SUTA)
                {
                    setStartUponTemperatureAlarmEnable(true);
                }

                SetMissionResolution(channel: 0, resolution: 0.0625, newMissionReg);

                SetClockRunEnable(true);

                Console.WriteLine();
                for(int i = 0; i < newMissionReg.Length - 1; i++)
                {
                    Console.Write("{0:X2}-", newMissionReg[i]);
                }

            }
            catch
            {

            }
        }

        public void StartMission()
        {

        }

        public void StopMission(PortAdapter portAdapter)
        {
            byte[] buffer = new byte[20];
            buffer[0] = (byte)0x66;
            buffer[1] = 0x09;
            buffer[2] = (byte)0xBB;

            // Dummy password
            buffer[3] = 0xFF;
            buffer[4] = 0xFF;
            buffer[5] = 0xFF;
            buffer[6] = 0xFF;
            buffer[7] = 0xFF;
            buffer[8] = 0xFF;
            buffer[9] = 0xFF;
            buffer[10] = 0xFF;

            // Release bytes
            buffer[11] = (byte)0xFF;
            buffer[12] = (byte)0xFF;

            byte result;
            int cnt = 0;

            try
            {
                Console.WriteLine(@"
                                ***************************
                                *      PARANDO MISSÃO     *
                                ***************************");

                portAdapter.DataBlock(buffer, 0, 13);

                // Compute CRC and verify it is correct
                if (CRC16.Compute(buffer, 0, 13, 0) != 0x0000B001)
                {
                    throw new Exception("Invalid CRC16 read from device.");
                    
                    
                }

                portAdapter.StartPowerDelivery(OWPowerStart.CONDITION_AFTER_BYTE);
                portAdapter.GetByte();

                Thread.Sleep(6);

                do
                {
                    result = (byte)portAdapter.GetByte();
                }
                while ((result != (byte)0xAA) && (result != (byte)0x55) && (cnt++ < 50));

                if ((result != (byte)0xAA) && (result != (byte)0x55))
                {
                    throw new Exception(
                       "OneWireContainer53-XPC Stop Mission failed. Return Code " + Convert.ToString((byte)result));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine(@"
                                **************************************
                                *      MISSÃO PARADA COM SUCESSO     *
                                **************************************");
        }

        public bool isStartUponTemperatureAlarmEnable(byte[] missionRegister)
        {
            return GetFlag(0x213 + 1, 0x20, missionRegister);
        }

        public void setStartUponTemperatureAlarmEnable(bool sutaValue)
        {
            SetFlag(0x213, 0x20, sutaValue, newMissionReg);

            WriteDevice(newMissionReg);
        }

        public double  GetMissionResolution(byte channel, byte[] missionRegister)
        {
            double resolution = 0;

            if(channel == 0x00)
            {
                bool flag = GetFlag(0x213 + 1, 0x04, missionRegister);
                resolution =  (flag ? 0.0625 : 0.5);
            }

            return resolution;
        }

        public void SetMissionResolution(int channel, double resolution, byte[] state)
        {
            if(state == null)
            {
                throw new Exception("Invalid mission register, restart program");
            }

            if(channel == 0x00)
            {
                SetFlag(0x213, 0x04, resolution == 0.0625 ? true : false, state);
            }
            else
            {
                throw new Exception("Invalid Channel");
            }

            // WriteDevice(state);
        }

        public void SetSampleRate(int sampleRate)
        {

        }

        public void EnableChannels(byte[] channels)
        {

        }

        public void SetClockRunEnable (bool runEnable)
        {
            SetFlag(0x212, 0x01, runEnable, newMissionReg);
        }

        public byte[] WriteDevice(byte[] state)
        {
            byte[] newState = new byte[64];

            return newState;
        }

        public bool GetFlag(int register, byte bitMask, byte[] state)
        {
            Console.WriteLine($"GetFlag: {state[register & 0x3F]}");
            return ((state[register & 0x3F] & bitMask) != 0x00);
        }

        public void SetFlag(int register, byte bitMask, bool flagValue, byte[] state)
        {
            register &= 0x3F;

            byte flags = state[register];

            if(flagValue)
            {
                flags = (byte)(flags | bitMask);
            }
            else
            {
                flags = (byte)(flags & ~(bitMask));
            }

            state[register] = flags;
        }
    }
}
