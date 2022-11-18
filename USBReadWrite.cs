using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System.Runtime.InteropServices;
using System.Text;

namespace USB
{
    public class USBReadWrite
    {
        #region Variables
        public string SerialNumber { get; set; }
        public Int32 ProductId1 { get; set; }// = 0x0001;
        public Int32 VendorId1 { get; set; }// = 0x0C1F;
        #endregion

        #region Constructor
        public USBReadWrite(string serialNumber)
        {
            SerialNumber = serialNumber;
            setPIDVID();
        }
        #endregion

        #region Helper
        [DllImport("Libraries/CRC32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint calculateCRC32(byte[] buffer, int length);

        /// <summary>
        /// Reads the value from the printer.
        /// </summary>
        /// <param name="setting">Printer</param>
        public void Read(string setting)
        {
            using (var context = new UsbContext())
            {
                context.SetDebugLevel(LogLevel.Info);

                var usbDeviceCollection = context.List();

                var switchUsbDevice = usbDeviceCollection.FirstOrDefault(d => d.ProductId == ProductId1 && d.VendorId == VendorId1);

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

        /// <summary>
        /// Reads the value from XML. Used in Read() method.
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="setting"></param>
        /// <returns>XML value</returns>
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

        /// <summary>
        /// Set the Product Id and Vendor Id.
        /// </summary>
        private void setPIDVID()
        {
            string devicePath = "";
            var list = GetUSBDevices();
            foreach(var device in list)
            {
                if (device.DeviceID.Contains(SerialNumber))
                {
                    devicePath = device.DeviceID;
                }
            }
            if(string.IsNullOrWhiteSpace(devicePath))
            {
                Console.WriteLine("No device in the list contain this serial number.");
            }
            string[] lines = devicePath.Split('_','_');

#pragma warning disable CS8605 // Unboxing a possibly null value.
            VendorId1 = Int32.Parse(lines[1].Substring(0, 4),System.Globalization.NumberStyles.HexNumber);
            ProductId1 = Int32.Parse(lines[2].Substring(0, 4),System.Globalization.NumberStyles.HexNumber);
#pragma warning restore CS8605 // Unboxing a possibly null value.
        }
        /// <summary>
        /// List of all USB devices.
        /// </summary>
        /// <returns>List of USB devices</returns>
        static List<USBDeviceInfo> GetUSBDevices()
        {
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();
            ManagementObjectCollection collection;
#pragma warning disable CA1416 // Validate platform compatibility
            //Win32_USBHub can give us a list of USB contollers while Win32_PnPEntity shows all devices
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_USBHub where DeviceID Like ""USB%"""))
                collection = searcher.Get();

            foreach (var device in collection)
            {
                devices.Add(new USBDeviceInfo((string)device.GetPropertyValue("DeviceID")));
            }
            collection.Dispose();
#pragma warning restore CA1416 // Validate platform compatibility
            return devices;
        }
        #endregion
    }

    /// <summary>
    /// Class which holds Device Id for our printer.
    /// </summary>
    class USBDeviceInfo
    {
        public USBDeviceInfo(string deviceID)
        {
            this.DeviceID = deviceID;
        }
        public string DeviceID { get; private set; }
    }
}
