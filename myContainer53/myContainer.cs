using System;
using System.Threading;
using DalSemi.OneWire;
using DalSemi.OneWire.Adapter;
using DalSemi.Utils;

using myContainer53.Device;

namespace Ibutton_CS.Container
{
    public class myContainer
    {
        public static PortAdapter portAdapter = null;

        // @HACK
        public static byte[] deviceAddress = new byte[8] { 0x53, 0xC7, 0x28, 0x10, 0x00, 0x00, 0x00, 0x9F };

        public static Device myDevice = new Device();
        public static byte[] newMissionReg = null;
        public static byte[] newMission = new byte[25 + 7];

        public static void FunctionTest(string address)
        {
            try
            {
                portAdapter = AccessProvider.GetAdapter("{DS9490}", "USB1");

                portAdapter.BeginExclusive(true);

                portAdapter.SetSearchAllDevices();
                portAdapter.TargetAllFamilies();

                portAdapter.Speed = OWSpeed.SPEED_REGULAR;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (portAdapter != null)
                {
                    portAdapter.EndExclusive();
                }
            }
        }

        public static void StartNewMission(int sampleRateInSeconds, byte[] alarmTemp, bool[] alarmEnable, bool SUTA, byte missionStartDelay, double missionResolution)
        {
            StartMissionArray();

            // Set mission time
            SetTime(true);

            if (sampleRateInSeconds % 60 == 0x00)
            {
                sampleRateInSeconds = (sampleRateInSeconds / 60) & 0x3FFF;
                SetSampleRateType(false);
            }
            else
            {
                SetSampleRateType(true);
                throw new Exception("Erro, período de leitura inválido");
            }

            SetSampleRate(sampleRateInSeconds);

            SetAlarms(alarmTemp, alarmEnable);

            // Enable Clock device
            SetClockRunEnable(true);

            if (SUTA)
            {
                SetStartUponTemperatureAlarmEnable(true);
            }

            SetMissionResolution(0, missionResolution, newMission);

            SetMissionStartDelay(0);

            newMission[25] = 0xFF;
            newMission[26] = 0xFF;
            newMission[27] = 0xFF;
            newMission[28] = 0xFF;

            newMission[29] = 0xFF;
            newMission[30] = 0xFF;
            newMission[31] = 0xFF;

            LoadMissionToMemory(newMission);

            StartMission();
        }

        public static void StartMissionArray()
        {
            for (int i = 0; i < newMission.Length - 1; i++)
            {
                newMission[i] = 0x00;
            }
        }

        public static void StopMission()
        {
            
            byte[] command = new byte[20];
            byte releaseByte = 0xFF;

            command[0] = (byte)0x66;
            command[1] = 0x09;
            command[2] = (byte)0xBB;

            // Dummy password
            command[3] = 0xFF;
            command[4] = 0xFF;
            command[5] = 0xFF;
            command[6] = 0xFF;
            command[7] = 0xFF;
            command[8] = 0xFF;
            command[9] = 0xFF;
            command[10] = 0xFF;

            byte result;
            int cnt = 0;
            
            MatchRom(deviceAddress);

            try
            {

                WriteBytes(command);

                byte[] crc = new byte[2];
                crc = ReadBytes(2);

                WriteByte(releaseByte);

                portAdapter.StartPowerDelivery(OWPowerStart.CONDITION_AFTER_BYTE);
                WriteByte(releaseByte);

                Thread.Sleep(6);

                portAdapter.SetPowerNormal();

                int status;
                byte count = 0x00;

                do
                {
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

        public static void StartMission()
        {
            byte[] command = new byte[] { 0x66, 0x09, 0xDD, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            byte[] crc = new byte[2];

            byte releaseByte = 0xFF;

            try
            {

                MatchRom(deviceAddress);

                WriteBytes(command);

                crc = ReadBytes(2);

                WriteByte(releaseByte);

                Thread.Sleep(15);

                WriteByte(releaseByte);

                int status;
                byte count = 0x00;

                do
                {

                    status = portAdapter.GetByte();

                } while ((status != 0xAA) && (status != 0x55) && (count++ < 100));

                if ((status != 0xAA) && (status != 0x55))
                {
                    throw new Exception("Erro durante a operação de cópia. Code " + Convert.ToString(status) + "\n");
                }

                portAdapter.Reset();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static bool isMissionRunning()
        {

            Device device = new Device();

            bool isRunning = true;
            byte[] memoryArray = new byte[64];

            device.ReadDevice(memoryArray, portAdapter, deviceAddress, memoryArray);

            isRunning = GetFlag(0x215, 0b10, memoryArray);

            if (isRunning)
            {
                Console.WriteLine("Uma missão esá rodando");
            }
            else
            {
                Console.WriteLine("Nenhuma missão esá rodando");
            }
            return isRunning;
        }

        public static int GetMissionSampleCount()
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

            Console.WriteLine("missionSamplesLowByte: " + missionSamplesLowByte);
            Console.WriteLine("missionSamplesCenterByte: " + missionSamplesCenterByte);
            Console.WriteLine("missionSamplesHighByte: " + missionSamplesHighByte);

            totalSamples |= missionSamplesLowByte;
            totalSamples |= (missionSamplesCenterByte << 0x16);
            totalSamples |= (missionSamplesHighByte << 0x24);

            return totalSamples;
        }

        public static float GetTemperatureSample(int sampleCount, double resolution) {
            
            float temperature = 0x00;

            float tempHighByte = 0x00;
            float tempLowByte = 0x00;

            int page = 0x00;
            int baseTemp = 0x00;

            Device device = new Device();
            byte[] memoryLogPage = new byte[32];

            // Conversão 16 bits: ϑ(°C) = TRH/2 - 41+ TRL/512
            if (resolution == 0.0625)
            {
                page = (int)((sampleCount * 2) / 32);
                baseTemp = (int)((sampleCount * 3) % 32);

                device.ReadDeviceTemp(page, portAdapter, deviceAddress, memoryLogPage);

                tempHighByte = (float) memoryLogPage[baseTemp] / 2 - 41;
                tempLowByte = (float)memoryLogPage[baseTemp + 1] / 512;

                temperature = tempHighByte + tempLowByte;

            }
            // Conversão 8 bits: ϑ(°C) = TRH/2 - 41
            else
            {
                page =     (int) (sampleCount) / 32;
                baseTemp = (int) (sampleCount % 32);

                device.ReadDeviceTemp(page, portAdapter, deviceAddress, memoryLogPage);

                temperature = (float) memoryLogPage[baseTemp] / 2 - 41;
            }

            Console.WriteLine("Amostra {0} | Página {1} | Temperatura {2}", baseTemp, page, temperature);
            Console.WriteLine();

            return temperature;
        }

        public static  long GetTimestamp(int baseAddress)
        {
            long timestamp = 0x00;
            byte timestampB1 = 0x00;
            byte timestampB2 = 0x00;
            byte timestampB3 = 0x00;
            byte timestampB4 = 0x00;

            Device device = new Device();
            byte[] memoryArray = new byte[64];

            device.ReadDevice(memoryArray, portAdapter, deviceAddress, memoryArray);

            if(baseAddress == 0x219)
            {
                timestampB4 = memoryArray[0x219 & 0x3F];
                timestampB3 = memoryArray[0x21A & 0x3F];
                timestampB2 = memoryArray[0x21B & 0x3F];
                timestampB1 = memoryArray[0x21C & 0x3F];
            }
            else if(baseAddress == 0x200)
            {

                timestampB4 = memoryArray[0x200 & 0x3F];
                timestampB3 = memoryArray[0x201 & 0x3F];
                timestampB2 = memoryArray[0x202 & 0x3F];
                timestampB1 = memoryArray[0x203 & 0x3F];
            }else
            {
                throw new Exception("Invalid memory offset");
            }

            timestamp |= timestampB4;
            timestamp |= (timestampB3 << 8);
            timestamp |= (timestampB2 << 16);
            timestamp |= (timestampB1 << 24);


            return timestamp;
        }

        public static long GetMissionTimestamp() {
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

            timestamp |= timestampB4;
            timestamp |= (timestampB3 << 8);
            timestamp |= (timestampB2 << 16);
            timestamp |= (timestampB1 << 24);


            return timestamp;
        }

        public static  void ClearMemoryLog()
        {
            byte[] commands = new byte[] { 0x66, 0x0A, 0x96, 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            byte releasebyte = 0xFF;

            if(isMissionRunning())
            {
                StopMission();
            }

            MatchRom(deviceAddress);

            try
            {
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void LoadMissionToMemory(byte[] param) {

            MatchRom(deviceAddress);
            byte[] command = new byte[3 + 25 + 7];

            command[0] = 0x0F;
            command[1] = 0x00;
            command[2] = 0x02;

            Array.Copy(param, 0, command, 3, param.Length);

            try {
                WriteBytes(command);

                byte[] crc = new byte[2];
                crc = ReadBytes(2);

                ReadScratchpad();
                CopyScratchpad();
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        public static byte[] ReadScratchpad() {
            byte[] command = new byte[] { 0xAA };
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

            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }

            return null;
        }

        public static void CopyScratchpad() {
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
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        public static void ReadMemoryLog() {

            byte[] memoryLog = new byte[64];
            
        }

        public static bool ResetOneWire(byte[] address) {

            bool AddressIsPresent = false;
            try {
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

        public static void MatchRom(byte[] address) {

            if (ResetOneWire(address)) {
                portAdapter.PutByte(0x55);

                if (address.Length != 0x08) {
                    throw new Exception("Erro no endereço do device");
                }
                else {
                    portAdapter.DataBlock(address, 0, 8);
                }
            }
            else {
                Console.WriteLine("Erro durante o reset, device não reconhecido!");
            }

        }

        public static void WriteBytes(byte[] bytes) {
            for (int i = 0; i <= bytes.Length - 1; i++) {
                portAdapter.PutByte(bytes[i]);
            }
        }

        public static void WriteByte(byte bytes) {

            try {
                portAdapter.PutByte((int)bytes);
            }
            catch (Exception e) {
                Console.WriteLine("Erro durante a escria do byte");
            }
        }

        public static byte[] ReadBytes(byte lenght) {

            byte[] readbytes = new byte[lenght];

            try {
                readbytes = portAdapter.GetBlock(lenght);
                return readbytes;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public static int ReadByte() {
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

        public static void SetMissionStartDelay(int startDelay) {
            int lowByte = (byte)(startDelay & 0xFF);
            int centerByte = (byte)((startDelay >> 8) & 0xFF);
            int highByte = (byte)((startDelay >> 16) & 0xFF);

            SetFlag(0x216, (byte)lowByte, true, newMission);
            SetFlag(0x217, (byte)centerByte, true, newMission);
            SetFlag(0x218, (byte)highByte, true, newMission);

        }

        public static bool isStartUponTemperatureAlarmEnable(byte[] missionRegister) {
            return GetFlag(0x213 + 1, 0x20, missionRegister);
        }

        public static void SetStartUponTemperatureAlarmEnable(bool sutaValue) {
            SetFlag(0x213, 0xC0, sutaValue, newMission);
        }

        public static double GetMissionResolution(byte channel) {
            
            double resolution = 0;

            Device device = new Device();
            byte[] memoryArray = new byte[64];

            device.ReadDevice(memoryArray, portAdapter, deviceAddress, memoryArray);

            if (channel == 0x00)
            {
                bool flag = GetFlag(0x213, 0x04, memoryArray);
                resolution = (flag ? 0.0625 : 0.5);
            }

            return resolution;
        }

        public static void SetMissionResolution(int channel, double resolution, byte[] state) {
            if (state == null) {
                throw new Exception("Invalid mission register, restart program");
            }

            if (channel == 0x00) {
                if (resolution != 0.0625 && resolution != 0.5) {
                    throw new Exception("Invalid mission resolution");
                }
                else {
                    SetFlag(0x213, 0x05, resolution == 0.0625 ? true : false, newMission);
                }
            }
            else {
                throw new Exception("Invalid Channel");
            }
        }

        public static void SetSampleRate(int sampleRate) {
            byte sampleRateLow;
            byte sampleRateHigh;

            if (sampleRate <= 0) {
                sampleRate = 600;
            }

            sampleRateLow = (byte)(sampleRate & 0xFF);
            sampleRateHigh = (byte)((sampleRate & 0xFF00) >> 0x04);

            SetFlag(0x206, sampleRateLow, true, newMission);
            SetFlag(0x207, sampleRateHigh, true, newMission);
        }

        public static int GetMissionSampleRate() {
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

        public static void SetSampleRateType(bool sampleRateIsMinutes) {
            SetFlag(0x212, 0x02, sampleRateIsMinutes, newMission);
        }

        public static void EnableChannels(int channel, bool channelState) {
            if (channel == 0x00) {
                SetFlag(0x213, 0x01, channelState, newMissionReg);
            }
            else {
                throw new Exception("Invalid channel, you did mean: Tempeature Channel?");
            }
        }

        public static void SetClockRunEnable(bool runEnable) {
            SetFlag(0x212, 0x01, runEnable, newMission);
        }

        public static void SetTime(bool time) {
            long timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            byte[] timeHex = new byte[4];

            // Timestamp to bytes
            timeHex[0] = (byte)(timestamp & 0xFF);
            timeHex[1] = (byte)((timestamp >> 8) & 0xFF);
            timeHex[2] = (byte)((timestamp >> 16) & 0x00FF);
            timeHex[3] = (byte)((timestamp >> 24) & 0xFF);

            for (int i = 0; i <= timeHex.Length - 1; i++) {
                SetFlag(0x200 + i, timeHex[i], true, newMission);
            }
        }

        public static void SetAlarms(byte[] tempAlarm, bool[] alarmEnable) {
            int temp = 2 * tempAlarm[0] + 82;

            SetFlag(0x208, (byte)temp, true, newMission);

            temp = 2 * tempAlarm[1] + 82;
            SetFlag(0x209, (byte)temp, true, newMission);

            EnableAlarm(alarmEnable);
        }

        public static void EnableAlarm(bool[] alarmEnable) {
            byte alarmBitMask = 0x00;

            if (alarmEnable[0]) { alarmBitMask |= 0x01; }
            if (alarmEnable[1]) { alarmBitMask |= 0x02; }

            SetFlag(0x210, alarmBitMask, true, newMission);
        }

        public static bool GetFlag(int register, byte bitMask, byte[] state) {
            return ((state[register & 0x3F] & bitMask) != 0x00);
        }

        public static void SetFlag(int register, byte bitMask, bool flagValue, byte[] state) {
            register &= 0x3F;

            byte flags = state[register];

            if (flagValue) {
                flags = (byte)(flags | bitMask);
            }
            else {
                flags = (byte)(flags & ~(bitMask));
            }

            state[register] = flags;
        }
    }
}
