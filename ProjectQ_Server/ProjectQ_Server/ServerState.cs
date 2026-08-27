namespace ProjectQ_Server
{
    public abstract class ServerState
    {
        public abstract void Enter(GameRoom server);
        public abstract void UpdateLoop(GameRoom server);
        public abstract void HandlePacket(Packet packet, string clientId, GameRoom server);
        public abstract void Exit(GameRoom server);
    }

}