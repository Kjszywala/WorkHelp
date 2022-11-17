using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System.Runtime.InteropServices;
using System.Text;

namespace Trial2
{
    public class USBReadWrite
    {
        #region Variables
        public string PrinterName { get; set; }
        public Int32 _ProductId { get; set; }
        public Int32 _VendorId { get; set; }
        #endregion

        #region Constructor
        public USBReadWrite(string printerName)
        {
            PrinterName = printerName;
        }
        #endregion

        #region Helper
        [DllImport("Libraries/CRC32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint calculateCRC32(byte[] buffer, int length);

        public void WriteRead(string setting)
        {
            using (var context = new UsbContext())
            {
                context.SetDebugLevel(LogLevel.Info);

                var usbDeviceCollection = context.List();

                var switchUsbDevice = usbDeviceCollection.FirstOrDefault(d => d.ProductId == 0x0001 && d.VendorId == 0x0C1F);

                if (switchUsbDevice != null)
                    switchUsbDevice.Open();
                else
                    return;

                switchUsbDevice.ClaimInterface(switchUsbDevice.Configs[0].Interfaces[0].Number);

                var writeEndpoint = switchUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep01);
                var readEnpoint = switchUsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);

                var Header =
                 Encoding.UTF8.GetBytes(
                     "<?xml version=\"1.0\"encoding=\"UTF-8\"?>" +
                     "<prn>" +
                     "<binary-size>0</binary-size>" +
                     "<get>" +
                     $"<param id=\"{setting}\"/>" +
                     "</get>" +
                     "</prn>" +
                     "\0");

                var crc32 = BitConverter.GetBytes(calculateCRC32(Header, Header.Length));

                var buffer = Header.Concat(crc32).ToArray();

                writeEndpoint.Write(buffer, 3000, out var bytesWritten);

                var readBuffer = new byte[1024];

                readEnpoint.Read(readBuffer, 3000, out var readBytes);

                var b = System.Text.Encoding.UTF8.GetString(readBuffer);

                var value = readValue(b, setting);

                Console.WriteLine(value);
            }
        }

        public string readValue(string xml, string setting)
        {
            string[] lines = xml.Split('\n');

            string value = "";

            foreach (string line in lines)
            {
                line.Trim();
                if (line.Contains($"{setting}"))
                {
                    var source = line.Split('>', '<');
                    value = source[2];
                }
            }
            return value;
        }
        #endregion
    }
}
