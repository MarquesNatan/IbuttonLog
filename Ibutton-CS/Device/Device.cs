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
                            mission = memoryMap.ReadPageCRC(portAdapter, page, false, mission, 0, null);
                            page++;
                            break;
                        case 1:
                            memoryMap.ReadPageCRC(portAdapter, page, true, mission, 32, null);
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

           try
           {

           }
           catch(Exception e)
           {
                Console.WriteLine(e.Message);
           }

            return mission;
        }
    }
}
