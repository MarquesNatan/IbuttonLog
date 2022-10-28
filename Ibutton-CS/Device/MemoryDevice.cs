using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ibutton_CS.HardwareMap
{
    public class MemoryRegister
    {
        public static int pageLength = 32;
        public static int numberPages = 2;
        public static int size = numberPages * pageLength;
        public static string bankDescription = "Register Mission Backup";
        public static int startPhysicalAddress = 0x0200;
        public static int startPhysicalAddressLogMemory = 0x1000;
        public static bool generalPurposeMemory = false;
        public static bool readOnly = true;
        public static bool readWrite = false;
    }


    public class MemoryLogMap
    {
        public static int pageLength = 32;
        public static int numberPages = 1960;
        public static int size = 1960 * 64;
        public static string bankDescription = "Data log";
        public static int startPhysicalAddress = 0x1000;
        public static bool generalPurposeMemory = false;
        public static bool readOnly             = true;
        public static bool readWrite = false;

    }
}
