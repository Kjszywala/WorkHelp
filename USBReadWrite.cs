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
        public Int32 _ProductId { get; set; }// = 0x0001;
        public Int32 _VendorId { get; set; }// = 0x0C1F;
        private string devicePath = "";
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

                var switchUsbDevice = usbDeviceCollection.FirstOrDefault(d => d.ProductId == _ProductId && d.VendorId == _VendorId);

                if (switchUsbDevice != null)
                {
                    switchUsbDevice.Open();
                }
                else
                {
                    Console.WriteLine("Cannot connect to device.");
                    return;
                }
                    
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

            _VendorId = Int32.Parse(lines[1].Substring(0, 4),System.Globalization.NumberStyles.HexNumber);
            _ProductId = Int32.Parse(lines[2].Substring(0, 4),System.Globalization.NumberStyles.HexNumber);

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

        /// <summary>
        /// Install or uninstall filter on usb device. 
        /// </summary>
        /// <param name="param">install or uninstall</param>
        /// <param name="usbPath">Path to usb device</param>
        private void setFilter(string param, string usbPath)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.WorkingDirectory = "C:\\Program Files\\LibUSB-Win32\\bin\\";
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/C install-filter {param} \"--device={usbPath}\"";
            process.StartInfo = startInfo;
            process.Start();
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
