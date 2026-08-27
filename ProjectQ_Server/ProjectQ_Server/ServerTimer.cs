using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace ProjectQ_Server
{
    public class ServerTimer
    {
        private long startTick;
        private long lastSyncedTick;
        private float duration;
        private bool useBroadCast;
        public Action onTimerEnd;

        public ServerTimer(GameRoom server, float duration, bool useBroadCast)
        {
            this.duration = duration;
            this.startTick = server.serverTick;  // 초기 틱 저장
            this.lastSyncedTick = server.serverTick;
            this.useBroadCast = useBroadCast;
            BroadcastRemainingTime(server,duration);
        }

        public void UpdateTimer(GameRoom server)
        {
            float elapsed = (server.serverTick - startTick) * GameConstants.TICK_DELTA_SECONDS;
            float remaining = duration - elapsed;

            if ((server.serverTick - lastSyncedTick) * GameConstants.TICK_DELTA_SECONDS >= 1f)
            {
                BroadcastRemainingTime(server, Math.Max(remaining, 0f));
                lastSyncedTick = server.serverTick;
            }

            if (remaining <= 0f)
            {
                onTimerEnd?.Invoke();
            }
        }

        private void BroadcastRemainingTime(GameRoom server, float time)
        {
            if (useBroadCast)
            {
                var payload = new SelectionTimePayload
                {
                    durationTime = duration,
                    remainingTime = time
                };

                var packet = new Packet
                {
                    type = "TimerUpdate",
                    senderId = "Server",
                    payload = System.Text.Json.JsonSerializer.Serialize(payload),
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    tick = server.serverTick
                };

                foreach (var client in server.Clients.Values)
                {
                    server.SendPacketAsync(packet, client);
                }
            }
           Console.WriteLine($"TimerUpdate : {Math.Round(time, 2)}초 남음");
        }

        public void SetOnTimerEnd(Action callback)
        {
            onTimerEnd = callback;
        }
    }
}
