using System;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DalSemi.OneWire;
using DalSemi.OneWire.Adapter;
using DalSemi.Utils;

using Ibutton_CS.DeviceFunctions;

namespace Ibutton_CS.Container
{
    public class myContainer
    {
        PortAdapter portAdapter = null;
        byte[] deviceAddress = new byte[8];

        Device myDevice = new Device();
        byte[] newMissionReg = null;
        byte[] newMission = new byte[25 + 7];

        public void FunctionTest()
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
                            IButton_ClearMemoryLog();
                            StartNewMission();
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

        public void StartNewMission() {

            int sampleRate = 600;

            byte alarmTempLow = 0x00;           // low Alarm 51°
            byte alarmTempHigh = 0x0A;          // High Alarm 40°

            bool SUTAMode = true;

            byte missionStartDelay = 0x00;

            for (int i = 0; i < newMission.Length - 1; i++) {
                newMission[i] = 0x00;
            }

            // set mission time
            SetTime(true);

            if (sampleRate % 60 == 0x00) {
                sampleRate = (sampleRate / 60) & 0x3FFF;
                SetSampleRateType(false);
            }
            else {
                SetSampleRateType(true);
                throw new Exception("Erro, período de leitura inválido");
            }

            SetSampleRate(sampleRate);

            SetAlarms(alarmTempLow, false, alarmTempHigh, true);

            // Enable Clock device
            SetClockRunEnable(true);

            if (SUTAMode) {
                SetStartUponTemperatureAlarmEnable(true);
            }

            SetMissionResolution(0, 0.0625, newMission);

            SetMissionStartDelay(missionStartDelay);

            newMission[25] = 0xFF;
            newMission[26] = 0xFF;
            newMission[27] = 0xFF;
            newMission[28] = 0xFF;

            newMission[29] = 0xFF;
            newMission[30] = 0xFF;
            newMission[31] = 0xFF;

            // byte[] mission = new byte[] { 0x4B, 0x32, 0x39, 0x55, 0x00, 0x00, 0x0A, 0x00, 0x52, 0x66, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x02, 0xFC, 0x01, 0xC5, 0xFF, 0xFF, 0x5A, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            byte[] mission = new byte[] { 0x4B, 0x32, 0x39, 0x55, 0x00, 0x00, 0x0A, 0x00, 0x52, 0x66, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01, 0xC5, 0x00, 0x00, 0x5A, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

            LoadMissionToMemory(newMission);

            StartMission();
        }

        public void StartMission() {
            byte[] command = new byte[] { 0x66, 0x09, 0xDD, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            byte[] crc = new byte[2];

            byte releaseByte = 0xFF;

            try {

                MatchRom(deviceAddress);

                WriteBytes(command);

                crc = ReadBytes(2);
                Console.WriteLine("Start Mission CRC: {0}", Convert.ToHexString(crc));

                WriteByte(releaseByte);

                Thread.Sleep(15);

                WriteByte(releaseByte);

                int status;
                byte count = 0x00;

                do {

                    status = portAdapter.GetByte();

                } while ((status != 0xAA) && (status != 0x55) && (count++ < 100));

                if ((status != 0xAA) && (status != 0x55)) {
                    throw new Exception("Erro durante a operação de cópia. Code " + Convert.ToString(status) + "\n");
                }
                else {
                    Console.WriteLine("Operação de cópia realizada com sucesso! status code: {0:X2}", status);
                }

                portAdapter.Reset();

            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        public void IButton_ClearMemoryLog()
        {
            byte[] commands = new byte[] { 0x66, 0x0A, 0x96, 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            byte releasebyte = 0xFF;

            MatchRom(deviceAddress);

            try {

                Console.Write("Clear memory command: ");
                WriteBytes(commands);

                byte[] crc = new byte[2];
                crc = ReadBytes(2);

                Console.WriteLine("CRC: {0:X2}\n", Convert.ToHexString(crc));

               
                WriteByte(releasebyte);

                portAdapter.StartPowerDelivery(OWPowerStart.CONDITION_AFTER_BYTE);

                WriteByte(releasebyte);

                Thread.Sleep(1500);

                portAdapter.SetPowerNormal();

                int status;
                byte count = 0x00;

                do {

                    status = portAdapter.GetByte();

                } while ((status != 0xAA) && (status != 0x55) && (count++ < 100));

                if ((status != 0xAA) && (status != 0x55)) {
                    throw new Exception("Erro durante a limpeza de mémoria. Code " + Convert.ToString(status) + "\n");
                }
                else {
                    Console.WriteLine("Mémoria de log limpa com sucesso! status code: {0:X2}", status);
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        public void LoadMissionToMemory(byte[] param)
        {
            
            MatchRom(deviceAddress);
            byte[] command = new byte[3 + 25 + 7];
            
            command[0] = 0x0F;
            command[1] = 0x00;
            command[2] = 0x02;

            Array.Copy(param, 0, command, 3, param.Length);

            try
            {
                Console.WriteLine("\n\npacote enviado: {0}\n", Convert.ToHexString(command));
                WriteBytes(command);

                byte[] crc = new byte[2];
                crc = ReadBytes(2);
                Console.WriteLine("CRC: {0}", Convert.ToHexString(crc));

                ReadScratchpad();
                CopyScratchpad();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public byte[] ReadScratchpad()
        {
            byte[] command = new byte[] { 0xAA};
            byte[] address = new byte[3];
            byte[] scratchpadmemory = new byte[32];

            try {
                MatchRom(deviceAddress);

                WriteBytes(command);

                address = ReadBytes(3);

                if (address[0] != 0x00 || address[1] != 0x02 || address[2] != 0x1F) {
                    throw new Exception("Erro - read Scratchpad, resposta não esperada.");
                }
                else {
                    scratchpadmemory = ReadBytes(32);

                    for(int i = 0; i < scratchpadmemory.Length - 1; i++) {
                        Console.Write("{0:X2} ", scratchpadmemory[i]);
                    }

                    return scratchpadmemory;
                }

            }catch(Exception e) {
                Console.WriteLine(e.Message);
            }

            return null;
        }

        public void CopyScratchpad() {
            byte[] command = new byte[] { 0x66, 0x0C, 0X99, 0x00, 0x02, 0x1F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            byte[] crc = new byte[2];

            byte releaseByte = 0xFF;

            try {
                MatchRom(deviceAddress);

                WriteBytes(command);

                crc = ReadBytes(2);
                Console.WriteLine("Copy scaratchpad CRC: {0}", Convert.ToHexString(crc));

                WriteByte(releaseByte);

                Thread.Sleep(15 + 2000);

                WriteByte(releaseByte);

                int status;
                byte count = 0x00;

                do {

                    status = portAdapter.GetByte();

                } while ((status != 0xAA) && (status != 0x55) && (count++ < 100));

                if ((status != 0xAA) && (status != 0x55)) {
                    throw new Exception("Erro durante a operação de cópia. Code " + Convert.ToString(status) + "\n");
                }
                else {
                    Console.WriteLine("Operação de cópia realizada com sucesso! status code: {0:X2}", status);
                }

            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        public bool ResetOneWire(byte[] address) {

            bool AddressIsPresent = false;
            try
            {
                if (address != null) {
                    OWResetResult result = portAdapter.Reset();

                    if ((result == OWResetResult.RESET_PRESENCE) || (result == OWResetResult.RESET_ALARM)) {
                        AddressIsPresent = true;
                    }
                    else {
                        AddressIsPresent = false;
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
                return AddressIsPresent;
        }

        public void MatchRom(byte[] address)
        {

            if (ResetOneWire(address))
            {
                portAdapter.PutByte(0x55);

                if(address.Length != 0x08)
                {
                    throw new Exception("Erro no endereço do divice");
                }
                else
                {
                    portAdapter.DataBlock(address, 0, 8);
                }
            }
            else {
                Console.WriteLine("Erro durante o reset, device não reconhecido!");
            }

        }
       
        public void WriteBytes(byte[] bytes)
        {
            for(int i = 0; i <=  bytes.Length - 1; i++) {
                portAdapter.PutByte(bytes[i]);
                Console.Write("{0:X2} ", bytes[i]);
            }

            Console.WriteLine();
        }

        public void WriteByte(byte bytes) {

            try {
                portAdapter.PutByte((int)bytes);
            }
            catch (Exception e) {
                Console.WriteLine("Erro durante a escria do byte");
            }
        }

        public byte[] ReadBytes(byte lenght) {

            byte[] readbytes = new byte[lenght];

            try {
                readbytes = portAdapter.GetBlock(lenght);
                return readbytes;
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public int ReadByte() {
            int readbytes;

            try {
                readbytes = portAdapter.GetByte();
                return readbytes;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                return 0xFF;
            }
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
                    SetFlag(0x213, 0x05, resolution == 0.0625 ? true : false, newMission);
                }
            }
            else
            {
                throw new Exception("Invalid Channel");
            }
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
