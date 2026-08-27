using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
//using System.Runtime.InteropServices.Swift;
using Newtonsoft.Json;
using ProjectQ_Server;
using static System.Runtime.InteropServices.JavaScript.JSType;


public class CardSelectionPhaseState : ServerState
{
    private int progress = 0;
    private Dictionary<string, List<List<int?>>> selectedIndex = new Dictionary<string, List<List<int?>>>();
    private Dictionary<string, List<List<string>>> myCard = new Dictionary<string, List<List<string>>>();
    
    private string[] alltypes = new[] { "Assault", "Magician", "Guardian", "Ranger", "Supporter" };
    private Dictionary<int, List<string>> mixCard = new Dictionary<int, List<string>>();
    private float selectionTime = 1f;
    private float loockTime = 1;
    private int currentTurnIndex = 0;
    private int loadCompleted = 0;
    private List<string> players = new List<string>();
    private int pick = 0;
    private ServerTimer timer;
    public override void Enter(GameRoom server)
    {
        ResetRound(server);
    }
    public override void UpdateLoop(GameRoom server)
    {
        timer?.UpdateTimer(server);
    }

    public override void HandlePacket(Packet packet, string clientId, GameRoom server)
    {
        string playerId = packet.senderId;

        switch (packet.type)
        {
            case "PhaseStart":
                HandlePhaseStart(server);
                break;
            case "CardSelected":
                HandleCardSelection(packet, playerId, server);
                break;
            case "NextProgress":
                HandleNexProgressStart(server);
                break;
            case "Leave":
                //server.HandleLeave(packet);
                break;
        }
    }

    public override void Exit(GameRoom server)
    {
    }
    private void HandlePhaseStart(GameRoom server)
    {
        loadCompleted++;
        if (loadCompleted == 2)
        {
            Console.WriteLine("▶모든 플레이어 선택 시작");
            timer = new ServerTimer(server, selectionTime, true);//5초 타이머 설정
            timer.onTimerEnd = () =>
            {
                EndTimer(server);
            };
            UpdateMyCard();
            SendFirstSelectionDataBroadCast(server);
            loadCompleted = 0;
        }
    }
    private void HandleNexProgressStart(GameRoom server)
    {
        loadCompleted++;
        if (loadCompleted == 2)
        {
            Console.WriteLine("▶모든 플레이어 다음 Progress 준비완료");
            progress++;

            if (progress >= 5)
            {
               server.ChangeState(new BatchPhaseState());
               for(int i=0;i<players.Count;i++)
                {
                    TcpClient ep = server.Clients[server.players[players[i]]];
                    var SendPacket = new Packet
                    {
                        type = "ChangePhase",
                        senderId = "Server",
                        payload = "{}",
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        tick = 0
                    };
                    server.SendPacketAsync(SendPacket,ep);
                }
            }

            else
            {

                foreach (var player in server.players)
                {
                    if (!selectedIndex.ContainsKey(player.Key))
                    {
                        selectedIndex[player.Key] = new List<List<int?>>();
                    }

                    //  리스트 크기 부족하면 null로 채워서 확장
                    while (selectedIndex[player.Key].Count <= progress)
                    {
                        selectedIndex[player.Key].Add(null);
                    }

                    //  해당 progress 위치가 null이면 초기화
                    if (selectedIndex[player.Key][progress] == null)
                    {
                        selectedIndex[player.Key][progress] = new List<int?>();
                    }
                }
                UpdateMyCard();
                SendFirstSelectionDataBroadCast(server);
                timer = new ServerTimer(server, selectionTime, true);//5초 타이머 설정
                timer.onTimerEnd = () =>
                {
                    EndTimer(server);
                };
                loadCompleted = 0;
            }
        }
    }
    private void HandleCardSelection(Packet packet, string playerId, GameRoom server)
    {
        var payload = JsonConvert.DeserializeObject<int>(packet.payload);
        int index = payload;
        Console.WriteLine("▶카드 선택:"+ playerId + "Index:" + index);
        //  값 추가
        if (index != null)
        {
            selectedIndex[playerId][progress].Add(index);
            SendSelectedIndexBroadCast(server, "UpdateSelectedCard");
        }
    }

    private void SendSelectedIndexBroadCast(GameRoom server,string type)
    {
        Console.WriteLine("▶카드 선택 상태 업데이트");
        for (int i = 0; i < players.Count; i++)
        {
            string CurrentPlayer = (currentTurnIndex < 2) ? players[currentTurnIndex] : "";
            TcpClient ep = server.Clients[server.players[players[i]]];
            var data = new CardSelectedpayload
            {
                progress = progress,
                firstSelectedIndex = (selectedIndex[players[0]][progress].Count!=0)?selectedIndex[players[0]][progress].LastOrDefault():null,
                secondSelectedIndex = (selectedIndex[players[1]][progress].Count != 0) ? selectedIndex[players[1]][progress].LastOrDefault():null,
                isMyTurn = (progress == 0) ? ((players[i] == CurrentPlayer) ? true : false) : ((currentTurnIndex < 2) ? true : false),
                isFirst = (i==0)
            };

            var SendPacket = new Packet
            {
                type = type,
                senderId = "Server",
                payload = JsonConvert.SerializeObject(data),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                tick = 0
            };
            server.SendPacketAsync(SendPacket, ep);
        }
    }

    
    private void SendFirstSelectionDataBroadCast(GameRoom server)
    {
        string currentPlayer = players[currentTurnIndex%players.Count];
        string waitPlayer = null;

        for (int i = 0; i < players.Count; i++)
        {
            string playerId = players[i];
            TcpClient ep = server.Clients[server.players[playerId]];

            var data = new FirstCardSelectionData
            {
                progress = progress,
                isMyTurn = (progress==0)?playerId==currentPlayer:true,
                firstCardtypes = myCard[players[0]][progress].ToArray(),
                secondCardtypes = myCard[players[1]][progress].ToArray(),
                isFirst=(i==0)
            };

            var Sendpacket = new Packet
            {
                type = "FirstCardSelectionData",
                senderId = "Server",
                payload = JsonConvert.SerializeObject(data),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                tick = 0
            };
            Console.WriteLine("▶카드 분배 (Progress" + progress+"):"+playerId);
            server.SendPacketAsync(Sendpacket, ep);
        }
    }
    private void SendSelectCompleteBroadCast(GameRoom server)
    {
        if (progress % 2 == 0)
        {
            foreach (var player in players)
            {
                string playerId = player;

                int? selected = selectedIndex[playerId][progress].LastOrDefault();
                List<Unit> units = new();
                if (server.myUnits[playerId].Count != 0)
                {
                    units = server.myUnits[playerId];
                }
                units.Add(new Unit(myCard[playerId][progress][(int)selected], CalculateLevel(playerId), server.myUnits[playerId].Count));
                server.myUnits[playerId] = units;
            }
        }


        for(int i=0;i<players.Count;i++)
        {

            TcpClient ep = server.Clients[server.players[players[i]]];
 
            var firstdata = new Unitpayload
            {
                type = server.myUnits[players[0]].Last().type,
                level = server.myUnits[players[0]].Last().level,
                batchIndex = server.myUnits[players[0]].Last().batchIndex
            };

            var seconddata = new Unitpayload
            {
                type = server.myUnits[players[1]].Last().type,
                level = server.myUnits[players[1]].Last().level,
                batchIndex = server.myUnits[players[1]].Last().batchIndex
            };

            var data = new SpawnDatapayload
            {
                progress = progress,
                firstUnit = firstdata,
                secondUnit = seconddata,
                isFirst = (i == 0)
            };

         
            var Sendpacket = new Packet
            {
                type = "SelectComplete",
                senderId = "Server",
                payload = JsonConvert.SerializeObject(data),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                tick = 0
            };

            server.SendPacketAsync(Sendpacket, ep);
            
        }
        Console.WriteLine("▶양 플레이어 카드 선택 완료!");
    }

    public void ResetRound(GameRoom server)
    {
        progress = 0;
        currentTurnIndex = 0;
        myCard.Clear();
        if (server.round > 0)
        {
            server.playersOrder.Reverse();
        }
        players = server.playersOrder;

        foreach (var player in players)
        {
            if (!selectedIndex.ContainsKey(player))
            {
                selectedIndex[player] = new List<List<int?>>();
            }

            //  리스트 크기 부족하면 null로 채워서 확장
            while (selectedIndex[player].Count <= progress)
            {
                selectedIndex[player].Add(null);
            }

            //  해당 progress 위치가 null이면 초기화
            if (selectedIndex[player][progress] == null)
            {
                selectedIndex[player][progress] = new List<int?>();
            }
            server.myUnits.Add(player, new List<Unit>());
        }
        server.round++;
    }

    public void EndTimer(GameRoom server)
    {
        if (currentTurnIndex <= 1)
        {
            if(progress==0)
            Console.WriteLine("▶"+players[currentTurnIndex] + " 선택 시간 종료!!");
        }
        else
        {
            Console.WriteLine("▶선택 보여주기 종료!");
        }
        if (currentTurnIndex < 1)
        {

            if(progress==0)
            {
                if (selectedIndex[players[currentTurnIndex]][progress].Count == 0)
                {
                    AutoSelect(players[currentTurnIndex]);
                }
                timer = new ServerTimer(server, selectionTime, true);//5초 타이머 설정
            }
            else
            {
                foreach (var player in players)
                {
                    if (selectedIndex[player][progress].Count == 0)
                        AutoSelect(player);
                }
                if (progress % 2 == 0)
                {
                    pick++;
                }
            }

            timer.onTimerEnd = () =>
            {
                EndTimer(server);
            };
        }
        else if (currentTurnIndex == 1)
        {
            switch(progress)
            {
                case 0:
                    if (selectedIndex[players[currentTurnIndex]][progress].Count == 0)
                    {
                        AutoSelect(players[currentTurnIndex]);
                    }
                    break;
            }
            timer = new ServerTimer(server, loockTime, true);//2초 타이머 설정
            timer.onTimerEnd = () =>
            {
                EndTimer(server);
            };
        }
        else
        {
            timer = new ServerTimer(server, 10 ,true);//10초 타이머 설정
            timer.onTimerEnd = () =>
            {
                Console.WriteLine("통신문제 연결지연!");
                Console.ReadLine();
            };
        }
        currentTurnIndex++;
        if (currentTurnIndex <= 2)
        {

            SendSelectedIndexBroadCast(server, "TimerEnd");
            
        }
        else
        {
            SendSelectCompleteBroadCast(server);
            currentTurnIndex = 0;
        }
        // 자동 선택 처리 또는 다음 progress로 넘어가기
    }
    public void AutoSelect(string nowPlayer)
    {
        string waitPlayer = null;
        foreach (var player in players)
        {
            if (player != nowPlayer)
            {
                waitPlayer = player;
                break;
            }
        }
        List<int> index = new List<int>();
        int[] filtered;
        for (int i=0;i < myCard[nowPlayer][progress].Count;i++)
        {
            index.Add(i);
        }
        if (progress==0 &&selectedIndex[waitPlayer][progress].Count!=0)
        {
            filtered = index.Where(x => x != selectedIndex[waitPlayer][progress].Last()).ToArray();
        }
        else
        {
            filtered = index.ToArray();
        }
        Random rand = new Random();
        int randomValue = filtered[rand.Next(filtered.Length)];
        selectedIndex[nowPlayer][progress].Add(randomValue);
 
        Console.WriteLine("▶" + nowPlayer + "랜덤 선택:"+randomValue);
    }
    public void UpdateMyCard()
    {
        switch (progress)
        {
            case 0:
            {
                foreach (var player in players)
                {
                     myCard.TryAdd(player, new List<List<string>>());
                     myCard[player].Add(alltypes.ToList());
                }
                break;
            }
            case 1:
            case 3:
                {
                    List<string> temp =new List<string>();
                   foreach(var type in alltypes)
                   {
                        for(int i=0;i<players.Count;i++)
                        {
                            if (type == alltypes[(int)selectedIndex[players[i]][0].Last()])
                            {
                                break;
                            }
                            else if(i==players.Count-1)
                            {
                                temp.Add(type);
                                temp.Add(type);
                            }
                        }
                   }
                    mixCard.Add(pick,SuffleList(temp));

                    myCard[players[0]].Add(mixCard[pick].GetRange(0, 3));
                    myCard[players[1]].Add(mixCard[pick].GetRange(3, 3));

                    break;
                }
            case 2:
            case 4:
                {
                    foreach (var player in players)
                    {
                        List<string> temp = new List<string>();

                        for(int i=0;i< myCard[player][progress - 1].Count;i++)
                        {
                            if(i!= (int)selectedIndex[player][progress - 1].Last())
                            {
                                temp.Add(myCard[player][progress - 1][i]);
                            }
                        }

                        myCard[player].Add(temp);
                    }
                    break;
                }
        }
    }
    public int CalculateLevel(string playerId)
    {
        int level=0;
        if(progress==0)
        {
            level = 1;
        }
        else
        {
            if (myCard[playerId][progress][0]== myCard[playerId][progress][1])
            {
                level = 1;
                selectedIndex[playerId][progress].Add(0);
            }
            else
            {
                level = 0;
            }
        }
        return level;
    }
    public List<string> SuffleList(List<string> temp)
    {
        Random rng = new Random();
        int n = temp.Count;
        while (n > 1)
        {
            int k = rng.Next(n--); // 0 이상 n 미만
            (temp[n], temp[k]) = (temp[k], temp[n]); // swap
        }
        return temp;
    }

    
}
