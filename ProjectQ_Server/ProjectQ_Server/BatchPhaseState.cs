using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace ProjectQ_Server
{
    class BatchPhaseState : ServerState
    {
        private Dictionary<string, List<Unit>> myUnits = new Dictionary<string, List<Unit>>();
        private int loadCompleted = 0;
        private int batchTime = 10;
        private ServerTimer timer;
        public override void Enter(GameRoom server)
        {
            Console.WriteLine("▶ 배치 단계 진입");
            myUnits = server.myUnits;
        }

        public override void HandlePacket(Packet packet, string clientId, GameRoom server)
        {
            string playerId = packet.senderId;

            switch (packet.type)
            {
                case "PhaseStart":
                    HandlePhaseStart(server);
                    break;
                case "SetComplete":
                    HandleUnitSetComplete(server, packet);
                    break;
            }
        }

        public override void UpdateLoop(GameRoom server)
        {
            timer?.UpdateTimer(server);
        }
        public override void Exit(GameRoom server)
        {

        }

        private void HandlePhaseStart(GameRoom server)
        {
            loadCompleted++;
            if (loadCompleted == 2)
            { 
                loadCompleted = 0;
                Console.WriteLine($"▶양 플레이어 배치 시작");
                timer = new ServerTimer(server, batchTime,true);//타이머 설정
                timer.onTimerEnd = () =>
                {
                    EndTimer(server);
                };
            }
        }
        private void HandleUnitSetComplete(GameRoom server,Packet packet)
        {
            var payload = JsonConvert.DeserializeObject<List<int>>(packet.payload);
            string debugString = "Units:";
            for (int i = 0; i < payload.Count; i++)
            {
                myUnits[packet.senderId][i].batchIndex = payload[i];
                debugString= string.Concat(debugString,"|", myUnits[packet.senderId][i].type);
            }
            loadCompleted++;
            Console.WriteLine("▶"+packet.senderId+debugString);
            if (loadCompleted == 2)
            {
                loadCompleted = 0;
                List<int> firstBatchIndexes = server.myUnits[server.playersOrder[0]].Select(u => u.batchIndex).ToList();
                List<int> secondBatchIndexes = server.myUnits[server.playersOrder[1]].Select(u => u.batchIndex).ToList();

                for (int i=0;i<server.playersOrder.Count;i++)
                {
                    string playerId = server.playersOrder[i];
                    TcpClient ep = server.Clients[server.players[playerId]];

                    var data = new UnitsIndexespayload
                    {
                        firstPlayerIndex = firstBatchIndexes,
                        secondPlayerIndex= secondBatchIndexes,
                        isFirst = (i == 0)
                    };

                    var Sendpacket = new Packet
                    {
                        type = "ChangePhase",
                        senderId = "Server",
                        payload = JsonConvert.SerializeObject(data),
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        tick = 0
                    };
                    server.SendPacketAsync(Sendpacket, ep);
                }
                Console.WriteLine("▶양 플레이어 배치 종료!");
                server.ChangeState(new BattlePhaseState());
            }
        }
        private void EndTimer(GameRoom server)
        {
            SendBatchEndBroadCast(server);
        }
        private void SendBatchEndBroadCast(GameRoom server)
        {
            foreach (var kvp in server.players)
            {
                string playerId = kvp.Key;
                TcpClient ep = server.Clients[server.players[playerId]];

                var Sendpacket = new Packet
                {
                    type = "TimerEnd",
                    senderId = "Server",
                    payload ="{}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    tick = 0
                };
                server.SendPacketAsync(Sendpacket, ep);
            }
        }

    }
}
