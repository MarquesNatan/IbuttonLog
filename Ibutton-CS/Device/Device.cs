using DalSemi.OneWire.Adapter;
using DalSemi.Utils;
using System;


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
                            ReadPage(0, false, mission, 0);
                            page++;
                        case 1:
                            ReadPage(1, true, mission, 32);
                            page++;
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
            byte[] rawBuffer = new byte[3];
            uint lastCRC = 0;

            MemoryDevice memory = new MemoryDevice();

            rawBuffer[0] = (byte)0xA5;

            int address = page * memory.pageLength + memory.startPhysicalAddress;

            rawBuffer[1] = (byte)(address & 0xFF);
            rawBuffer[2] = (byte)((address >> 8) & 0xFF);

            lastCRC = CRC16.Compute(rawBuffer, 0, rawBuffer.Length, lastCRC);
            Console.WriteLine($"Último CRC: {lastCRC}");

            portAdapter.DataBlock(rawBuffer, 0, 3);

            // @TODO : Receber os bits vindos do ibutton

        }
        public static byte[] WriteDevice(byte[] buffer)
        {
            return buffer;
        } 
    }
}
