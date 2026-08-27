using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
public struct UnitStat
{
    public int maxHP;//최대 체력
    public int maxMP;//최대 마나
    public int attack;//공격력
    public int defense;//방여력
    public int attackSpeed;//공격속도 몇 틱 마다 떄릴것인지 최소 1=>1초당 3대 30hz
    public int attackRange;//일반 공격 범위
    public int skillRange;//스킬 공격 범위
    public int attackPlusMP;//때릴때 마나 회복량
    public int hitPlusMP;//맞을때 마나 회복량
    public int critical;//치명타 확률
}
public class Unit
{
    public string type;                 //종류ex>Assulter,Gaurdian...
    public int id;                      //종류중에 어떤 유닛? 기사,창기사 등
    public int level;
    public int batchIndex;               // 현재 위치
    public int targetBatchIndex;         // 목표 위치
    public int arrivalTick = -1;         // 도착 예정 Tick
    public int nowHP;
    public int nowMP;
    public bool isMoving => batchIndex != targetBatchIndex;
    public Unit(string type, int level, int batchIndex)
    {
        this.type = type;
        this.level = level;
        this.batchIndex = batchIndex;
    }
    public UnitStat stat;
}
namespace ProjectQ_Server
{
    public class GameRoom
    {
        public string RoomId { get; } // 고유 ID
        public Dictionary<string, string> players = new Dictionary<string, string>(); // 플레이어 ID, 이름 쌍?
        public Dictionary<string, TcpClient> Clients { get; } = new(); // 플레이어 ID -> GameSever와 이어지는 TcpClient 쌍
        public ServerState currentState;
        public Dictionary<string, List<Unit>>myUnits = new();
        public List<string> playersOrder= new();
        public int serverTick = 0;
        public int round = 0;

        public GameRoom(string roomId)
        {
            RoomId = roomId;
            currentState = new LobbyState();
            currentState.Enter(this);
        }
        public void AddPlayer(string playerId, TcpClient client)
        {
            Clients[playerId] = client;
        }
        public void Update()
        {
            serverTick++;
            currentState?.UpdateLoop(this);
        }

        public async Task SendPacketAsync(Packet packet, TcpClient client)
        {
            string json = JsonConvert.SerializeObject(packet) + "\n";
            byte[] data = Encoding.UTF8.GetBytes(json);

            try
            {
                await client.GetStream().WriteAsync(data, 0, data.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine($"전송 실패: {e.Message}");
            }
        }
        public void ChangeState(ServerState newState)
        {
            currentState?.Exit(this);
            currentState = newState;
            currentState.Enter(this);

            Console.WriteLine($"게임 상태 변경됨: {currentState.GetType().Name}");
        }

    }
}
