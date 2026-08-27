using System.Net;
namespace ProjectQ_Server;

public interface IGamePhaseState
{
    void Enter(GameRoom server);
    void UpdateLoop(GameRoom server);
    void HandlePacket(Packet packet, IPEndPoint sender, GameRoom server);
}
