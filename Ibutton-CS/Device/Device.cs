using DalSemi.OneWire.Adapter;
using DalSemi.Utils;
using System;
using Ibutton_CS.HardwareMap;


namespace Ibutton_CS.DeviceFunctions
{
    public static class Device
    {
        public static byte[] ReadDevice(byte[] buffer, PortAdapter portAdapter)
        {
            byte[] mission = new byte[64];
            int page = 0;

            do
            {

                try
                {

                    switch (page)
                    {
                        case 0:
                            ReadPage(portAdapter, 0, false, mission, 0);
                            page++;
                            break;
                        case 1:
                            ReadPage(portAdapter, 1, true, mission, 32);
                            page++;
                            break;
                        default:
                            throw new Exception($"Erro na leitura da página: {page}");
                    }

                }catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            } while (page < 2);


            return buffer;
        }

        private static void ReadPage(PortAdapter portAdapter, int page, bool readContinue, byte[] buffer, int offset)
        {
            byte[] rawBuffer = new byte[16];
            uint lastCRC = 0;

            rawBuffer[0] = (byte)0x66; // XPC_COMMAND
            rawBuffer[1] = (byte)0x0B; //  Length byte
            rawBuffer[2] = (byte)0x44; // XPC_READ_MEMORY_CRC

            // calcular o endereço da leitura 
            int address = page * MemoryDevice.pageLength + MemoryDevice.startPhysicalAddress;

            // Using byte addressing
            rawBuffer[3] = (byte)(address & 0x00FF);
            rawBuffer[4] = (byte)((address >> 8) & 0xFF);

            // dummy password
            rawBuffer[5] = (byte)0xFF;
            rawBuffer[6] = (byte)0xFF;
            rawBuffer[7] = (byte)0xFF;
            rawBuffer[8] = (byte)0xFF;
            rawBuffer[9] = (byte)0xFF;
            rawBuffer[10] = (byte)0xFF;
            rawBuffer[11] = (byte)0xFF;
            rawBuffer[12] = (byte)0xFF;

            // configure position 13 and 14 with 0xFF value
            rawBuffer[13] = (byte)0xFF;
            rawBuffer[14] = (byte)0xFF;

            try {
                // do the first block for xpc command, length, sub command, TA1, TA2, and password
                portAdapter.DataBlock(rawBuffer, 0, 15);

                if (CRC16.Compute(rawBuffer, 0, 15, 0) != 0x0000B001) {
                    // sp.forceVerify();
                    throw new Exception("Invalid CRC16 read from device, block " + Convert.ToString(rawBuffer));
                }

                Console.WriteLine("CRC OK");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            // @TODO : Receber os bits vindos do ibutton

        }

        public static byte[] WriteDevice(byte[] buffer)
        {
            return buffer;
        } 
    }
}
