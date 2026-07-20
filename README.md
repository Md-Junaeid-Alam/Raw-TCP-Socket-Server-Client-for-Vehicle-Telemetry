# Raw-TCP-Socket-Server-Client-for-Vehicle-Telemetry
Raw TCP Socket Server &amp; Client for Vehicle Telemetry (no MQTT, no framework)

A from-scratch TCP socket server and client built in C#/.NET, simulating a vehicle telemetry ingestion pipeline without relying on any messaging framework (no MQTT, no SignalR, no HTTP). The goal of this project is to understand what libraries like MQTTnet and SignalR are doing under the hood — connection handling, message framing, and concurrency — by implementing it manually with `System.Net.Sockets`.

## Overview

Most real-time systems (MQTT, HTTP, SignalR, gRPC) sit on top of raw TCP sockets and handle message framing, connection lifecycle, and concurrency for you. This project strips that away and implements those pieces directly:

- A `TcpListener`-based server that accepts multiple concurrent client connections
- A custom **length-prefixed framing protocol** to correctly delimit messages over a raw byte stream
- A `TcpClient`-based vehicle simulator that streams telemetry once per second
- Verified support for multiple simultaneous vehicle connections (concurrency)

## Architecture

```
[Vehicle Simulator #1] ─┐
[Vehicle Simulator #2] ─┼──> TCP (port 5000) ──> [TCP Server] ──> Console output
[Vehicle Simulator #3] ─┘                          (async per-connection handling)
```

Each simulated vehicle opens its own independent TCP connection to the server. The server accepts connections in a loop and hands each one off to an async task, so multiple vehicles can stream telemetry concurrently without blocking each other.

## Tech Stack

| Layer              | Technology                          |
|---------------------|--------------------------------------|
| Transport            | Raw TCP (`System.Net.Sockets`)      |
| Framing protocol      | Custom length-prefixed binary framing |
| Serialization         | `System.Text.Json`                  |
| Concurrency           | `async`/`await`, fire-and-forget per-connection tasks |
| Language / Runtime    | C#, .NET                            |

## Project Structure

```
RawSocketTelemetry/
├── RawSocketTelemetry.sln
├── TelemetryServer/          # TCP server — accepts connections, reads framed telemetry
│   └── Program.cs
├── TelemetryClient/          # TCP client — simulates a vehicle streaming telemetry
│   └── Program.cs
└── TelemetryShared/          # Shared library — model + framing protocol
    ├── VehicleTelemetry.cs
    └── FramedStream.cs
```

**Why a shared library:** the server and client are independent, separately runnable projects, but both need to agree on the data model (`VehicleTelemetry`) and the wire protocol (`FramedStream`). Keeping that contract in one shared project avoids duplicating it in two places.

## The Framing Protocol

Raw TCP delivers a byte stream — it has no built-in concept of "messages." This project uses a simple **length-prefixed framing** scheme:

```
[4-byte message length][UTF-8 JSON payload]
```

The receiver first reads exactly 4 bytes to learn the payload length, then reads exactly that many bytes to reconstruct the full message. This correctly handles two things that raw sockets don't guarantee out of the box:

- **Partial reads** — a single `Read()` call is not guaranteed to return a full message; the reader loops until it has received the full expected byte count.
- **Message boundaries** — without framing, there's no way to tell where one JSON payload ends and the next begins.

This is the same fundamental problem that MQTT, HTTP, and SignalR solve internally — this project solves it explicitly and visibly.

## How to Run

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (8, 9, or 10)

### 1 — Clone the repo
```bash
git clone https://github.com/Md-Junaeid-Alam/RawSocketTelemetry.git
cd RawSocketTelemetry
```

### 2 — Run the server
```bash
cd TelemetryServer
dotnet run
```
Wait for:
```
Server listening on port 5000...
```

### 3 — Run a vehicle simulator
In a second terminal:
```bash
cd TelemetryClient
dotnet run
```
The simulator connects to the server and sends telemetry (vehicle ID, GPS coordinates, speed, timestamp) once per second. The server prints each received message.

## Testing Concurrency

The server is designed to handle multiple vehicles connecting and streaming simultaneously, each on its own async task.

Run several simulator instances in separate terminals, passing a distinct vehicle ID to each:
```bash
dotnet run vehicle-001
dotnet run vehicle-002
dotnet run vehicle-003
```

The server log will show interleaved telemetry from all connected vehicles, confirming that connections are handled concurrently rather than processed one at a time:
```
[vehicle-001] lat=42.3321 lon=-83.0441 speed=54.0kph
[vehicle-002] lat=42.3489 lon=-83.0198 speed=71.0kph
[vehicle-003] lat=42.3602 lon=-83.0355 speed=33.0kph
```

Disconnecting a client mid-stream (Ctrl+C) is also handled cleanly — the server detects the closed connection via a zero-byte read and cleans up that connection without affecting the others.

## What This Project Demonstrates

- Manual TCP server/client implementation using `TcpListener` and `TcpClient`
- A hand-built message framing protocol solving the same problem MQTT/HTTP/SignalR solve internally
- Correct handling of partial reads and mid-stream disconnects
- Concurrent connection handling using async, per-connection tasks
- A clean multi-project .NET solution structure (server / client / shared library)

## Related Projects

- [VehicleLink Gateway](https://github.com/Md-Junaeid-Alam/VehicleLink-Gateway) — a higher-level V2X telemetry gateway using MQTT over mTLS, Kafka, and SignalR, built on ASP.NET Core.
