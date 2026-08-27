using ProjectQ_Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;



/*
 *  
        StartAsync() -> 
            TryMatchPlayers() ->
                HandleClientAsync() ->
                    UpdateLoop()
    StartAsync(): TCP 서버 시작 -> 
        <!cts.Token.IsCancellationRequested라면(클라이언트가 접속하면) ->
            GUID 발급 -> 클라에 저장 -> waitingPlayers에 enqueue -> TryMatchPlayers()구에서 방 할당 (방이 있다면 기존 방에, 아니면 새방 생성)
 
 */
public static class GameConstants
{
    public const float TICK_HZ = 30f;
    public const float TICK_DELTA_SECONDS = 1f / TICK_HZ; // 정확히 30Hz
}
public class GameServer
{
    private TcpListener tcpListener;
    public Dictionary<string, TcpClient> Clients = new Dictionary<string, TcpClient>();
    private CancellationTokenSource cts = new CancellationTokenSource();
    private const int Port = 7777;
    private Queue<string> waitingPlayers = new(); // 대기열
    private Dictionary<string, GameRoom> roomByPlayer = new(); // 누구든지 방 찾을 수 있게
    private Dictionary<string, GameRoom> activeRooms = new();
    private GameRoom pendingRoom = null;
    public async Task StartAsync()
    {
        tcpListener = new TcpListener(IPAddress.Any, 7777);
        tcpListener.Start();

        Console.WriteLine("서버 시작됨");
        _ = Task.Run(UpdateLoop, cts.Token);

        while (!cts.Token.IsCancellationRequested)
        {
            var tcpClient = await tcpListener.AcceptTcpClientAsync();
            string clientId = Guid.NewGuid().ToString();

            Clients[clientId] = tcpClient;
            waitingPlayers.Enqueue(clientId);
            TryMatchPlayers();

            Console.WriteLine($"클라이언트 연결: {clientId}");
            _ = HandleClientAsync(clientId, tcpClient);
        }
    }

    private async Task HandleClientAsync(string clientId, TcpClient tcpClient)
    {
        var stream = Clients[clientId].GetStream();
        byte[] buffer = new byte[4096];
        var sb = new StringBuilder();

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                if (bytesRead == 0) break;

                sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                string all = sb.ToString();
                var chunks = all.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                if (!all.EndsWith("\n"))
                {
                    sb.Clear();
                    sb.Append(chunks[^1]);
                    chunks = chunks[..^1];
                }
                else sb.Clear();

                foreach (var json in chunks)
                {
                    var packet = JsonConvert.DeserializeObject<Packet>(json);
                    if (roomByPlayer.TryGetValue(clientId, out var room))
                    {
                        room.currentState?.HandlePacket(packet, clientId, room);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"수신 실패: {e.Message}");
        }
    }
    public void Stop()
    {
        cts.Cancel();
        tcpListener.Stop();
        Console.WriteLine("서버 종료됨.");
    }
    private async Task UpdateLoop()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        double tickInterval = 1000.0 / GameConstants.TICK_HZ;
        long lastTickTime = 0;

        while (!cts.Token.IsCancellationRequested)
        {
            long now = stopwatch.ElapsedMilliseconds;
            if (now - lastTickTime >= tickInterval)
            {
                lastTickTime += (long)tickInterval;
                foreach (var room in activeRooms.Values)
                    room.Update();
            }
            await Task.Delay(1);
        }
    }
    private void TryMatchPlayers()
    {
        while (waitingPlayers.Count > 0)
        {
            string newPlayer = waitingPlayers.Dequeue();
            var newClient = Clients[newPlayer];

            if (pendingRoom == null)
            {
                // 방 새로 생성
                string roomId = Guid.NewGuid().ToString();
                pendingRoom = new GameRoom(roomId);
                pendingRoom.AddPlayer(newPlayer, newClient);
                roomByPlayer[newPlayer] = pendingRoom;
                activeRooms[roomId] = pendingRoom;

                Console.WriteLine($"대기 방 생성됨: {roomId}, 플레이어 {newPlayer}");
            }
            else
            {
                // 기존 대기 방에 추가
                pendingRoom.AddPlayer(newPlayer, newClient);
                roomByPlayer[newPlayer] = pendingRoom;

                Console.WriteLine($"방 입장 완료: {pendingRoom.RoomId}, 플레이어 {newPlayer}");

                pendingRoom = null; // 다음 방 생성을 위해 초기화
            }
        }
    }
}
