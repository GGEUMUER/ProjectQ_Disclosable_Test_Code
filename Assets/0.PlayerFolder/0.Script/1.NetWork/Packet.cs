using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using JetBrains.Annotations;
using Newtonsoft.Json;

[Serializable]
public class Packet
{
    public string type;       // "Join", "GameStart", etc - 패킷 종류
    public string senderId;   // 고유 클라이언트 ID
    //public object payload; // 스트링 말고 진짜 제이슨으로 보내보기
    public string payload;    // 추가 데이터 (직렬화된 JSON)
    public long timestamp;    // UTC 타임
    public int tick;          // 서버 틱 or 0
}
[Serializable]
// 유닛이 목표 위치에 도착하였는가?
public class Arrivepayload
{
    public int unitId { get; set; }
    public int arriveIndex { get; set; }
}

[Serializable] // Set by type=InitialSnapshot time: BattlePhase start time
public class InitalUnit
{
    public int Id { get; set; }
    public int BatchIndex { get; set; }
    public int Player { get; set; }
    public int UnitIndex {  get; set; }
}

[Serializable] // Set by type=InitialSnapshot
public class InitialSnapShot
{
    public int Seed { get; set; }
    public List<InitalUnit> Units { get; set; }
}

public interface IBattleEvent
{
    
}

public interface IBattleObject
{
    public float CenterX { get; set; }
    public float CenterY { get; set; }
    public int Player { get; set; }
    public int UnitIndex { get; set; }
}

public class SerializedBattleEvent
{
    public string Type {  get; set; }
    public IBattleEvent Event { get; set; }
}
public class SerializedBattleObject
{
    public string Type {  get; set; }
    public IBattleObject Event { get; set; }

}

public class AttackStartEvent: IBattleEvent
{
    public int AttackerPlayer { get; set; }
    public int AttackerIndex { get; set; }
    public int Direction { get; set; }

}

public class TargetDamegedEvent : IBattleEvent
{
    public int AttackerPlayer { get; set; }
    public int TargetIndex { get; set; }
    public int AttackerIndex { get; set; }
    public int TargetPlayer {  get; set; }
    public int Damege { get; set; }
    public bool IsCrital { get; set; }
}

// 범위 효과 스킬 패킷
public class AOEBattleObject : IBattleObject 
{
    public float CenterX {  set; get; }
    public float CenterY { set; get; }
    public int Player { get; set; }
    public int UnitIndex { get; set; }
}

// 포물선(곡선) 스킬 패킷
public class ParabolaProjectile : IBattleObject
{
    public float CenterX { set; get; }
    public float CenterY { set; get; }
    public int Player { get; set; }
    public int UnitIndex { get; set; }
    public float VelocityX { set; get; }
    public float VelocityY { set; get; }
}

[Serializable]
public class EndSnapshot
{
    public bool IsWin {get; set;}
    public List<UnitSnapshot> Units { get; set;}
}

[Serializable] // Set by type=BattleProgress / type=EndSnapshot
public class BattleProgress
{
    public int TotalTick { get; set; }
    public List<BattleSnapshot> BattleSnapshots { get; set; }
}
[Serializable] // Set by type=BattleProgress / type=EndSnapshot
public class BattleSnapshot
{
    public int BattleTick { get; set; }
    public List <UnitSnapshot> Units { get; set; }
    public List <SerializedBattleEvent> Events { get; set; }
    public List<SerializedBattleObject> Objects { get; set; }
}
[Serializable] // Set by type=BattleProgress / type=EndSnapshot
public class UnitSnapshot
{
    public bool Player { get; set; }
    public int UnitIndex { get; set; }
    public int MaxHp {  get; set; }
    public int MaxMp { get; set; }
    public float HalfWidth { get; set; }
    public float HalfHeight { get; set; }
    public float X {  get; set; } // based by tick
    public float Y { get; set; }
    public int State {  get; set; } // UnitState -> 0: Stop, 1: Move, 2: Attack, 10: Dying, 11: Death
    public int NextActionTick { get; set; }
    public int Hp {  get; set; }
    public int Mp { get; set; }
    public int Direction { get; set; }
}
[Serializable] // Set by type=BattleProgress / type=EndSnapshot
public class StatusEffeectSnapshot
{
    public int Type { get; set; }
    public int EndTick { get; set; }
}

// 커맨드를 상속하는 패킷들을 참조하는 코드들 갈아엎어야 함.

[Serializable]
// 플레이어의 명령들을 리스트에 저장해둔 클래스
public class UnitCommandListpayload
{
    public List<Command> firstCommands { get; set; }
    public List<Command> secondCommands { get; set; }
}
[JsonConverter(typeof(CommandConverter))]
// 명령을 위한 기본 클래스 CommandConverter를 가지고 역직렬화
public class Command
{
    public string type { get; set; } // 명령 종류
    public int ticksUntilArrival { get; set; } // 목표 위치까지의 도달 예정 틱
}
[Serializable]
// 유닛 이동 명령
public class UnitMoveCommand:Command 
{
    public int unitId { get; set; } // 고유 ID
    public int nowIndex { get; set; } // 지금 위치 인덱스
    public int targetIndex { get; set; } // 목표 위치 인덱스
}
[Serializable]
// 유닛 공격 명령
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
// 1p 2p의 유닛 위치 정보, 누가 선공인가에 대한 정보
public class UnitsBatchIndex 
{ 
    public List<int> MyUnitIndex {  get; set; }
    public List<int> OpponentIndex { get; set; }
    public bool isFirst { get; set; }
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
    public string Type { get; set; }
    public int Level { get; set; }
}
/*
 * 기존 패킷
// 개별 유닛 정보로 추정
public class Unitpayload
{
    public string type { get; set; }
    public int level { get; set; }
    public int batchIndex { get; set; }
}
*/
[Serializable]
public class UpdateBothUnits
{
    public int Step { get; set; }

    public Unitpayload MyUnit { get; set; }
    public Unitpayload OpponentUnit { get; set; }
}// 아래는 기존 패킷
/*
 * 기존 코드
// 스폰 될 때 서버가 클라에게 전달하는 데이터
public class SpawnDatapayload
{
    public int progress { get; set; }

    public Unitpayload firstUnit { get; set; }
    public Unitpayload secondUnit { get; set; }
    
    public bool isFirst { get; set; }
}*/

// 카드 선택 정보 전송 패킷
[Serializable]
public class UpdateSelectedCard
{ 
    public int Step { get; set; }
    public string UnitType { get; set; }
    public bool IsOwner { get; set; }
} // 아래는 기존 패킷
/*
 * 기존 패킷
public class CardSelectedpayload
{
    public int progress{ get; set; }
    public int? firstSelectedIndex { get; set; }
    public int? secondSelectedIndex { get; set; }              //선택한 카드 인덱스
    public bool isMyTurn { get; set; }
    public bool isFirst { get; set; }
}
*/

// 추가 패킷: DealCards (후공 유저 선택 또는 3초 후 강제 진행 후 즉시 추가 전송되는 패킷)
[Serializable]
public class DealCards
{ 
    public int Step { get; set; }
    public string MyCard {  get; set; }
    public string OpponentCard { get; set; }
}

// 추가 패킷: PickTwoCards (각 유저 선택 후 불러지는 패킷. 남은 두개 카드 전송)
[Serializable]
public class PickTwoCards
{ 
    public int Step { get; set; }
    public string[] Units { get; set; }
}

[Serializable]
public class SelectionTimePayload
{
    public float durationTime { get; set; }
    public float remainingTime{ get; set; }             // 남은 선택 시간
}

[Serializable]
public class PickFirstCards
{ 
    public int Step { get; set; }
    public string[] AllTypes { get; set; }
    public bool IsMyTurn { get; set; }
}// 아래는 기존 패킷
/*
 * 기존 패킷
public class FirstCardSelectionData
{
    public int progress{ get; set; }                    // 현재 카드뽑기 단계
    public string[] firstCardTypes { get; set; }           // 내가 가지고 있는 카드 종류들
    public string[] secondCardtypes { get; set; }
    public bool isMyTurn { get; set; }                  // 내 턴인지 여부
    public bool isFirst { get; set; } // 기존
}
*/