using UnityEngine;
using System.Text;
using NativeWebSocket;
using System.Threading.Tasks;
using PassthroughCameraSamples; // Presumo sia lì dentro ServerDiscovery

public class TestPingWS : MonoBehaviour
{
    private WebSocket websocket;

    private async void Start()
    {
        Debug.Log("🔍 Inizio ricerca server...");
        string serverIP = await ServerDiscovery.DiscoverServerIP();

        if (string.IsNullOrEmpty(serverIP))
        {
            Debug.LogError("❌ Server non trovato nella rete locale.");
            return;
        }

        string serverUrl = $"ws://{serverIP}:5000";
        Debug.Log($"🌐 Connessione a: {serverUrl}");

        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () =>
        {
            Debug.Log("✅ Connessione test aperta!");
            SendTestMessage();
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError("❌ Errore: " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("🔌 Connessione chiusa con codice: " + e);
        };

        websocket.OnMessage += (bytes) =>
        {
            string msg = Encoding.UTF8.GetString(bytes);
            Debug.Log("📩 Risposta dal server: " + msg);
        };

        await websocket.Connect();
    }

    private async void SendTestMessage()
    {
        if (websocket.State == WebSocketState.Open)
        {
            string message = "Ciao dal visore Unity!";
            await websocket.SendText(message);
            Debug.Log("➡ Inviato al server: " + message);
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
            await websocket.Close();
    }
}