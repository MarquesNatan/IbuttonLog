using System;

using DalSemi.OneWire;
using DalSemi.OneWire.Adapter;
using DalSemi.Utils;

using Ibutton_CS.DeviceFunctions;


namespace Ibutton_CS.Container
{
    public class myContainer
    {
        public void myContainer_StopMission()
        {
            PortAdapter portAdapter = null;

            try
            {
                portAdapter = AccessProvider.GetAdapter("{DS9490}", "USB1");
                byte[] deviceAddress = new byte[8];

                portAdapter.BeginExclusive(true);

                portAdapter.SetSearchAllDevices();
                portAdapter.TargetAllFamilies();

                portAdapter.Speed = OWSpeed.SPEED_REGULAR;
                
                // Get Device Address
                if(portAdapter.GetFirstDevice(deviceAddress, 0))
                {
                    int deviceIndex = 0;
                    do
                    {
                        Console.WriteLine($"DEVICE INDEX: {deviceIndex}");
                        deviceIndex++;
                         
                        for(int i = deviceAddress.Length - 1; i >= 0; i--)
                        {
                            if(i != deviceAddress.Length - 1)
                            {

                                Console.Write(":");
                            }

                            Console.Write("{0:X2}", deviceAddress[i]);
                        };

                        Console.WriteLine();
                        if (deviceAddress[0].ToString() == "83")
                        {
                            // ClearMemoryLog(portAdapter);
                            // StopMission(portAdapter);
                            StartNewMission(portAdapter);
                        }

                    } while (portAdapter.GetNextDevice(deviceAddress, 0));

                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if(portAdapter != null)
                {
                    portAdapter.EndExclusive();
                }
            }
        }

        public void ClearMemoryLog(PortAdapter portAdapter)
        {
            byte[] buffer = new byte[20];
            Console.WriteLine("Enviando Comandos de dentro da função SendCommands");

            buffer[0] = 0x66;
            buffer[1] = 0x0A;
            buffer[2] = 0x96;
            buffer[3] = 0x01;
            
            // dummy password
            buffer[4] = 0xFF;
            buffer[5] = 0xFF;
            buffer[6] = 0xFF;
            buffer[7] = 0xFF;
            buffer[8] = 0xFF;
            buffer[9] = 0xFF;
            buffer[10] = 0xFF;
            buffer[11] = 0xFF;

            // release byte
            buffer[12] = 0xFF;
            buffer[13] = 0xFF;

            portAdapter.DataBlock(buffer, 0, 14);

            if(CRC16.Compute(buffer, 0, 14, 0) != 0x0000B001)
            {
                throw new Exception("Invalid CRC16 read from device.");
            }

            portAdapter.StartPowerDelivery(OWPowerStart.CONDITION_AFTER_BYTE);
            int result = portAdapter.GetByte();

            Thread.Sleep(1500);

            portAdapter.SetPowerNormal();
            int cnt = 0;

            // Read result byte
            do
            {
                result = (byte)portAdapter.GetByte();

            } while (result != 0xAA && result != 0x55 && (cnt++ < 50));

            if ((result != 0xAA) && (result != 0x55))
            {
                Console.WriteLine($"result: {result}");
                throw new Exception(
                   "OneWireContainer53-Clear Memory failed. Return Code " + Convert.ToString((byte)result));
            }
            else{
                Console.WriteLine("Resultado: {0:X2}", result);
                Console.WriteLine("Log de Missão limpo com sucesso!");
            }
        }

        public void StartNewMission(PortAdapter portAdapter)
        {
            byte[] newMissionReg = new byte[32];

            int sampleRate;
            int missionDelay;

            bool rolloverEnabled;
            bool syncClock;
            bool[] channelEnable;

            // carregar os novos valores do registrador
            // limpar a memoria de log
            // configurar os registradores
            // OPICIONAL - Ler os registradores
            // Carrega os valores do scratchpad para o registrador
            // Inicia uma nova missão


            try
            {
                // Limpa a memória 
                ClearMemoryLog(portAdapter);

                /** 
                 * 0F 00 02 4B 32 39 55 00
                 *  00 0A 00 52 66 00 FF FF
                 *  FF FF FF 02 FC 01 C5 FF
                 *  FF 5A 00 00 FF FF FF FF 
                 *  FF FF FF
                 */

                newMissionReg = DeviceFunctions.Device.ReadDevice(newMissionReg, portAdapter);

            }
            catch
            {

            }
        }

        public void StartMission(PortAdapter portAdapter)
        {

        }

        public void StopMission(PortAdapter portAdapter)
        {
            byte[] buffer = new byte[20];
            buffer[0] = (byte)0x66;
            buffer[1] = 0x09;
            buffer[2] = (byte)0xBB;

            // Dummy password
            buffer[3] = 0xFF;
            buffer[4] = 0xFF;
            buffer[5] = 0xFF;
            buffer[6] = 0xFF;
            buffer[7] = 0xFF;
            buffer[8] = 0xFF;
            buffer[9] = 0xFF;
            buffer[10] = 0xFF;

            // Release bytes
            buffer[11] = (byte)0xFF;
            buffer[12] = (byte)0xFF;

            byte result;
            int cnt = 0;

            try
            {
                Console.WriteLine(@"
                                ***************************
                                *      PARANDO MISSÃO     *
                                ***************************");

                portAdapter.DataBlock(buffer, 0, 13);

                // Compute CRC and verify it is correct
                if (CRC16.Compute(buffer, 0, 13, 0) != 0x0000B001)
                {
                    throw new Exception("Invalid CRC16 read from device.");
                }

                portAdapter.StartPowerDelivery(OWPowerStart.CONDITION_AFTER_BYTE);
                portAdapter.GetByte();

                Thread.Sleep(6);

                do
                {
                    result = (byte)portAdapter.GetByte();
                }
                while ((result != (byte)0xAA) && (result != (byte)0x55) && (cnt++ < 50));

                if ((result != (byte)0xAA) && (result != (byte)0x55))
                {
                    throw new Exception(
                       "OneWireContainer53-XPC Stop Mission failed. Return Code " + Convert.ToString((byte)result));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine(@"
                                **************************************
                                *      MISSÃO PARADA COM SUCESSO     *
                                **************************************");
        }

        public double ReadData_LastMission( PortAdapter portAdapter)
        {
            return 10.0;
        }

        public double ReadData_LastConversion( PortAdapter portAdapter)
        {
            return 12.0;
        }
    }
}
