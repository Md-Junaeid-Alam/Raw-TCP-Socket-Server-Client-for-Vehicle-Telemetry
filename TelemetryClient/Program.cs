using System.Net.Sockets;
using TelemetryShared;

using var client = new TcpClient();
await client.ConnectAsync("127.0.0.1", 5000);
Console.WriteLine("Connected to server.");

var stream = client.GetStream();
var random = new Random();
double lat = 42.3314, lon = -83.0458; // Detroit-ish starting point

while (true)
{
    lat += (random.NextDouble() - 0.5) * 0.001;
    lon += (random.NextDouble() - 0.5) * 0.001;

    var telemetry = new VehicleTelemetry
    {
        VehicleId = "vehicle-001",
        Latitude = lat,
        Longitude = lon,
        SpeedKph = random.Next(20, 90),
        TimestampUtc = DateTime.UtcNow
    };

    await FramedStream.WriteMessageAsync(stream, telemetry);
    Console.WriteLine($"Sent telemetry at {telemetry.TimestampUtc:T}");

    await Task.Delay(1000);
}