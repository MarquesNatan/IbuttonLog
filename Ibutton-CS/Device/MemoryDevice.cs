using DalSemi.OneWire.Adapter;
using DalSemi.Utils;
using System.Data;
using System.Globalization;

namespace Ibutton_CS.HardwareMap
{
    public class MemoryRegisterMap
    {
        public  int _pageLength;
        public  int _numberPages;
        public  int _size;
        public  string _bankDescription;
        public  int _startPhysicalAddress;
        public  bool _generalPurposeMemory;
        public  bool _readOnly;
        public  bool _readWrite;

        public MemoryRegisterMap( int pageLength, int numberPages, string bankDescription,
            int startPhysicalAddress, bool generalPurposeMemory, bool readOnly, bool readWrite)
        {
            _pageLength = pageLength;
            _numberPages = numberPages; 
            _size = numberPages * pageLength;
            _bankDescription = bankDescription;
            _startPhysicalAddress = startPhysicalAddress;
            _generalPurposeMemory = generalPurposeMemory;
            _readOnly = readOnly;
            _readWrite = readWrite;
        }


        public void ReadPageCRC(PortAdapter portAdapter, int page, bool readContinue, byte[] mission, int offset, byte[] extraInfo, byte[] deviceAddress)
        {
            Console.WriteLine("******************** Read Memory ******************** ");
            byte[] rawBuffer = new byte[16];

            rawBuffer[0] = (byte)0x66;
            rawBuffer[1] = (byte)0x0B;
            rawBuffer[2] = (byte)0x44;

            // calculate values of TA1 and TA2
            int address = page * _pageLength + _startPhysicalAddress;

            Console.WriteLine($@"página: {page} | readContinue: {readContinue} | offSet: {offset} | address: {address.ToString("X2")}");

            rawBuffer[3] = (byte)(address & 0xFF);
            rawBuffer[4] = (byte)(((address & 0xFFFF) >> 8) & 0xFF);

            rawBuffer[5] = (byte)0x0FF;
            rawBuffer[6] = (byte)0x0FF;
            rawBuffer[7] = (byte)0x0FF;
            rawBuffer[8] = (byte)0x0FF;
            rawBuffer[9] = (byte)0x0FF;
            rawBuffer[10] = (byte)0x0FF;
            rawBuffer[11] = (byte)0x0FF;
            rawBuffer[12] = (byte)0x0FF;

            rawBuffer[13] = (byte)0x0FF;
            rawBuffer[14] = (byte)0x0FF;
            rawBuffer[15] = (byte)0x0FF;


            if (!readContinue)
            {
                try {

                    portAdapter.Reset();
                    portAdapter.SelectDevice(deviceAddress, 0);

                    portAdapter.DataBlock(rawBuffer, 0, 15);

                    if (CRC16.Compute(rawBuffer, 0, 15, 0) != 0x0000B001) {
                        throw new Exception("Invalid CRC16 read from device, block " + Convert.ToHexString(rawBuffer));
                    }

                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }

            portAdapter.StartPowerDelivery(OWPowerStart.CONDITION_AFTER_BYTE);

            portAdapter.GetByte();

            Thread.Sleep(10);
            portAdapter.SetPowerNormal();

            rawBuffer = new byte[_pageLength + 3];

            for (int i = 0; i <= rawBuffer.Length - 1; i++) {
                rawBuffer[i] = (byte)0xFF;
            }

            try {

                portAdapter.DataBlock(rawBuffer, 0, rawBuffer.Length);
                uint value = CRC16.Compute(rawBuffer, 1, rawBuffer.Length - 1, 0);

                if (CRC16.Compute(rawBuffer, 1, rawBuffer.Length - 1, 0) != 0x0000B001) {
                    throw new Exception("Invalid CRC16 read from device, block " + Convert.ToHexString(rawBuffer));
                }

                if(!readContinue)
                {
                    Array.Copy(rawBuffer, 1, mission, 0, 32);
                }else {
                    Array.Copy(rawBuffer, 1, mission, 32, 32);
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }
    }
}
