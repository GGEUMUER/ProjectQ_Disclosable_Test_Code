using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

public class TcpClientSender
{
    private TcpClient tcpClient;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isRunning;

    public Action<Packet> OnPacketReceived;
    public TcpClientSender(string serverIP, int serverPort)
    {
        tcpClient = new TcpClient();
        tcpClient.Connect(serverIP, serverPort);
        stream = tcpClient.GetStream();
        isRunning = true;

        receiveThread = new Thread(ReceiveLoop);
        receiveThread.IsBackground = true;
        receiveThread.Start();
        
        Debug.Log($"TCP 연결됨 {serverIP}:{serverPort}");
    }
    /// <summary>
    /// 백그라인드에서 도는 수신 루프
    /// </summary>
    private void ReceiveLoop()
    {
        byte[] buffer = new byte[4096];
        StringBuilder sb = new StringBuilder();

        while (isRunning)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Debug.LogWarning("서버 연결 종료됨");
                    break;
                }

                sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                string allData = sb.ToString();
                string[] packets = allData.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                if (!allData.EndsWith("\n"))
                {
                    // 마지막 incomplete 데이터만 남기기
                    sb.Clear();
                    sb.Append(packets[^1]);
                    packets = packets[..^1]; // 마지막 제외
                }
                else
                {
                    sb.Clear();
                }

                foreach (string json in packets)
                {
                    try
                    {
                        // Newtonsoft로 받아오기
                        Packet packet = JsonConvert.DeserializeObject<Packet>(json);
                        // 기존 코드, 여기서 에러가 발생함. Newtonesoft.Json으로 보내고, 유니티 내장으로 받아 문제가 생김
                        //Packet packet = JsonUtility.FromJson<Packet>(json);
                        MainThreadDispatcher.Enqueue(() => OnPacketReceived?.Invoke(packet));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"JSON 파싱 실패: {ex.Message}\n데이터: {json}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"TCP 수신 오류: {e.Message}");
                break;
            }
        }
        Close();
    }

    public void SendRawStringPayloadPacket(string type, string rawString)
    {
        var packet = new Packet
        {
            type = type,
            payload = JsonConvert.SerializeObject(rawString), // "MyID1" → "\"MyID1\""
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            tick = 0
        };

        string json = JsonConvert.SerializeObject(packet, new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver() // PascalCase 유지
        }) + "\n";

        byte[] bytes = Encoding.UTF8.GetBytes(json);

        try
        {
            stream.Write(bytes, 0, bytes.Length);
            Debug.Log("TCP 패킷 전송됨: " + json);
        }
        catch (Exception e)
        {
            Debug.LogError($"전송 오류: {e.Message}");
        }
    }

    /// <summary>
    /// 패킷 송신
    /// </summary>
    public void SendPacket(string type, object data)
    {
        var packet = new Packet
        {
            type = type,
            //명세에 안 쓴다고 안내 되어 있음. 일단 뺴보기
            //senderId = GameSession.Instance.PlayerId,
            //payload = data,
            
            payload = JsonConvert.SerializeObject(data, JsonSettings.CamelCaseSettings),
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            tick = 0
        };

        string json = JsonConvert.SerializeObject(packet, JsonSettings.CamelCaseSettings) +"\n";

        Debug.Log(json);
        // 지금 여기서도 유니티 json 사용 중, 일단 빼보기
        // string json = JsonUtility.ToJson(packet) + "\n"; // ✅ \n 추가
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        try
        {
            stream.Write(bytes, 0, bytes.Length);
            Debug.Log("TCP 패킷 전송됨: " + json);
        }
        catch (Exception e)
        {
            Debug.LogError($"전송 오류: {e.Message}");
        }
    }

    public void Close()
    {
        isRunning = false;
        stream?.Close();
        tcpClient?.Close();

        if (receiveThread != null)
        {
            receiveThread.Join(1000);
        }
    }
}
