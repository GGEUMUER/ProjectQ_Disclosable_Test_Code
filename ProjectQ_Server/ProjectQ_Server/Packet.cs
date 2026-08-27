namespace ProjectQ_Server;

using System;

using Newtonsoft.Json.Serialization;

public class Packet
{
    public string type { get; set; }

    public string senderId { get; set; }

    public string payload { get; set; }

    public long timestamp { get; set; }

    public long tick { get; set; }
}
[Serializable]
public class Arrivepayload
{
   public int unitId { get; set; }
   public int arriveIndex { get; set; }
 }

[Serializable]
public class UnitCommandListpayload
{
    public List<Command> firstCommands { get; set; }
    public List<Command> secondCommands { get; set; }
}
public class Command
{
    public string type { get; set; }
    public int ticksUntilArrival { get; set; }
}
[Serializable]
public class UnitMoveCommand:Command
{
    public int unitId { get; set; }
    public int nowIndex { get; set; }
    public int targetIndex { get; set; }
}
[Serializable]
public class UnitAttackCommand:Command
{
    public int attackIndex { get; set; }
    public int hitIndex { get; set; }
    public int nowHP { get; set; }
    public int maxHP { get; set; }
    public int attackMaxMP { get; set; }
    public int attackNowMP { get; set; }
    public int hitMaxMP { get; set; }
    public int hitNowMP { get; set; }
}
[Serializable]
public class UnitsIndexespayload
{
    public List<int> firstPlayerIndex { get; set; }
    public List<int> secondPlayerIndex { get; set; }

    public bool isFirst { get; set; }
}
[Serializable]
public class Unitpayload
{
    public string type { get; set; }
    public int level { get; set; }
    public int batchIndex { get; set; }
}
[Serializable]
public class SpawnDatapayload
{
    public int progress { get; set; }

    public Unitpayload firstUnit { get; set; }
    public Unitpayload secondUnit { get; set; }
    public bool isFirst { get; set; }

}
[Serializable]
public class CardSelectedpayload
{
    public int progress{ get; set; }
    public int? firstSelectedIndex { get; set; }
    public int? secondSelectedIndex { get; set; }              //선택한 카드 인덱스
    public bool isMyTurn { get; set; }
    public bool isFirst { get; set; }
}
[Serializable]
public class SelectionTimePayload
{
    public float durationTime { get; set; }
    public float remainingTime { get; set; }             // 남은 선택 시간
}
[Serializable]
public class FirstCardSelectionData
{
    public int progress { get; set; }                    // 현재 카드뽑기 단계
    public string[] firstCardtypes { get; set; }            // 내가 가지고 있는 카드 종류들

    public string[] secondCardtypes { get; set; }
    public bool isMyTurn { get; set; }                   // 내 턴인지 여부
    public bool isFirst { get; set; }
}
