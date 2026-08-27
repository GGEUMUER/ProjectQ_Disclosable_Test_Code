namespace ProjectQ_Server;
using System;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

public class LobbyState : ServerState
{
    List<string> readyPlayer = new List<string>();
    List<string> sceneReadyPlayer = new List<string>();
    public override void Enter(GameRoom server)
    {
        Console.WriteLine("LobbyState: 게임 로비에 진입했습니다.");
    }


    public override void HandlePacket(Packet packet, string clientId, GameRoom server)
    {
        switch (packet.type)
        {
            case "Join":
                HandleJoin(server, packet,clientId);
                break;
            case "Ready":
                HandleReady(server,packet);
                break;
            case "Unready":
                HandleUnready(server, packet);
                break;
            case "Leave":
                HandleLeave(server, packet);
                break;
            case "SceneReady":
                HandleSceneReady(server, packet);
                break;
            default:
                Console.WriteLine($"로비에서 처리할 수 없는 패킷: {packet.type}");
                break;
        }

        if (server.Clients.Count >= 2)
        {
            if (sceneReadyPlayer.Count == server.Clients.Count)
            {
                BroadcastGameStart(server);
            }
            else if (readyPlayer.Count == server.Clients.Count && sceneReadyPlayer.Count==0)
            {
                BroadcastLoadScene(server);
            }
        }
    }
    public void  HandleJoin(GameRoom server, Packet packet ,string clientId)
    {
        server.players.Add(packet.senderId,clientId);
        server.playersOrder.Add(packet.senderId);
        Console.WriteLine($"Join: {packet.senderId} ({server.players.Count}/2)");
    }
    public void HandleReady(GameRoom server, Packet packet)
    {

        readyPlayer.Add(packet.senderId);
        Console.WriteLine($"Ready: {packet.senderId} ({readyPlayer.Count}/2)");
    }

    public void HandleUnready(GameRoom server, Packet packet)
    {
        readyPlayer.Remove(packet.senderId);
        Console.WriteLine($"Unready: {packet.senderId} ({readyPlayer.Count}/2)");
    }

    public void HandleLeave(GameRoom server, Packet packet)
    {
        server.Clients.Remove(server.players[packet.senderId]);
        server.players.Remove(packet.senderId);
        server.playersOrder.Remove(packet.senderId);
        readyPlayer.Remove(packet.senderId);
        sceneReadyPlayer.Remove(packet.senderId);
        Console.WriteLine($"Leave: {packet.senderId}");
    }

    public void HandleSceneReady(GameRoom server, Packet packet)
    {
        sceneReadyPlayer.Add(packet.senderId);
        Console.WriteLine($"SceneReady: {packet.senderId} ({sceneReadyPlayer.Count}/{server.Clients.Count})");
    }

    public void BroadcastLoadScene(GameRoom server)
    {
        foreach (var kvp in server.Clients)
        {
            server.SendPacketAsync(new Packet
            {
                type = "SceneLoad",
                senderId = "Server",
                payload = "{}",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                tick = 0
            }, kvp.Value);
        }
    }
    public void BroadcastGameStart(GameRoom server)
    {
        foreach (var kvp in server.Clients)
        {
            server.SendPacketAsync(new Packet
            {
                type = "GameStart",
                senderId = "Server",
                payload = "{}",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                tick = 0
            }, kvp.Value);
        }

        server.ChangeState(new CardSelectionPhaseState());
    }
    public override void UpdateLoop(GameRoom server)
    {
    }
    public override void Exit(GameRoom server)
    {
        Console.WriteLine("LobbyState: 게임 로비에서 나갑니다.");
    }
}
