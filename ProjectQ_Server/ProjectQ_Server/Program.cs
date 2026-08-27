using ProjectQ_Server;

public class Program
{
    static async Task Main(string[] args)
    {
        GameServer server = new GameServer();
        await server.StartAsync();
    }
}