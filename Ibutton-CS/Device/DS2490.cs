using System;
using System.Collections;
using System.IO.Ports;
using System.Runtime.InteropServices.ObjectiveC;

namespace Ibutton_CS.DeviceFunctions {

    internal enum Parameter : byte {
        PARAMETER_SLEW = 16, // 0x10
        PARAMETER_12VPULSE = 32, // 0x20
        PARAMETER_5VPULSE = 48, // 0x30
        PARAMETER_WRITE1LOW = 64, // 0x40
        PARAMETER_SAMPLEOFFSET = 80, // 0x50
        PARAMETER_BAUDRATE = 112, // 0x70
    }

    public class DS2490
    {

        Parameter param;
        ArrayList datalist;

        public void function() {
            datalist.Add((object)(byte)1);

            foreach(var i in datalist) {
                Console.WriteLine(i);
            }
        }
    }
}
