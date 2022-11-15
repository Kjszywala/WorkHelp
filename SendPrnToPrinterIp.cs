private void sendPrn(string ipAddress, string filePath)
{
    byte[] array = File.ReadAllBytes(filePath);
    var client = new TcpClient(AddressFamily.InterNetwork);
    client.Connect(IPAddress.Parse(ipAddress), 9100);
    client.GetStream().Write(array, 0, array.Length);
    client.Close();
}
