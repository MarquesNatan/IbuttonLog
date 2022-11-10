using System;
using System.Data.SqlTypes;
using System.Diagnostics.SymbolStore;
using System.Net;
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
        PortAdapter portAdapter = null;
        byte[] deviceAddress = new byte[8];

        Device myDevice = new Device();
        byte[] newMissionReg = null;
        byte[] newMission = new byte[25 + 7];

        public void myContainer_StopMission()
        {

            try
            {
                portAdapter = AccessProvider.GetAdapter("{DS9490}", "USB1");
                

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

                            portAdapter.SelectDevice(deviceAddress, 0);
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

            int sampleRate = 600;

            byte alarmTempLow = 0x00;           // low Alarm 51°
            byte alarmTempHigh = 0x0A;          // High Alarm 40°

            bool SUTAMode = true;

            byte missionStartDelay = 0x5A;

            for (int i = 0; i < newMission.Length - 1; i++)
            {
                newMission[i] = 0x00;
            }

            try
            {
                // set mission time
                SetTime(true);

                if (sampleRate % 60 == 0x00)
                {
                    sampleRate = (sampleRate / 60) & 0x3FFF;
                    SetSampleRateType(false);
                }
                else
                {
                    SetSampleRateType(true);
                    throw new Exception("Erro, período de leitura inválido");
                }

                SetSampleRate(sampleRate);

                SetAlarms(alarmTempLow, false, alarmTempHigh, true);

                // Enable Clock device
                SetClockRunEnable(true);

                if(SUTAMode)
                {
                    SetStartUponTemperatureAlarmEnable(true);
                }

                // set mission resolution
                SetMissionResolution(0, 0.0625, newMission);

                SetMissionStartDelay(missionStartDelay);

                newMission[25] = 0xFF;
                newMission[26] = 0xFF;
                newMission[27] = 0xFF;
                newMission[28] = 0xFF;

                newMission[29] = 0xFF;
                newMission[30] = 0xFF;
                newMission[31] = 0xFF;
                // newMission[32] = 0xFF;

                StartMission(true);

            }
            catch
            {

            }


        }

        public void WriteDevice(byte[] writeBuffer)
        {
            int startAddress = 0x00;
            bool updateRTC = false;
            int bufferLength = 32 - startAddress;

            if (updateRTC != false)
            {
                startAddress = 0x06;
                bufferLength = 32 - startAddress;
            }


            int offset = startAddress + bufferLength;

            if ((offset & 0x1F) > 0x00)
            {
                // Verificar se a senha está correta

                throw new Exception("The password will be replaced, are you sure?");
            }
            else
            {
                // A senha não será sobreescrita
                WriteScratchpad(writeBuffer, startAddress, offset, bufferLength);

            }

        }

        public void WriteScratchpad(byte[] writeBuffer, int startAddress, int offset, int length)
        {
            if ((startAddress + length) > 32)
            {
                throw new Exception("Write exceeds memory bank end");
            }

            // 9F:00:00:00:10:28:C7:53
            byte[] deviceAddress = new byte[] { 0x53, 0xC7, 0x28, 0x10, 0x00, 0x00, 0x00, 0x9F };

            if (!portAdapter.SelectDevice(deviceAddress, 0))
            {
                throw new Exception("Select devie failed");
            }

            byte[] raw_buff = new byte[32 + 5];

            raw_buff[0] = 0x0F;
            // TA1 e TA2
            raw_buff[1] = (byte)(startAddress & 0xFF);
            raw_buff[2] = (byte)(((0x200 & 0xFFFF) >> 8) & 0xFF);

            Array.Copy(writeBuffer, 0, raw_buff, 3, length);

            for (int i = 0; i < raw_buff.Length - 1; i++)
            {
                Console.Write("{0:X2}-", raw_buff[i]);
            }
            Console.WriteLine();


            if ((startAddress + length) % 32 == 0x00)
            {
                raw_buff[33] = (byte)0xFF;
                raw_buff[34] = (byte)0xFF;
            }

            try
            {
                portAdapter.DataBlock(raw_buff, 0, length + 3 + 2);

                if (CRC16.Compute(raw_buff, 0, length + 5, 0) != 0x0000B001)
                {
                    throw new Exception("Invalid CRC16 read from device, block " + Convert.ToHexString(raw_buff));
                }
                else
                {
                    Console.WriteLine("Device Mission Started!");
                }
                Console.WriteLine("TESTE 3");

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void StartMission(bool missionState)
        {
            if(missionState)
            {
                SetFlag(0x213, 0x01, missionState, newMission);
            }
            else
            {
                SetFlag(0x213, 0x01, false, newMission);
            }

            try {
                byte[] commandPacket = new byte[32 + 5];

                commandPacket[0] = 0x0F;
                commandPacket[1] = 0x00;
                commandPacket[2] = 0x02;

                commandPacket[35] = 0xFF;
                commandPacket[36] = 0xFF;

                Array.Copy(newMission, 0, commandPacket, 3, newMission.Length);

                portAdapter.DataBlock(commandPacket, 0, commandPacket.Length);

                if (CRC16.Compute(commandPacket, 0, 32 + 5, 0) != 0x0000B001) {

                    throw new Exception("Invalid CRC16 read from device, block: " + Convert.ToHexString(commandPacket));
                }

                int result = portAdapter.GetByte();

                ReadScratchpad();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

           
        }

        public byte[] ReadScratchpad()
        {
            byte[] rawbuffer = new byte[32 + 3];

            rawbuffer[0] = 0xCC;
            rawbuffer[1] = 0xAA;

            try
            {
                portAdapter.Reset();
                portAdapter.DataBlock(rawbuffer, 0, 2);

                byte[] result = portAdapter.GetBlock(3);

                if (result[0] != 0x00 || result[1] != 0x02 || result[2] != 0x1F) {
                    throw new Exception("invalid scratchpad memory");
                }

                byte[] scratchpad = portAdapter.GetBlock(32);

                Console.WriteLine();
                for (int i = 0; i <= scratchpad.Length - 1; i++) {
                    if (scratchpad[i] != newMission[i]) {
                        throw new Exception("Bad mission register");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return rawbuffer;
        }

        public void CopyScratchpadToMemory()
        {
            byte[] rawBuffer = new byte[16];

            rawBuffer[0] = 0xCC;
            rawBuffer[1] = 0x66;
            rawBuffer[2] = 0x0C;
            rawBuffer[3] = 0x99;

            // TA1 AND TA2
            rawBuffer[1] = 0x00;
            rawBuffer[1] = 0x02;
            rawBuffer[1] = 0x1F;



            try
            {
                portAdapter.Reset();

            }
            catch(Exception e)
            {

            }

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

        public void SetMissionStartDelay(int startDelay)
        {
            int lowByte = (byte)(startDelay & 0xFF);
            int centerByte = (byte)((startDelay >> 8) & 0xFF);
            int highByte = (byte)((startDelay >> 16) & 0xFF);

            SetFlag(0x216, (byte)lowByte, true, newMission);
            SetFlag(0x217, (byte)centerByte, true, newMission);
            SetFlag(0x218, (byte)highByte, true, newMission);

        }

        public bool isStartUponTemperatureAlarmEnable(byte[] missionRegister)
        {
            return GetFlag(0x213 + 1, 0x20, missionRegister);
        }

        public void SetStartUponTemperatureAlarmEnable(bool sutaValue)
        {
            SetFlag(0x213, 0xC0, sutaValue, newMission);

            //  WriteDevice(newMissionReg);
        }

        public double GetMissionResolution(byte channel, byte[] missionRegister)
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
                if(resolution != 0.0625 && resolution != 0.5)
                {
                    throw new Exception("Invalid mission resolution");
                }
                else
                {
                    SetFlag(0x213, 0x04, resolution == 0.0625 ? true : false, newMission);
                }
            }
            else
            {
                throw new Exception("Invalid Channel");
            }

            // WriteDevice(state);
        }

        public void SetSampleRate(int sampleRate)
        {
            byte sampleRateLow;
            byte sampleRateHigh;

            if(sampleRate <= 0)
            {
                sampleRate = 600;
            }

            sampleRateLow = (byte)(sampleRate & 0xFF);
            sampleRateHigh = (byte)((sampleRate & 0xFF00) >> 0x04);

            SetFlag(0x206, sampleRateLow, true, newMission);
            SetFlag(0x207, sampleRateHigh, true, newMission);
        }

        public void SetSampleRateType(bool sampleRateIsMinutes)
        {
            SetFlag(0x212, 0x02, sampleRateIsMinutes, newMission);
        }

        public void EnableChannels(int channel, bool channelState)
        {
            if(channel == 0x00)
            {
                SetFlag(0x213, 0x01, channelState, newMissionReg);
            }
            else
            {
                throw new Exception("Invalid channel, you did mean: Tempeature Channel?");
            }
        }

        public void SetClockRunEnable (bool runEnable)
        {
            SetFlag(0x212, 0x01, runEnable, newMission);
        }

        public void SetTime(bool time)
        {
            long timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            byte[] timeHex = new byte[4];

            // Timestamp to bytes
            timeHex[0] = (byte)(timestamp & 0xFF);
            timeHex[1] = (byte)((timestamp >> 8) & 0xFF);
            timeHex[2] = (byte)((timestamp >> 16) & 0x00FF);
            timeHex[3] = (byte)((timestamp >> 24) & 0xFF);
            
            for (int i = 0; i <= timeHex.Length - 1; i++)
            {
                SetFlag(0x200 + i, timeHex[i], true, newMission);

                // Console.Write("{0:X2}-",timeHex[i])
            }
        }

        public void SetAlarms(byte tempLow, bool alarmeLowEnable, byte tempHigh, bool alarmeHighEnable)
        {
            int tempAlarm = 2 * tempLow + 82;

            SetFlag(0x208, (byte)tempAlarm, true, newMission);
            
            tempAlarm = 2 * tempHigh + 82; 
            SetFlag(0x209, (byte)tempAlarm, true, newMission);

            EnableAlarm(alarmeLowEnable, alarmeHighEnable);
        }

        public void EnableAlarm(bool alarmLowEnable, bool alarmHighEnable)
        {
            byte alarmBitMask = 0x00;

            if (alarmLowEnable) { alarmBitMask |= 0x01; }
            if (alarmHighEnable) { alarmBitMask |= 0x02; }

            SetFlag(0x210, alarmBitMask, true, newMission);

        }

        public bool GetFlag(int register, byte bitMask, byte[] state)
        {
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
