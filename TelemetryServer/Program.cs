using System.Net;
using System.Net.Sockets;
using TelemetryShared;

var listener = new TcpListener(IPAddress.Any, 5000);
listener.Start();
Console.WriteLine("Server listening on port 5000...");

while (true)
{
    TcpClient client = await listener.AcceptTcpClientAsync();
    Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
    _ = HandleClientAsync(client); // don't await — handle concurrently
}

async Task HandleClientAsync(TcpClient client)
{
    using (client)
    {
        var stream = client.GetStream();
        try
        {
            while (client.Connected)
            {
                var telemetry = await FramedStream.ReadMessageAsync<VehicleTelemetry>(stream);
                if (telemetry == null)
                {
                    Console.WriteLine("Client disconnected.");
                    break;
                }

                Console.WriteLine($"[{telemetry.VehicleId}] " +
                    $"lat={telemetry.Latitude:F4} lon={telemetry.Longitude:F4} " +
                    $"speed={telemetry.SpeedKph:F1}kph at {telemetry.TimestampUtc:T}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection error: {ex.Message}");
        }
    }
}