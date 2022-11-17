using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

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

    var buffer = File.ReadAllBytes("C:/Users/kamil.szywala/Desktop/2.prn");

    writeEndpoint.Write(buffer, 3000, out var bytesWritten);

    var readBuffer = new byte[1024];

    readEnpoint.Read(readBuffer, 3000, out var readBytes);

    var b = System.Text.Encoding.UTF8.GetString(readBuffer);

    Console.WriteLine(b);
}
