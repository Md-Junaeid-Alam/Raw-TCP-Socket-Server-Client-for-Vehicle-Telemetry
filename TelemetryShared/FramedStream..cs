using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace TelemetryShared;

public static class FramedStream
{
    // Writes: [4-byte length][UTF8 JSON payload]
    public static async Task WriteMessageAsync<T>(NetworkStream stream, T message)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(message);
        byte[] lengthPrefix = BitConverter.GetBytes(payload.Length);

        await stream.WriteAsync(lengthPrefix);
        await stream.WriteAsync(payload);
    }

    // Reads exactly one framed message, handling partial reads correctly
    public static async Task<T?> ReadMessageAsync<T>(NetworkStream stream)
    {
        byte[] lengthBuffer = await ReadExactAsync(stream, 4);
        if (lengthBuffer == null) return default; // connection closed

        int length = BitConverter.ToInt32(lengthBuffer, 0);
        if (length <= 0 || length > 10_000_000) // sanity guard
            throw new InvalidOperationException($"Invalid frame length: {length}");

        byte[] payloadBuffer = await ReadExactAsync(stream, length);
        if (payloadBuffer == null) return default;

        return JsonSerializer.Deserialize<T>(payloadBuffer);
    }

    // TCP can deliver a message across multiple Read() calls.
    // This loops until we've received exactly 'count' bytes, or the connection closes.
    private static async Task<byte[]?> ReadExactAsync(NetworkStream stream, int count)
    {
        byte[] buffer = new byte[count];
        int totalRead = 0;

        while (totalRead < count)
        {
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(totalRead, count - totalRead));
            if (bytesRead == 0)
                return null; // client disconnected mid-message

            totalRead += bytesRead;
        }

        return buffer;
    }
}