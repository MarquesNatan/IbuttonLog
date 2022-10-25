using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ibutton_CS.DeviceFunctions
{
    internal class MemoryDevice
    {
        public int pageLength = 32;
        public int numberPages = 2;
        public int size = 2 * 32;
        public string bankDescription = "Register Mission Backup";
        public int startPhysicalAddress = 0x0260;
        public bool generalPurposeMemory = false;
        public bool readOnly = true;
        public bool readWrite = false;
    }
}
