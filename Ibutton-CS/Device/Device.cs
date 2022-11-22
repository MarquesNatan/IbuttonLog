using DalSemi.OneWire.Adapter;
using DalSemi.Utils;
using System;
using Ibutton_CS.HardwareMap;
using DalSemi.OneWire;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Ibutton_CS.DeviceFunctions
{
    public class Device
    {
        MemoryRegisterMap memoryMap = new MemoryRegisterMap(32, 2, "Register Mission Backup", 0x0200, false, true, false);
        MemoryRegisterMap logMemoryMap = new MemoryRegisterMap(32, 1960, "Temperature log", 0x01000, false, true, false);

        public void ReadDevice(byte[] buffer, PortAdapter portAdapter, byte[] deviceAddress, byte[] memory)
        {
            int page = 0;

            do
            {
                try
                {
                    switch (page)
                    {
                        case 0:
                            memoryMap.ReadPageCRC(portAdapter, page, false, memory, 0, null, deviceAddress);
                            page++;
                            break;
                        case 1:
                            memoryMap.ReadPageCRC(portAdapter, page, true, memory, 32, null, deviceAddress);
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

        }
    }
}
