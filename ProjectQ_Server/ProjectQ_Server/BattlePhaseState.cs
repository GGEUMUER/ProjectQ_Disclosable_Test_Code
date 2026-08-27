using ProjectQ_Server;
using System.Net.Sockets;
using System.Numerics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;
using System;


public class BattlePhaseState : ServerState
{
    private Dictionary<string, List<Unit>> allUnits = new Dictionary<string, List<Unit>>();
    private List<string> players = new List<string>();
    private List<int>firstBatch = new List<int> { 3, 2, 1 };
    private List<int> secondBatch = new List<int> { 10, 11, 12 };
    private int loadPlayer = 0;
    private bool battleStart = false;
    private ServerTimer timer;
    public override void Enter(GameRoom server)
    {
        allUnits = server.myUnits;
        players = server.playersOrder;
        foreach (var player in allUnits)
        {
            foreach (var kvp in player.Value)
            {
               kvp.id = 0;
               kvp.stat.attackRange = 1;
               kvp.stat.attack = 80;
               kvp.stat.attackSpeed = 30;
               kvp.stat.maxHP = 1000;
               kvp.stat.maxMP = 80;
               kvp.stat.attackPlusMP = 20;
               kvp.stat.hitPlusMP = 5;
               kvp.nowHP = kvp.stat.maxHP;
               kvp.nowMP = 0;
            }
        }
        Console.WriteLine("▶전투 단계 진입");
    }

    public override void UpdateLoop(GameRoom server)
    {
        if(timer!=null)
        timer.UpdateTimer(server);
        if (battleStart)
        {
            List<Command> fistCommands = new();
            List<Command> secondCommands = new();
       
            List<Unit> firstUnits = allUnits[players[0]];
            List<Unit> secondUnits = allUnits[players[1]];

            makeCommands(firstUnits, secondUnits,fistCommands,server,1);
            makeCommands(secondUnits, firstUnits, secondCommands,server,-1);

     
            if (fistCommands.Count > 0|| secondCommands.Count>0)
            {
                UnitCommandListpayload payload = new()
                {
                    firstCommands = fistCommands,
                    secondCommands = secondCommands
                };
                BroadcastGroupedCommands(payload, server);
            }
            
        }
    }
    public void makeCommands(List<Unit>myUnits,List<Unit>enemyUnits, List<Command> commands, GameRoom server,int moveDistance)
    {
        foreach (var unit in myUnits)
        {
            if (unit.isMoving && server.serverTick >= unit.arrivalTick)
            {
                unit.batchIndex = unit.targetBatchIndex;
                unit.arrivalTick = -1;
                BroadcastUnitArrived(unit, server);
            }

            if (!unit.isMoving)
            {
                var targetsInRange = enemyUnits.Where(e => Math.Abs(e.batchIndex - unit.batchIndex) <= unit.stat.attackRange).ToList();
                if (targetsInRange.Count == 0)
                {
                    int targetIndex = unit.batchIndex + moveDistance;
                    bool isOccupied = false;
                    isOccupied = CheckCanMove(myUnits, targetIndex) || CheckCanMove(enemyUnits, targetIndex);

                    if (!isOccupied)
                    {
                        int distance = Math.Abs(unit.batchIndex - targetIndex);
                        unit.targetBatchIndex = targetIndex;
                        unit.arrivalTick = server.serverTick + (int)(distance * GameConstants.TICK_HZ*0.5f);//4틱 이내 도착

                        commands.Add(new UnitMoveCommand
                        {
                            type = "Move",
                            unitId = unit.id,
                            nowIndex=unit.batchIndex,
                            targetIndex = targetIndex,
                            ticksUntilArrival = unit.arrivalTick
                        });
                    }
                }
                else
                {
                    // 공격 처리 (공격 쿨타임: 공격속도에 따라 Tick 체크)
                    if (server.serverTick % unit.stat.attackSpeed == 0)
                    {
                        var target = targetsInRange.First();
                        Console.WriteLine($"{unit.batchIndex} 공격! 대상: {target.batchIndex}");
                        target.nowHP -= unit.stat.attack;
                        target.nowMP += target.stat.hitPlusMP;
                        unit.nowMP += unit.stat.attackPlusMP;

                        if (unit.nowMP >= unit.stat.maxMP)
                        {
                            unit.nowMP = 0;
                            //스킬 구현
                        }
                        else
                        {
                            commands.Add(new UnitAttackCommand
                            {
                                type = "Attack",
                                attackIndex = unit.batchIndex,
                                hitIndex = target.batchIndex,
                                maxHP = target.stat.maxHP,
                                nowHP = target.nowHP,
                                attackMaxMP = unit.stat.maxMP,
                                attackNowMP = unit.nowMP,
                                hitMaxMP = target.stat.maxMP,
                                hitNowMP = target.nowMP,
                                ticksUntilArrival = unit.arrivalTick
                            });
                        }
                    }
                }
            }
        }
    }

    public override void HandlePacket(Packet packet, string clientId, GameRoom server)
    {
        switch (packet.type)
        {
            case "PhaseStart":
                HandlePhaseStart(server);
                break;
            case "BattleStart":
                HandleBattleStart(server);
                break;
        }
    }
    void HandleBattleStart(GameRoom server)
    {
        loadPlayer++;
        if(loadPlayer==2)
        {  
            timer = new ServerTimer(server, 2, false);
            timer.onTimerEnd = () => { battleStart = true; timer = null; };
        }
    }
    void BroadcastGroupedCommands(UnitCommandListpayload payload, GameRoom server)
    {
        for (int i = 0; i < players.Count; i++)
        {
            string playerId = players[i];
            TcpClient ep = server.Clients[server.players[playerId]];

            var packet = new Packet
            {
                type = "UnitCommandGroup",
                senderId = "Server",
                payload = JsonConvert.SerializeObject(payload),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                tick = server.serverTick
            };

            server.SendPacketAsync(packet, ep);
        }
    }

    void BroadcastUnitArrived( Unit unit, GameRoom server)
    {
        for (int i = 0; i < players.Count; i++)
        {
            string playerId = players[i];
            TcpClient ep = server.Clients[server.players[playerId]];

            var payload = new Arrivepayload
            {
                unitId = unit.id,
                arriveIndex = unit.batchIndex
            };

            var packet = new Packet
            {
                type = "UnitArrived",
                senderId = "Server",
                payload = JsonConvert.SerializeObject(payload),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                tick = server.serverTick
            };

            server.SendPacketAsync(packet, ep);
        }
    }

    public override void Exit(GameRoom server)
    {

    }
    public void HandlePhaseStart(GameRoom server)
    {
        loadPlayer++;
        if (loadPlayer == 2)
        {
            SendIndexDataBroadCast(server);
            loadPlayer = 0;
        }
    }
    public void SendIndexDataBroadCast(GameRoom server)
    {
        Console.WriteLine("▶모두 전장으로 이동");
        List<int>firstBatchIndexes= ReorderAndApplyBatchIndexes(server.myUnits[server.playersOrder[0]], firstBatch);
        List<int> secondBatchIndexes = ReorderAndApplyBatchIndexes(server.myUnits[server.playersOrder[1]], secondBatch);

        for (int i=0;i< players.Count;i++)
        {
            string playerId = players[i];
            TcpClient ep = server.Clients[server.players[playerId]];


            var data = new UnitsIndexespayload
            {
                firstPlayerIndex = firstBatchIndexes,
                secondPlayerIndex = secondBatchIndexes,
                isFirst =( i == 0 )
            };

            var Sendpacket = new Packet
            {
                type = "FirstUnitMove",
                senderId = "Server",
                payload = JsonConvert.SerializeObject(data),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                tick = 0
            };
            server.SendPacketAsync(Sendpacket, ep);
        }
    }
    public List<int> ReorderAndApplyBatchIndexes(List<Unit> units, List<int> target)
    {
        var sortedOrder = units
            .Select((u, i) => new { unitIndex = i, batchIndex = u.batchIndex })
            .OrderBy(x => x.batchIndex)
            .Select(x => x.unitIndex)
            .ToList(); 


        int[] result = new int[units.Count];
        for (int i = 0; i < units.Count; i++)
        {
            int unitIndex = sortedOrder[i];
            result[unitIndex] = target[i];
            units[unitIndex].batchIndex = target[i];
            units[unitIndex].targetBatchIndex = target[i];
        }

        return result.ToList(); 
    }

    List<Unit> GetEnemyUnits(string playerId)
    {
        return allUnits
            .Where(p => p.Key != playerId)
            .SelectMany(p => p.Value)
            .ToList();
    }
    private bool CheckCanMove(List<Unit> units,int targetIndex)
    {
        bool isOccupied = false;
        foreach (var kvp in units)
        {
            if(kvp.targetBatchIndex==targetIndex)
            {
                isOccupied = true;
                break;
            }
        }
        return isOccupied;
    }
}
