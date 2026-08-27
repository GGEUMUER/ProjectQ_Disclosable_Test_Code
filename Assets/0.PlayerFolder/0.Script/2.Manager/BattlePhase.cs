using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using Core;

/* -----------------------------------------------------------
 *  BattlePhase.cs (시간 기반 플레이백 버전)
 *  - 프레임레이트와 무관하게 ServerTicksPerSecond 기준으로 재생
 *  - 통째로 교체해서 사용
 * -----------------------------------------------------------*/


#region Debug dumper
public static class NetDebugDump
{
    public static bool Enabled = true;
    public static bool DumpRawJson = false;    // 너무 길면 끄세요
    public static int MaxRawJsonChars = 20000;
    public static int MaxTicksToDump = 40;     // 로그 잘리는거 방지용
    public static int MaxUnitsPerTick = -1;
    public static int MaxEventsPerTick = -1;
    public static int MaxObjectsPerTick = -1;

    static readonly CultureInfo CI = CultureInfo.InvariantCulture;

    public static void DumpPacketHeader(Packet p, string rawPayload = null)
    {
        if (!Enabled) return;
        var sb = new StringBuilder(1024);
        sb.AppendLine("=== [NET] Packet Header ===");
        sb.AppendLine($"type={p.type}, senderId={p.senderId}, tick={p.tick}, timestamp={p.timestamp}");
        if (DumpRawJson && !string.IsNullOrEmpty(rawPayload))
        {
            var raw = rawPayload.Length > MaxRawJsonChars
                ? rawPayload[..MaxRawJsonChars] + $" ...(+{rawPayload.Length - MaxRawJsonChars} chars)"
                : rawPayload;
            sb.AppendLine("--- RAW payload ---");
            sb.AppendLine(raw);
        }
        Debug.Log(sb.ToString());
    }

    public static void DumpInitialSnapshot(InitialSnapShot s)
    {
        if (!Enabled || s == null) return;
        var sb = new StringBuilder(2048);
        sb.AppendLine("=== [NET] InitialSnapshot ===");
        sb.AppendLine($"Seed: {s.Seed}");
        int count = s.Units?.Count ?? 0;
        sb.AppendLine($"Units: {count}");
        if (s.Units != null)
        {
            for (int i = 0; i < s.Units.Count; i++)
            {
                var u = s.Units[i];
                sb.AppendLine($"  [{i}] Id={u.Id}, Player={u.Player}, UnitIndex={u.UnitIndex}, BatchIndex={u.BatchIndex}");
            }
        }
        Debug.Log(sb.ToString());
    }

    public static void DumpBattleProgress(BattleProgress p)
    {
        if (!Enabled || p == null) return;
        var sb = new StringBuilder(8192);
        sb.AppendLine("=== [NET] BattleProgress ===");
        sb.AppendLine($"TotalTick: {p.TotalTick}");
        int snapCount = p.BattleSnapshots?.Count ?? 0;
        sb.AppendLine($"BattleSnapshots: {snapCount}");

        if (p.BattleSnapshots != null)
        {
            int limitTick = MaxTicksToDump < 0 ? snapCount : Math.Min(snapCount, MaxTicksToDump);
            for (int i = 0; i < limitTick; i++)
            {
                var s = p.BattleSnapshots[i];
                sb.AppendLine($"-- Snapshot[{i}] Tick={s.BattleTick}");

                int uCount = s.Units?.Count ?? 0;
                sb.AppendLine($"   Units: {uCount}");
                if (s.Units != null)
                {
                    int limitUnits = MaxUnitsPerTick < 0 ? uCount : Math.Min(uCount, MaxUnitsPerTick);
                    for (int ui = 0; ui < limitUnits; ui++)
                    {
                        var u = s.Units[ui];
                        sb.AppendLine(
                            $"     [U{ui}] Player={(u.Player ? 1 : 0)}(raw:{u.Player}), UnitIndex={u.UnitIndex}, " +
                            $"Pos=({u.X.ToString("0.###", CI)}, {u.Y.ToString("0.###", CI)}), Dir={u.Direction}, " +
                            $"State={u.State}, NextActionTick={u.NextActionTick}, " +
                            $"HP={u.Hp}/{u.MaxHp}, MP={u.Mp}/{u.MaxMp}, Half=({u.HalfWidth.ToString("0.###", CI)}, {u.HalfHeight.ToString("0.###", CI)})"
                        );
                    }
                }

                int eCount = s.Events?.Count ?? 0;
                sb.AppendLine($"   Events: {eCount}");

                int oCount = s.Objects?.Count ?? 0;
                sb.AppendLine($"   Objects: {oCount}");
            }
        }
        Debug.Log(sb.ToString());
    }

    public static void DumpEndSnapshot(EndSnapshot p)
    {
        if (!Enabled || p == null) return;
        var sb = new StringBuilder(4096);
        sb.AppendLine("=== [NET] EndSnapshot ===");
        sb.AppendLine($"IsWin: {p.IsWin}");
        int count = p.Units?.Count ?? 0;
        sb.AppendLine($"Units: {count}");
        if (p.Units != null)
        {
            for (int i = 0; i < p.Units.Count; i++)
            {
                var u = p.Units[i];
                sb.AppendLine(
                    $"  [{i}] Player={u.Player}, UnitIndex={u.UnitIndex}, " +
                    $"Pos=({u.X.ToString("0.###", CI)}, {u.Y.ToString("0.###", CI)}), Dir={u.Direction}, State={u.State}, " +
                    $"HP={u.Hp}/{u.MaxHp}, MP={u.Mp}/{u.MaxMp}, NextActionTick={u.NextActionTick}"
                );
            }
        }
        Debug.Log(sb.ToString());
    }
}
#endregion

#region 빈 바디 컨버터
public sealed class EmptyBattleEvent : IBattleEvent { }
public sealed class DefaultBattleObject : IBattleObject
{
    public float CenterX { get; set; }
    public float CenterY { get; set; }
    public int Player { get; set; }
    public int UnitIndex { get; set; }
}

public sealed class SerializedBattleEventConverter : JsonConverter<SerializedBattleEvent>
{
    public override SerializedBattleEvent ReadJson(JsonReader reader, Type objectType, SerializedBattleEvent existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);
        var type = jo["type"]?.ToString() ?? jo["Type"]?.ToString();
        return new SerializedBattleEvent { Type = type, Event = new EmptyBattleEvent() };
    }
    public override void WriteJson(JsonWriter writer, SerializedBattleEvent value, JsonSerializer serializer)
    {
        var jo = new JObject { ["Type"] = value.Type, ["Event"] = null };
        jo.WriteTo(writer);
    }
}

public sealed class SerializedBattleObjectConverter : JsonConverter<SerializedBattleObject>
{
    public override SerializedBattleObject ReadJson(JsonReader reader, Type objectType, SerializedBattleObject existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);
        var type = jo["type"]?.ToString() ?? jo["Type"]?.ToString();
        var evTok = jo["event"] ?? jo["Event"] ?? jo;
        var obj = new DefaultBattleObject
        {
            CenterX = evTok.Value<float?>("centerX") ?? 0f,
            CenterY = evTok.Value<float?>("centerY") ?? 0f,
            Player = evTok.Value<int?>("player") ?? 0,
            UnitIndex = evTok.Value<int?>("unitIndex") ?? 0
        };
        return new SerializedBattleObject { Type = type, Event = obj };
    }
    public override void WriteJson(JsonWriter writer, SerializedBattleObject value, JsonSerializer serializer)
    {
        var jo = new JObject
        {
            ["Type"] = value.Type,
            ["Event"] = value.Event != null ? JToken.FromObject(value.Event, serializer) : null
        };
        jo.WriteTo(writer);
    }
}
#endregion




public interface IBattleReady { void NotifyUnitArrived(); }

public class BattlePhase : IGameScenePhase, IBattleReady
{
    // ===== 재생 파라미터(인스펙터에서 조절) =====
    [Header("Playback")]
    [Tooltip("서버 1초당 틱 수 (예: 10)")]
    public float ServerTicksPerSecond = 10f;

    [Tooltip("재생 속도 배수 (1=실시간, 0.5=절반, 2=2배)")]
    public float PlaybackSpeed = 1f;

    [Tooltip("버퍼가 많이 앞서 있을 때 한 프레임에 최대 진행할 틱 수(급행 제한)")]
    public int MaxTicksPerUpdate = 3;

    [Tooltip("서버 최신틱 - 이만큼 뒤에서 재생 (지연 완충)")]
    public int TargetLatencyTick = 3;

    // ===== 로그 토글 =====
    [Header("Logging")]
    [SerializeField] bool LogStateChanges = false;
    [SerializeField] bool LogHpChanges = false;
    [SerializeField] bool LogMove = true;
    [SerializeField] bool LogDirChanges = false;

    // ===== 내부 상태 =====
    private GameSceneManager _gsm;
    private UIManager _ui;

    private bool _started;
    private int _curTick;                 // 현재 렌더 기준 틱
    private int _latestServerTick;
    private int _playUntilTick;

    private bool _gotEnd;
    private EndSnapshot _pendingEnd;

    private readonly SortedDictionary<int, BattleSnapshot> _buffer = new();
    private readonly Dictionary<(int side, int unitIdx), UnitPrev> _prev = new();

    private struct UnitPrev { public int Hp, State, Dir; public float X, Y; }

    // 시간 기반 누적
    private float _tickAccum;             // 누적된 시간(초)
    private float TickDuration => 1f / Mathf.Max(1f, ServerTicksPerSecond); // 한 틱의 시간(초)

    // JSON
    private static readonly JsonSerializerSettings JSON_INIT = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };
    private static readonly JsonSerializerSettings JSON_RUNTIME = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Converters = new List<JsonConverter>
        {
            new SerializedBattleEventConverter(),
            new SerializedBattleObjectConverter()
        }
    };

    // ===== 수명주기 =====
    public void Enter(GameSceneManager gsm)
    {
        _gsm = gsm;
        _ui = gsm.uIManager;

        _started = false;
        _curTick = 0;
        _latestServerTick = 0;
        _playUntilTick = 0;
        _buffer.Clear();
        _prev.Clear();
        _gotEnd = false;
        _pendingEnd = null;

        _tickAccum = 0f;

        Camera.main.GetComponent<SmoothFollowCamera>()?.CoroutineCamera(Vector3.zero);
        _ui?.ReturnCurrentCanvas()?.SetActive(false);

        Debug.Log("[BattlePhase] Enter: 시간 기반 재생 준비");
    }

    public void UpdateLoop()
    {
        // 버퍼가 차면 늦게라도 스타트
        if (!_started && _buffer.Count > 0)
        {
            _started = true;
            _curTick = GetMinTick(_buffer);
            _tickAccum = 0f;
            Debug.Log($"[BattlePhase] START cur={_curTick}, latest={_latestServerTick}, playUntil={_playUntilTick}, tickDur={TickDuration:0.###}s");
        }

        if (!_started) return;

        // 시간 누적 (재생 속도 배수 적용)
        _tickAccum += Mathf.Max(0f, PlaybackSpeed) * Time.deltaTime;

        // 목표틱(지연 고려)
        int targetTick = Mathf.Max(0, _latestServerTick - TargetLatencyTick);

        // 누적 시간이 한 틱 이상 될 때만 한 틱씩 전진 (프레임레이트와 독립)
        int advanced = 0;
        while (_tickAccum >= TickDuration && advanced < Mathf.Max(1, MaxTicksPerUpdate))
        {
            // 다음 틱 데이터가 있고, 아직 target에 도달하지 않았다면 1틱 진전
            if (_curTick < targetTick && _buffer.ContainsKey(_curTick + 1))
            {
                _curTick++;
                _tickAccum -= TickDuration;
                advanced++;
            }
            else
            {
                // 데이터가 없거나(버퍼 부족) target에 이미 도달 → 더 이상 전진 안 함
                break;
            }
        }

        // 현재 틱과 그 다음 틱으로 보간 렌더
        if (!_buffer.TryGetValue(_curTick, out var a)) return;
        _buffer.TryGetValue(_curTick + 1, out var b);

        float alpha = (b != null) ? Mathf.Clamp01(_tickAccum / TickDuration) : 0f; // 0~1 사이
        RenderTick(a, b, alpha);

        // End 도착 시: 재생 끝까지 오면 마무리
        if (_gotEnd && _curTick >= _playUntilTick && _tickAccum < 0.0001f)
        {
            ApplyEndImmediate(_pendingEnd);
            RenderEnd(_pendingEnd);
            _started = false;
        }
    }

    public void OnPacketReceived(Packet packet)
    {
        NetDebugDump.DumpPacketHeader(packet, packet.payload);

        switch (packet.type)
        {
            case "InitialSnapshot":
                {
                    var p = JsonConvert.DeserializeObject<InitialSnapShot>(packet.payload, JSON_INIT);
                    NetDebugDump.DumpInitialSnapshot(p);
                    ApplyInitialSnapshot(p);
                    break;
                }

            case "BattleProgress":
                {
                    var p = JsonConvert.DeserializeObject<BattleProgress>(packet.payload, JSON_RUNTIME);
                    NetDebugDump.DumpBattleProgress(p);

                    _latestServerTick = Mathf.Max(_latestServerTick, p.TotalTick);

                    if (p.BattleSnapshots != null)
                    {
                        foreach (var s in p.BattleSnapshots)
                            _buffer[s.BattleTick] = s;
                    }

                    int maxBufTick = _buffer.Count > 0 ? _buffer.Keys.Max() : 0;
                    _playUntilTick = Mathf.Max(_playUntilTick, Mathf.Max(_latestServerTick, maxBufTick));

                    if (!_started && _buffer.Count > 0)
                    {
                        _started = true;
                        _curTick = GetMinTick(_buffer);
                        _tickAccum = 0f;
                        Debug.Log($"[BattlePhase] START cur={_curTick}, latest={_latestServerTick}, playUntil={_playUntilTick}, tickDur={TickDuration:0.###}s");
                    }
                    break;
                }

            case "EndSnapshot":
                {
                    var p = JsonConvert.DeserializeObject<EndSnapshot>(packet.payload, JSON_RUNTIME);
                    NetDebugDump.DumpEndSnapshot(p);

                    _pendingEnd = p;
                    _gotEnd = true;

                    // 재생이 아직 시작 안했고 버퍼도 없으면 즉시 적용
                    if (!_started && _buffer.Count == 0)
                    {
                        ApplyEndImmediate(p);
                        RenderEnd(p);
                        _started = false;
                    }
                    break;
                }
        }
    }

    // ===== 렌더링 =====
    private void ApplyInitialSnapshot(InitialSnapShot p)
    {
        if (p?.Units == null) return;

        foreach (var unit in p.Units)
        {
            int side = unit.Player; // 초기 스냅샷은 int(0/1)

            int localIdx = _gsm.MapServerIndexToLocal(side, unit.BatchIndex);
            var row = _gsm.SafeBatchRow(side, localIdx, $"Unit[{side}:{unit.UnitIndex}]");
            if (row == null) continue;

            GameObject go = _gsm.GetUnitGO(side, unit.UnitIndex);
            if (!go) continue;

            var snap = go.GetComponent<SnapPosition>();
            if (snap) snap.enabled = false;

            go.transform.SetParent(row, false);
            go.transform.localPosition = Vector3.zero;

            var ch = go.GetComponent<Character>();
            if (ch)
            {
                ch.smoothMove = false;
                ch.moveEvnet = true;
            }

            _prev[(side, unit.UnitIndex)] = new UnitPrev
            {
                Hp = ch ? (int)ch.nowHP : 0,
                State = 0,
                Dir = 0,
                X = go.transform.position.x,
                Y = go.transform.position.z
            };
        }
    }

    private void RenderTick(BattleSnapshot from, BattleSnapshot to, float timeAlpha)
    {
        if (from?.Units == null) return;

        foreach (var unit in from.Units)
        {
            int side = SideFromBool(unit.Player); // False→0, True→1
            GameObject go = _gsm.GetUnitGO(side, unit.UnitIndex);
            if (!go) continue;

            // 서버 X/Y는 월드 좌표
            Vector3 p0World = new Vector3(unit.X, go.transform.position.y, unit.Y);
            Vector3 p1World = p0World;

            if (to != null)
            {
                var u2 = FindUnit(to.Units, unit.Player, unit.UnitIndex);
                if (u2 != null) p1World = new Vector3(u2.X, go.transform.position.y, u2.Y);
            }

            go.transform.position = Vector3.Lerp(p0World, p1World, timeAlpha);

            // 1) 서버 bool Player → int(0/1), 그걸 로컬 좌/우로 매핑
            int serverSide = unit.Player ? 1 : 0;
            side = _gsm.isFirst ? serverSide : (1 - serverSide); // 내가 second면 좌/우 뒤집힘

            // 2) 원하는 월드 방향 계산: dir==0(유지)이면 팀 기본 시선 적용
            int dir = Mathf.Clamp(unit.Direction, -1, 1);
            int desired = (dir != 0) ? dir : (side == 0 ? +1 : -1);

            // 3) 트랜스폼 스케일로 ‘정답’ 강제
            var ls = go.transform.localScale;
            float abs = Mathf.Abs(ls.x);
            ls.x = (desired >= 0) ? abs : -abs;
            go.transform.localScale = ls;

            var ch = go.GetComponent<Character>();
            if (ch)
            {
                ch.tick = from.BattleTick;
                ch.SetDir(unit.Direction);
                ch.SetHpMp(unit.Hp, unit.Mp,
                    unit.MaxHp > 0 ? unit.MaxHp : (int?)null,
                    unit.MaxMp > 0 ? unit.MaxMp : (int?)null);
                // 상태머신 호출은 잠시 보류(구 커맨드 로직과 충돌 가능)
                ch.ChangeStateInt(unit.State);
            }

            var key = (side, unit.UnitIndex);
            if (!_prev.TryGetValue(key, out var prev))
            {
                prev = new UnitPrev { Hp = unit.Hp, State = unit.State, Dir = unit.Direction, X = unit.X, Y = unit.Y };
            }

            if (LogStateChanges && prev.State != unit.State)
                Debug.Log($"[Tick {from.BattleTick}] [STATE] side={side} unit={unit.UnitIndex} {prev.State} -> {unit.State}");
            if (LogHpChanges && prev.Hp != unit.Hp)
                Debug.Log($"[Tick {from.BattleTick}] [HP]    side={side} unit={unit.UnitIndex} {prev.Hp} -> {unit.Hp} ({unit.Hp - prev.Hp:+#;-#;0})");
            if (LogDirChanges && prev.Dir != unit.Direction)
                Debug.Log($"[Tick {from.BattleTick}] [DIR]   side={side} unit={unit.UnitIndex} {prev.Dir} -> {unit.Direction}");
            if (LogMove && (Mathf.Abs(prev.X - unit.X) > 0.0001f || Mathf.Abs(prev.Y - unit.Y) > 0.0001f))
                Debug.Log($"[Tick {from.BattleTick}] [MOVE]  side={side} unit={unit.UnitIndex} ({prev.X:0.###},{prev.Y:0.###}) -> ({unit.X:0.###},{unit.Y:0.###})");

            prev.Hp = unit.Hp;
            prev.State = unit.State;
            prev.Dir = unit.Direction;
            prev.X = unit.X;
            prev.Y = unit.Y;
            _prev[key] = prev;
        }
    }

    private void ApplyEndImmediate(EndSnapshot end)
    {
        if (end?.Units == null) return;
        foreach (var u in end.Units)
        {
            int side = SideFromBool(u.Player);
            var go = _gsm.GetUnitGO(side, u.UnitIndex);
            if (!go) continue;

            go.transform.position = new Vector3(u.X, go.transform.position.y, u.Y);

            var ch = go.GetComponent<Character>();
            if (ch)
            {
                ch.tick = _playUntilTick;
                ch.SetDir(u.Direction);
                ch.SetHpMp(u.Hp, u.Mp,
                    u.MaxHp > 0 ? u.MaxHp : (int?)null,
                    u.MaxMp > 0 ? u.MaxMp : (int?)null);
            }
        }
    }

    private void RenderEnd(EndSnapshot p)
    {
        Debug.Log($"[BattlePhase] End IsWin={p.IsWin}");
    }

    // ===== 유틸 =====
    private static int GetMinTick(SortedDictionary<int, BattleSnapshot> buffer)
    {
        using var it = buffer.GetEnumerator();
        return it.MoveNext() ? it.Current.Key : 0;
    }

    private static UnitSnapshot FindUnit(List<UnitSnapshot> list, bool playerBool, int unitIndex)
        => list?.Find(x => x.Player == playerBool && x.UnitIndex == unitIndex);

    private static int SideFromBool(bool serverPlayer) => serverPlayer ? 1 : 0;

    // ===== 준비 신호 =====
    public void NotifyUnitArrived()
    {
        if (IsEveryoneSettled())
        {
            GameSession.Instance.Sender?.SendPacket("BattleStart", "{}");
            Debug.Log("[BattlePhase] All units settled → BattleStart sent");
        }
    }

    private bool IsEveryoneSettled()
    {
        if (_gsm?.FirstSecondDatas != null && _gsm.FirstSecondDatas.Length >= 2)
        {
            for (int side = 0; side < 2; side++)
            {
                var units = _gsm.FirstSecondDatas[side].units;
                if (units == null) return false;
                for (int i = 0; i < units.Count; i++)
                {
                    var ch = units[i]?.GetComponent<Character>();
                    if (ch == null || ch.moveEvnet) return false;
                }
            }
            return true;
        }
        return false;
    }
}


//public sealed class EmptyBattleEvent : IBattleEvent { }

//public sealed class DefaultBattleObject : IBattleObject
//{
//    public float CenterX { get; set; }
//    public float CenterY { get; set; }
//    public int Player { get; set; }
//    public int UnitIndex { get; set; }
//}

//public sealed class SerializedBattleEventConverter : JsonConverter<SerializedBattleEvent>
//{
//    public override SerializedBattleEvent ReadJson(JsonReader reader, Type objectType, SerializedBattleEvent existingValue, bool hasExistingValue, JsonSerializer serializer)
//    {
//        var jo = JObject.Load(reader);
//        // Type은 반드시 살려서 읽고
//        var type = jo["type"]?.ToString() ?? jo["Type"]?.ToString();

//        // 서버가 이벤트 바디를 안 보내면 그냥 빈 구현체로
//        // (혹시 'event' 객체가 와도, 여기서는 무시하거나 필요하면 일부만 읽어도 됨)
//        return new SerializedBattleEvent
//        {
//            Type = type,
//            Event = new EmptyBattleEvent()
//        };
//    }

//    public override void WriteJson(JsonWriter writer, SerializedBattleEvent value, JsonSerializer serializer)
//    {
//        // 보낼 일 없으면 안 써도 되지만, 형태를 맞추려면 이렇게
//        var jo = new JObject
//        {
//            ["Type"] = value.Type,
//            ["Event"] = null // 빈 구현체를 다시 보내야 할 일은 없다고 가정
//        };
//        jo.WriteTo(writer);
//    }
//}

//public sealed class SerializedBattleObjectConverter : JsonConverter<SerializedBattleObject>
//{
//    public override SerializedBattleObject ReadJson(JsonReader reader, Type objectType, SerializedBattleObject existingValue, bool hasExistingValue, JsonSerializer serializer)
//    {
//        var jo = JObject.Load(reader);
//        var type = jo["type"]?.ToString() ?? jo["Type"]?.ToString();

//        // 오브젝트 바디가 'event' 안에 있든, 평탄화되어 오든 최대한 유연하게 대응
//        var evTok = jo["event"] ?? jo["Event"] ?? jo;

//        var obj = new DefaultBattleObject
//        {
//            CenterX = evTok.Value<float?>("centerX") ?? 0f,
//            CenterY = evTok.Value<float?>("centerY") ?? 0f,
//            Player = evTok.Value<int?>("player") ?? 0,
//            UnitIndex = evTok.Value<int?>("unitIndex") ?? 0
//        };

//        return new SerializedBattleObject { Type = type, Event = obj };
//    }

//    public override void WriteJson(JsonWriter writer, SerializedBattleObject value, JsonSerializer serializer)
//    {
//        var jo = new JObject
//        {
//            ["Type"] = value.Type,
//            ["Event"] = value.Event != null ? JToken.FromObject(value.Event, serializer) : null
//        };
//        jo.WriteTo(writer);
//    }
//}

//public interface IBattleReady
//{
//    void NotifyUnitArrived();
//}





//public class BattlePhase : IGameScenePhase, IBattleReady
//{
//    private EndSnapshot _pendingEnd;

//    [SerializeField] bool LogStateChanges = true;
//    [SerializeField] bool LogHpChanges = true;
//    [SerializeField] bool LogMove = true; // 너무 많으면 false
//    [SerializeField] bool LogDirChanges = true;

//    private bool _gotEnd;
//    private int _playUntilTick;
//    private struct UnitPrev
//    {
//        public int Hp;
//        public int State;
//        public int Dir;
//        public float X, Y;
//    }
//    private static readonly JsonSerializerSettings JSON_INIT = new()
//    {
//        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
//    };

//    private static readonly JsonSerializerSettings JSON_RUNTIME = new()
//    {
//        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
//        Converters = new List<JsonConverter>
//        {
//            new SerializedBattleEventConverter(),
//            new SerializedBattleObjectConverter()
//        }
//    };
//    GameSceneManager _gsm;
//    UIManager _ui;

//    [SerializeField]
//    [Header("Ticks")]
//    bool _started;
//    int _curTick;
//    int _latestServerTick;
//    const int TargetLatencyTick = 3;

//    [SerializeField]
//    [Header("Tick to Snapshot buffer")]
//    readonly SortedDictionary<int, BattleSnapshot> _buffer = new();
//    readonly Dictionary<(int side, int unitIdx), UnitPrev> _prev = new();

//    //[Header("Mapping")]
//    //readonly Dictionary<(int playerID, int unitIdx), UnitMeta> unitMeta = new();

//    private static readonly JsonSerializerSettings JSON = new JsonSerializerSettings
//    {
//        ContractResolver = new CamelCasePropertyNamesContractResolver()
//    };

//    public void Enter(GameSceneManager gsm)
//    {
//        _gsm = gsm;
//        _ui = gsm.uIManager;

//        /* tick init */
//        _started = false;
//        _curTick = 0;
//        _latestServerTick = 0;
//        _buffer.Clear();
//        //unitMeta.Clear();

//        /* ui init */
//        Camera.main.GetComponent<SmoothFollowCamera>()?.CoroutineCamera(Vector3.zero);
//        _ui?.ReturnCurrentCanvas()?.SetActive(false);

//        Debug.Log($"[BattlePhase]: Enter 진입 - 스냅샷 받아오기 준비 중");

//    }

//    public void UpdateLoop()
//    {
//        if (!_started && _buffer.Count > 0)
//        {
//            _started = true;
//            _curTick = GetMinTick(_buffer);
//            Debug.Log($"[BattlePhase] LATE-START curTick={_curTick}, buf={_buffer.Count}, latest={_latestServerTick}");
//        }

//        if (!_started) return;

//        /* 서버틱과 목표지연 유지 */
//        int targetTick = Mathf.Max(0, _latestServerTick - TargetLatencyTick);
//        if (_curTick < targetTick)
//        {
//            _curTick++;
//        }

//        if (!_buffer.TryGetValue(_curTick, out var a)) return;
//        if (_buffer.TryGetValue(_curTick + 1, out var b))
//        {
//            float alpha = .5f;
//            RenderTick(a, b, alpha);
//        }
//        else
//        {
//            RenderTick(a, null, 0);
//        }

//        _curTick = a.BattleTick;

//        if (_gotEnd && _curTick >= _playUntilTick)
//        {
//            ApplyEndImmediate(_pendingEnd);
//            RenderEnd(_pendingEnd);
//            _started = false;
//        }
//    }
//    private void ApplyEndImmediate(EndSnapshot end)
//    {
//        if (end?.Units == null) return;
//        foreach (var u in end.Units)
//        {
//            int side = SideFromBool(u.Player);
//            var go = _gsm.GetUnitGO(side, u.UnitIndex);
//            if (!go) continue;

//            var drv = go.GetComponent<CharacterSnapshotDriver>();
//            if (drv)
//            {
//                var pos = new Vector3(u.X, go.transform.position.y, u.Y);
//                drv.Apply(u, pos);
//            }
//        }
//    }
//    public void OnPacketReceived(Packet packet)
//    {
//        switch (packet.type)
//        {
//            case "InitialSnapshot":
//                {
//                    var p = JsonConvert.DeserializeObject<InitialSnapShot>(packet.payload, JSON_INIT);
//                    NetDebugDump.DumpInitialSnapshot(p); // ★ 덤프
//                    //_started = true;
//                    ApplyInitialSnapshot(p);
//                    break;
//                }

//            case "BattleProgress":
//                {
//                    var p = JsonConvert.DeserializeObject<BattleProgress>(packet.payload, JSON_RUNTIME);
//                    NetDebugDump.DumpBattleProgress(p);  // ★ 덤프

//                    _latestServerTick = Mathf.Max(_latestServerTick, p.TotalTick);

//                    foreach (var s in p.BattleSnapshots)
//                    {
//                        _buffer[s.BattleTick] = s;
//                    }


//                    int maxBuf = _buffer.Count > 0 ? _buffer.Keys.Max() : 0;
//                    _playUntilTick = Mathf.Max(_playUntilTick, Mathf.Max(_latestServerTick, maxBuf));

//                    if (!_started && _buffer.Count > 0)
//                    {
//                        _started = true;
//                        _curTick = GetMinTick(_buffer);
//                        Debug.Log($"[BattleState] 재생 시작 curTick={_curTick}, latest={_latestServerTick}");
//                    }
//                    break;
//                }

//            case "EndSnapshot":
//                {
//                    var p = JsonConvert.DeserializeObject<EndSnapshot>(packet.payload, JSON_RUNTIME);
//                    NetDebugDump.DumpEndSnapshot(p);     // ★ 덤프
//                    _pendingEnd = p;
//                    _gotEnd = true;
//                    if (!_started && _buffer.Count == 0)
//                    {
//                        ApplyEndImmediate(p);
//                        RenderEnd(p);
//                        _started = false;
//                    }

//                    break;
//                }

//        }
//    }

//    private void RenderEnd(EndSnapshot p)
//    {
//        Debug.Log($"[BattleState] End IsWin={p.IsWin}");
//    }

//    private int GetMinTick(SortedDictionary<int, BattleSnapshot> buffer)
//    {
//        using var it = buffer.GetEnumerator();
//        return it.MoveNext() ? it.Current.Key : 0;
//    }

//    private void ApplyInitialSnapshot(InitialSnapShot p)
//    {
//        foreach (var unit in p.Units)
//        {

//            int side = unit.Player;

//            int localIdx = _gsm.MapServerIndexToLocal(side, unit.BatchIndex);

//            var parent = _gsm.FirstSecondDatas[side].batchPos;
//            Debug.Log($"[InitPlace] side={side}, unit={unit.UnitIndex}, serverBatch={unit.BatchIndex} -> local={localIdx}, parent='{parent?.name}', childCount={parent?.childCount}");

//            var row = _gsm.SafeBatchRow(side, localIdx, $"Unit[{side}:{unit.UnitIndex}]");
//            if (row == null) continue;

//            GameObject go = _gsm.GetUnitGO(side, unit.UnitIndex);
//            if (go == null) continue;

//            go.GetComponent<SnapPosition>().enabled = false;
//            go.transform.SetParent(row, false);
//            go.transform.localPosition = Vector3.zero;

//            var ch = go.GetComponent<Character>();
//            ch.smoothMove = false;
//            ch.moveEvnet = true;

//            _prev[(side, unit.UnitIndex)] = new UnitPrev
//            {
//                Hp = (int)ch.nowHP,
//                State = 0,
//                Dir = 0,
//                X = go.transform.localPosition.x,
//                Y = go.transform.localPosition.z
//            };
//        }
//    }

//    private void RenderTick(BattleSnapshot from, BattleSnapshot to, float time)
//    {
//        //Debug.Log("In?????????");
//        foreach (var unit in from.Units)
//        {
//            int side = SideFromBool(unit.Player);
//            GameObject go = _gsm.GetUnitGO(side, unit.UnitIndex);
//            if (go == null) continue;

//            // 서버 X/Y는 월드 기준으로 온다고 가정
//            Vector3 p0World = new Vector3(unit.X, go.transform.position.y, unit.Y);
//            Vector3 p1World = p0World;

//            if (to != null)
//            {
//                UnitSnapshot u2 = FindUnit(to.Units, side, unit.UnitIndex);
//                if (u2 != null)
//                    p1World = new Vector3(u2.X, go.transform.position.y, u2.Y);
//            }

//            // ✅ 월드 좌표로 보간
//            go.transform.position = Vector3.Lerp(p0World, p1World, time);

//            var ch = go.GetComponent<Character>();
//            ch.tick = from.BattleTick;
//            ch.SetDir(unit.Direction);
//            ch.ChangeStateInt(unit.State);
//            ch.SetHpMp(unit.Hp, unit.Mp,
//                unit.MaxHp > 0 ? unit.MaxHp : (int?)null,
//                unit.MaxMp > 0 ? unit.MaxMp : (int?)null);

//            var key = (side, unit.UnitIndex);
//            if (!_prev.TryGetValue(key, out var prev))
//            {
//                _prev[key] = prev = new UnitPrev
//                {
//                    Hp = unit.Hp,
//                    State = unit.State,
//                    Dir = unit.Direction,
//                    X = unit.X,
//                    Y = unit.Y,
//                };
//            }

//            if (LogStateChanges && prev.State != unit.State)
//            {
//                Debug.Log($"[Tick {from.BattleTick}] [State] side={side} unit={unit.UnitIndex} {prev.State} -> {unit.State}");
//            }
//            if (LogHpChanges && prev.Hp != unit.Hp)
//            {
//                int delta = unit.Hp - prev.Hp;
//                Debug.Log($"[Tick {from.BattleTick}] [HP]    side={side} unit={unit.UnitIndex} {prev.Hp} -> {unit.Hp} ({delta:+#;-#;0})");
//            }
//            if (LogDirChanges && prev.Dir != unit.Direction)
//            {
//                Debug.Log($"[Tick {from.BattleTick}] [DIR]   side={side} unit={unit.UnitIndex} {prev.Dir} -> {unit.Direction}");
//            }
//            if (LogMove && (Mathf.Abs(prev.X - unit.X) > 0.0001f || Mathf.Abs(prev.Y - unit.Y) > 0.0001f))
//            {
//                Debug.Log($"[Tick {from.BattleTick}] [MOVE]  side={side} unit={unit.UnitIndex} ({prev.X:0.###},{prev.Y:0.###}) -> ({unit.X:0.###},{unit.Y:0.###})");
//            }

//            if (prev.State != unit.State && unit.State == 2)
//            {
//                //OnAttackStartDerived(side, unit.UnitIndex);
//            }

//            if (unit.Hp < prev.Hp)
//            {
//                int dmg = prev.Hp - unit.Hp;
//                //OnDamagedDerived(side, unit.UnitIndex, dmg);
//            }

//            if ((prev.Hp > 0 && unit.Hp <= 0) || unit.State == 11)
//            {
//                //OnDeathDerived(side, unit.UnitIndex);
//            }

//            prev.Hp = unit.Hp;
//            prev.State = unit.State;
//            prev.Dir = unit.Direction;
//            prev.X = unit.X;
//            prev.Y = unit.Y;
//            _prev[key] = prev;
//        }
//    }

//    public void NotifyUnitArrived()
//    {
//        // 모든 유닛이 moveEvnet == false 면 전투 시작 신호(필요한 경우)
//        if (IsEveryoneSettled())
//        {
//            // 서버가 기다리는 프로토콜이면 유지
//            GameSession.Instance.Sender?.SendPacket("BattleStart", "{}");
//            Debug.Log("[BattleState] All units settled → BattleStart sent");
//        }
//    }

//    private bool IsEveryoneSettled()
//    {
//        // FirstSecondDatas를 쓰는 경우
//        if (_gsm?.FirstSecondDatas != null && _gsm.FirstSecondDatas.Length >= 2)
//        {
//            for (int side = 0; side < 2; side++)
//            {
//                var units = _gsm.FirstSecondDatas[side].units;
//                if (units == null) return false;
//                for (int i = 0; i < units.Count; i++)
//                {
//                    var ch = units[i]?.GetComponent<Character>();
//                    if (ch == null || ch.moveEvnet) return false;
//                }
//            }
//            return true;
//        }

//        // myData/enemyData를 쓰는 경우 (프로젝트에 맞게 둘 중 하나만 남겨도 됨)
//        if (_gsm?.myData?.units == null || _gsm?.enemyData?.units == null) return false;
//        foreach (var go in _gsm.myData.units) if (go?.GetComponent<Character>()?.moveEvnet == true) return false;
//        foreach (var go in _gsm.enemyData.units) if (go?.GetComponent<Character>()?.moveEvnet == true) return false;
//        return true;
//    }

//    private void OnDeathDerived(int side, int unitIndex)
//    {
//        throw new NotImplementedException();
//    }

//    private void OnDamagedDerived(int side, int unitIndex, int dmg)
//    {
//        throw new NotImplementedException();
//    }

//    private void OnAttackStartDerived(int side, int unitIndex)
//    {
//        throw new NotImplementedException();
//    }
//    private UnitSnapshot FindUnit(List<UnitSnapshot> list, int side, int unitIndex)
//        => list.Find(x => SideFromBool(x.Player) == side && x.UnitIndex == unitIndex);

//    /// <summary>
//    /// 패킷의 bool, int로 들어오는 플레이어 데이터 평탄화? 맞춰 넣기
//    /// </summary>
//    static int SideFromBool(bool isFirstSide)
//    {
//        if (isFirstSide) return 0;
//        else return 1;
//    }
//    // 기존코드

//    /*
//   private string playerId;
//   private GameSceneManager myManager;
//   private UIManager uIManager;
//   public void Enter(GameSceneManager gameSceneManager)
//   {
//       myManager = gameSceneManager;
//       uIManager = gameSceneManager.uIManager;
//       Debug.Log("배틀 상태 진입");
//       GameSession.Instance.Sender.SendPacket("PhaseStart", "{}");
//   }

//   public void UpdateLoop()
//   {

//   }
//   /*

//   public void OnPacketReceived(Packet packet)
//   {
//       switch (packet.type)
//       {
//           case "FirstUnitMove":
//           {
//               var data = JsonConvert.DeserializeObject<UnitsIndexespayload>(packet.payload,
//                   JsonSettings.CamelCaseSettings);
//               MoveToBattleField(data);
//               break;
//           }
//           case "UnitCommandGroup":
//           {
//               var data = JsonConvert.DeserializeObject<UnitCommandListpayload>(
//                   packet.payload, new JsonSerializerSettings
//                   {
//                       Converters = new List<JsonConverter> { new CommandConverter() },
//                       ContractResolver = new CamelCasePropertyNamesContractResolver()
//                   });
//               UpdateCommand(data,packet.tick);
//               break;
//           }
//           case "UnitArrived":
//           {
//               var data = JsonConvert.DeserializeObject<Arrivepayload>(packet.payload,
//                   JsonSettings.CamelCaseSettings);
//               UpdateArrive(data);
//               break;
//           }
//       }
//   }

//   private void UpdateArrive(Arrivepayload data)
//   {
//       myManager.GetRowPositions().GetChild(data.arriveIndex).GetChild(0).transform.localPosition = Vector3.zero;
//   }

// private void UpdateCommand(UnitCommandListpayload data, int tick)
//{
//   foreach (var command in data.firstCommands)
//   {
//       switch (command)
//       {
//           case UnitMoveCommand move:
//           {
//               foreach (var unit in myManager.FirstSecondDatas[0].units)
//               {
//                   if (unit.transform.parent.GetSiblingIndex() == move.nowIndex)
//                   {
//                       var character = unit.GetComponent<Character>();
//                       character.tick = tick;
//                       character.data = move;
//                       character.ChangeState("Move");
//                   }
//               }
//               break;
//           }
//           case UnitAttackCommand attack:
//           {
//               foreach (var unit in myManager.FirstSecondDatas[0].units)
//               {
//                   if (unit.transform.parent.GetSiblingIndex() == attack.attackIndex)
//                   {
//                       unit.transform.parent = myManager.GetRowPositions().GetChild(attack.attackIndex);
//                       var character = unit.GetComponent<Character>();
//                       character.tick = tick;
//                       character.data = attack;
//                       character.ChangeState("Attack");

//                       Debug.Log($"[공격] {attack.attackIndex} -> {attack.hitIndex} (남은 HP: {attack.nowHP})");
//                   }
//               }
//               break;
//           }
//       }
//   }

//   foreach (var command in data.secondCommands)
//   {
//       switch (command)
//       {
//           case UnitMoveCommand move:
//           {
//               foreach (var unit in myManager.FirstSecondDatas[1].units)
//               {
//                   if (unit.transform.parent.GetSiblingIndex() == move.nowIndex)
//                   {
//                       var character = unit.GetComponent<Character>();
//                       character.tick = tick;
//                       character.data = move;
//                       character.ChangeState("Move");
//                   }
//               }
//               break;
//           }
//           case UnitAttackCommand attack:
//           {
//               foreach (var unit in myManager.FirstSecondDatas[1].units)
//               {
//                   if (unit.transform.parent.GetSiblingIndex() == attack.attackIndex)
//                   {
//                       unit.transform.parent = myManager.GetRowPositions().GetChild(attack.attackIndex);
//                       var character = unit.GetComponent<Character>();
//                       character.tick = tick;
//                       character.data = attack;
//                       character.ChangeState("Attack");

//                       Debug.Log($"[공격] {attack.attackIndex} -> {attack.hitIndex} (남은 HP: {attack.nowHP})");
//                   }
//               }
//               break;
//           }
//       }
//   }
//}

//   void MoveToBattleField(UnitsIndexespayload data)
//   {
//       Camera.main.GetComponent<SmoothFollowCamera>().CoroutineCamera(Vector3.zero);
//       List<int> myTargetIndex;
//       List<int> enemyTargetIndex;
//       if (data.isFirst)
//       {
//           myTargetIndex = data.firstPlayerIndex;
//           enemyTargetIndex = data.secondPlayerIndex;
//       }
//       else
//       {
//           myTargetIndex = data.secondPlayerIndex;
//           enemyTargetIndex = data.firstPlayerIndex;
//       }


//       for (int i = 0; i < myManager.myData.units.Count; i++)
//       {
//           myManager.myData.units[i].GetComponent<SnapPosition>().enabled = false;
//           myManager.myData.units[i].transform.parent = myManager.GetRowPositions().GetChild(myTargetIndex[i]);
//           myManager.myData.units[i].GetComponent<Character>().smoothMove = false;
//           myManager.myData.units[i].GetComponent<Character>().moveEvnet = true;
//           // unit.GetComponent<Character>().moveTarget=??
//       }

//       for (int i = 0; i < myManager.enemyData.units.Count; i++)
//       {
//           myManager.enemyData.units[i].transform.parent = myManager.GetRowPositions().GetChild(enemyTargetIndex[i]);
//           myManager.enemyData.units[i].GetComponent<Character>().smoothMove = false;
//           myManager.enemyData.units[i].GetComponent<Character>().moveEvnet = true;
//       }

//       uIManager.ReturnCurrentCanvas().SetActive(false);
//   }

//   public void CheckReadyToBattle()
//   {
//       if (myManager.myData.units.All(u => !u.GetComponent<Character>().moveEvnet))
//       {
//           if (myManager.enemyData.units.All(u => !u.GetComponent<Character>().moveEvnet))
//           {
//               GameSession.Instance.Sender.SendPacket("BattleStart", "{}");
//           }
//       }
//   }*/
//}
