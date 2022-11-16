using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace PrinterHelper
{
    public class ReadWrite
    {
        /// <summary>
        /// Send prn to the printer using IP address.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="filePath"></param>
        public void sendPrn(string ipAddress, string filePath)
        {
            byte[] array = File.ReadAllBytes(filePath);
            var client = new TcpClient(AddressFamily.InterNetwork);
            client.Connect(IPAddress.Parse(ipAddress), 9100);
            client.GetStream().Write(array, 0, array.Length);
            client.Close();
        }

        /// <summary>
        /// Returns the setting value.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <returns>Setting value</returns>
        [DllImport("Libraries/CRC32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint calculateCRC32(byte[] buffer, int length);
        public string getSetting(string ip, string setting)
        {
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

            TcpClient client = new TcpClient(ip, 9100);
            NetworkStream stream = client.GetStream();

            Byte[] Data = Header.Concat(crc32).ToArray(); 
            stream.Write(Data, 0, Data.Length);

            Data = new Byte[1024];
            Int32 bytes = stream.Read(Data, 0, Data.Length);
            string response = System.Text.Encoding.UTF8.GetString(Data, 0, bytes);
            var value = readValue(response, setting);

            return value;
        }

        /// <summary>
        /// Needed for getSetting method, reads a string and
        /// returns setting value.
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="setting"></param>
        /// <returns></returns>
        public string readValue(string xml, string setting)
        {
            string[] lines = xml.Split('\n');

            string value = "";

            foreach (string line in lines)
            {
                line.Trim();
                if (line.Contains($"{setting}"))
                {
                    var source = line.Split('>','<');
                    value = source[2];
                }
            }
            return value;
        }
    }
}
