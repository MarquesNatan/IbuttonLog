using DalSemi.OneWire.Adapter;
using DalSemi.Utils;
using System;
using Ibutton_CS.HardwareMap;
using DalSemi.OneWire;
using System.ComponentModel.DataAnnotations;

namespace Ibutton_CS.DeviceFunctions
{
    public class Device
    {
        MemoryRegisterMap memoryMap = new MemoryRegisterMap(32, 2, "Register Mission Backup", 0x0200, false, true, false);
        MemoryRegisterMap logMemoryMap = new MemoryRegisterMap(32, 1960, "Temperature log", 0x01000, false, true, false);


        public byte[] ReadDevice(byte[] buffer, PortAdapter portAdapter)
        {
            Console.WriteLine("\n_____________________ Start ReadDevice _____________________");
            byte[] mission = new byte[64];

            int page = 0;
            int retryCount = 0;

            do
            {
                try
                {
                    switch (page)
                    {
                        case 0:
                            memoryMap.ReadPageCRC(portAdapter, page, false, mission, 0, null);
                            // ReadPageCRC(portAdapter, page, false, mission, 0 null);
                            page++;
                            break;
                        case 1:
                            memoryMap.ReadPageCRC(portAdapter, page, true, mission, 32, null);
                            // ReadPageCRC(portAdapter, page, true, mission, 32, null);
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

            Console.WriteLine("_____________________ End ReadDevice _____________________\n");
            return mission;
        }

        public static void GetLogMemory(PortAdapter portAdapter, int page, bool readContinue, byte[] readBuffer, int offset, byte[] extraInfo)
        {
            // byte[] pagebuffer = new byte[MemoryLogMap.pageLength];
            byte[] rawBuffer = new byte[16];

            rawBuffer[0] = (byte)0x66;
            rawBuffer[1] = (byte)0x0B;
            rawBuffer[2] = (byte)0x44;

            // calculate values of TA1 and TA2
            int address = page * MemoryLogMap.pageLength + MemoryLogMap.startPhysicalAddress;

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


            if (!readContinue) {
                try {
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

            rawBuffer = new byte[MemoryLogMap.pageLength + 3];

            for (int i = 0; i <= rawBuffer.Length - 1; i++) {
                rawBuffer[i] = (byte)0xFF;
            }

            try {

                portAdapter.DataBlock(rawBuffer, 0, rawBuffer.Length);

                if (CRC16.Compute(rawBuffer, 1, rawBuffer.Length - 1, 0) != 0x0000B001) {
                    throw new Exception("Invalid CRC16 read from device, block " + Convert.ToHexString(rawBuffer));
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }

            for(int i = 0; i <= rawBuffer.Length - 1; i++) {
                Console.WriteLine(rawBuffer[i]);
            }
        }

    }
}
