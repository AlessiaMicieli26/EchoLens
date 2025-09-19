using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class ServerDiscovery
{
    private const int DISCOVERY_PORT = 8888;
    private const string DISCOVERY_REQUEST = "DISCOVER_SERVER";

    public static async Task<string> DiscoverServerIP()
    {
        using (UdpClient client = new UdpClient())
        {
            client.EnableBroadcast = true;

            byte[] requestData = Encoding.UTF8.GetBytes(DISCOVERY_REQUEST);
            IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, DISCOVERY_PORT);
            await client.SendAsync(requestData, requestData.Length, broadcastEndPoint);

            var task = client.ReceiveAsync();
            if (await Task.WhenAny(task, Task.Delay(3000)) == task) // timeout 3s
            {
                var result = task.Result;
                string response = Encoding.UTF8.GetString(result.Buffer);
                if (response.StartsWith("SERVER_IP:"))
                {
                    string ip = response.Replace("SERVER_IP:", "").Trim();
                    Debug.Log($"📡 Server scoperto: {ip}");
                    return ip;
                }
            }
            else
            {
                Debug.LogWarning("⏱ Nessuna risposta dal server UDP.");
            }
        }
        return null;
    }
}