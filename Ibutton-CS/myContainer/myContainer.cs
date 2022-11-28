using System;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net.Security;
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

                        if (deviceAddress[0].ToString() == "83") {
                            // IButton_ClearMemoryLog();

                            // IButton_ClearMemoryLog();

                            int sampleRateInSeconds = 600;
                            byte[] alarmTemp = new byte[] { 0x00, 0x0A };
                            bool[] alarmEnable = new bool[] { false, true };
                            bool SUTA = false;
                            byte missionStartDelay = 0x00;

                            // StartNewMission(sampleRateInSeconds: 600, alarmTemp, alarmEnable, SUTA, missionStartDelay);
                            // isMissionRunning();
                            // GetMissionSampleCount();
                            // GetMissionTimestamp();

                            for(int i = 0; i <= 64 - 1; i++)
                            {
                                 GetTempSample(i, 10, 0.0625);
                            }
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

        public void StartNewMission(int sampleRateInSeconds, byte[] alarmTemp, bool[] alarmEnable, bool SUTA, byte missionStartDelay)
        {
            bool SUTAMode = true;

            StartMissionArray();

            // Set mission time
            SetTime(true);

            if (sampleRateInSeconds % 60 == 0x00) {
                sampleRateInSeconds = (sampleRateInSeconds / 60) & 0x3FFF;
                SetSampleRateType(false);
            }
            else {
                SetSampleRateType(true);
                throw new Exception("Erro, período de leitura inválido");
            }

            SetSampleRate(sampleRateInSeconds);

            SetAlarms(alarmTemp, alarmEnable);

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
            byte[] mission = new byte[] {0x4B, 0x32, 0x39, 0x55, 0x00, 0x00, 0x0A, 0x00, 0x52, 0x66, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01, 0xC5, 0x00, 0x00, 0x5A, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};

            LoadMissionToMemory(newMission);

            StartMission();
        }

        public void StartMissionArray()
        {
            for (int i = 0; i < newMission.Length - 1; i++) {
                newMission[i] = 0x00;
            }
        }

        public void StartMission() {
            byte[] command = new byte[] { 0x66, 0x09, 0xDD, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            byte[] crc = new byte[2];

            byte releaseByte = 0xFF;

            try {

                MatchRom(deviceAddress);

                WriteBytes(command);

                crc = ReadBytes(2);

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

                portAdapter.Reset();
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        public bool isMissionRunning() {

            Device device = new Device();

            bool isRunning = true;
            byte[] memoryArray = new byte[64];
            
            device.ReadDevice(memoryArray, portAdapter, deviceAddress, memoryArray);

            isRunning = GetFlag(0x215, 0b10, memoryArray);

            if (isRunning) {
                Console.WriteLine("Uma missão esá rodando");
            }
            else {
                Console.WriteLine("Nenhuma missão esá rodando");
            }
            return isRunning;
        }

        public int GetMissionSampleCount()
        {
            int totalSamples = 0x00;

            int missionSamplesLowByte = 0x00;
            int missionSamplesCenterByte = 0x00;
            int missionSamplesHighByte = 0x00;
            
            Device device = new Device();

            byte[] memoryArray = new byte[64];

            device.ReadDevice(memoryArray, portAdapter, deviceAddress, memoryArray);
           

            missionSamplesLowByte = memoryArray[0x220 & 0x3F];
            missionSamplesCenterByte = memoryArray[0x221 & 0x3F];
            missionSamplesHighByte = memoryArray[0x222 & 0x3F];

            totalSamples |= missionSamplesLowByte;
            totalSamples |= (missionSamplesCenterByte << 8);
            totalSamples |= (missionSamplesHighByte << 16);

            return totalSamples;
        }

        public long GetMissionTimestamp()
        {
            long timestamp = 0x00;
            byte timestampB1 = 0x00;
            byte timestampB2 = 0x00;
            byte timestampB3 = 0x00;
            byte timestampB4 = 0x00;

            Device device = new Device();
            byte[] memoryArray = new byte[64];

            device.ReadDevice(memoryArray, portAdapter, deviceAddress, memoryArray);

            timestampB4 = memoryArray[0x219 & 0x3F];
            timestampB3 = memoryArray[0x21A & 0x3F];
            timestampB2 = memoryArray[0x21B & 0x3F];
            timestampB1 = memoryArray[0x21C & 0x3F];

#if DEBUG
            // Timestamp
            Console.WriteLine("timestampB1: {0:X2}", timestampB1);
            Console.WriteLine("timestampB2: {0:X2}", timestampB2);
            Console.WriteLine("timestampB3: {0:X2}", timestampB3);
            Console.WriteLine("timestampB4: {0:X2}", timestampB4);
#endif
            timestamp |= timestampB4;
            timestamp |= (timestampB3 << 8);
            timestamp |= (timestampB2 << 16);
            timestamp |= (timestampB1 << 24);
         

            return timestamp;
        }

        public void IButton_ClearMemoryLog()
        {
            byte[] commands = new byte[] { 0x66, 0x0A, 0x96, 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            byte releasebyte = 0xFF;

            MatchRom(deviceAddress);

            try {
                WriteBytes(commands);

                byte[] crc = new byte[2];
                crc = ReadBytes(2);
               
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
                WriteBytes(command);

                byte[] crc = new byte[2];
                crc = ReadBytes(2);

                ReadScratchpad();
                CopyScratchpad();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public float GetTempSample(int sampleCount, int sampleRateInMinutes, double resolution) {
            
            float tempLowByte = 0x00; 
            float tempHighByte = 0x00;
            float temp = 0x00;

            int page = (int)((sampleCount * 2) / 32);

            int baseTemp = ((sampleCount * 2) % 32);

            Device device = new Device();
            byte[] memoryLogPage = new byte[32];
            device.ReadDeviceTemp(page, portAdapter, deviceAddress, memoryLogPage);

            // Conversão 16 bits: ϑ(°C) = TRH/2 - 41+ TRL/512
            if (resolution == 0.0625)
            {
                tempHighByte = (float) memoryLogPage[baseTemp] / 2 - 41;
                tempLowByte =  (float) memoryLogPage[baseTemp + 1] / 512;

                temp = tempHighByte + tempLowByte;
                Console.WriteLine("Amostra {0} | Página {1} |  tempHighByte: {3} | tempLowByte: {4} | Temperatura {2}", baseTemp, page, temp, tempHighByte, tempLowByte);
                Console.WriteLine();
            }
            // Conversão 8 bits: ϑ(°C) = TRH/2 - 41
            else
            {

            }

            return temp;

        }

        public double TempConvert(double resolution, int samplesQuantity) {
            
            double temp = 0x00;

            // Conversão de temperatura para 16 bits: ϑ(°C) = TRH/2 - 41+ TRL/512
            if (resolution == 0.0625)
            {

            }
            // Conversão de temperatura para 8 bits: ϑ(°C) = TRH/2 - 41
            else if (resolution == 0.5) {

            }


            return temp;
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
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        public void ReadMemoryLog()
        {
            // Realiza a leitura e conversão das temperaturas lidas pelo ibutton
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
                    throw new Exception("Erro no endereço do device");
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
            }
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
                bool flag = GetFlag(0x213, 0x04, missionRegister);
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

        public int GetSampleRate() {
            int sampleRate = 0x00;

            byte sampleRateLow;
            byte sampleRateHigh;

            Device device = new Device();
            byte[] memoryArray = new byte[64];

            device.ReadDevice(memoryArray, portAdapter, deviceAddress, memoryArray);

            sampleRateLow = memoryArray[0x206 & 0x3F];
            sampleRateHigh = memoryArray[0x207 & 0x3F];

            sampleRate |= sampleRateLow;
            sampleRate |= (sampleRateHigh << 8);

            Console.WriteLine("Sample Rate: " + sampleRate);
            return sampleRate;
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

        public void SetAlarms(byte[] tempAlarm, bool[] alarmEnable)
        {
            int temp = 2 * tempAlarm[0] + 82;

            SetFlag(0x208, (byte)temp, true, newMission);
            
            temp = 2 * tempAlarm[1] + 82; 
            SetFlag(0x209, (byte)temp, true, newMission);

            EnableAlarm(alarmEnable);
        }

        public void EnableAlarm(bool[] alarmEnable)
        {
            byte alarmBitMask = 0x00;

            if (alarmEnable[0]) { alarmBitMask |= 0x01; }
            if (alarmEnable[1]) { alarmBitMask |= 0x02; }

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
