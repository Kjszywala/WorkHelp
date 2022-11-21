# WorkHelp

This code supports .net 6.0, I did not check this with other versions.

You will also need the newest version of LibUsbDotNet:
NuGet\Install-Package LibUsbDotNet -Version 3.0.97-alpha

LibUsb-Win32 is also needed to run the usb connection code.
After you downloaded and installed LibUsb-Win32, 
if you want to use it manually you will need to start this app
and install on the right usb ports which are displayed when app is on.
If you want to do it automatically you will need to uncomment the
setFilter(“install”, devicePath) method in the constructor of the USBReadWrite class.
Remember to uninstall the filter after using so the driver can see your device.
You can use setFilter(“uninstall”, devicePath) for it or do it manually.

If you do it automatically you need to add the app.manifest to your project and
in the file change:

requestedExecutionLevel level="asInvoker" uiAccess="false" 
to

requestedExecutionLevel level="requireAdministrator" uiAccess="false" 

The LibUsb-Win32 should be installed in:
C:/Program Files/LibUSB-Win32/ 
so, the bin folder should be in:
C:/Program Files/LibUSB-Win32/bin/

To connect to the printer (USB) you will need the serial number.

Example:
USBReadWrite r = new USBReadWrite("72657602");
r.Read("id-model-name");

Output:
Rio Pro 360
