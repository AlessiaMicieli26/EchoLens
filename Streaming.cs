//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using Meta.WitAi.TTS.Utilities;
//using NativeWebSocket;
//using PassthroughCameraSamples;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;
//using Debug = UnityEngine.Debug;

//public class Streaming : MonoBehaviour
//{
//    [SerializeField] private WebCamTextureManager camManager;

//    [Header("Server Settings")]
//    [SerializeField] private GameObject audio;

//    [Header("UI")]
//    [SerializeField] private TextMeshProUGUI serverResponseText;
//    [SerializeField] private TextMeshProUGUI statusText;

//    [Header("TTS - Configurazione Migliorata")]
//    [SerializeField] private TTSSpeaker Speaker;
//    [SerializeField] private Transform TTS;
//    [SerializeField] private float ttsDelay = 0.3f; // Ridotto per maggiore reattività
//    [SerializeField] private bool skipServerTTS = false;

//    [Header("Cattura Frame")]
//    [SerializeField] private int framesToCapture = 8;
//    [SerializeField] private float frameInterval = 0.2f; // Intervallo tra i frame (200ms)

//    private WebCamTexture webcamTexture;
//    private Texture2D reusableFrame;
//    private WebSocket websocket;

//    private string latestServerResponse = null;
//    private bool hasNewResponse = false;
//    private TaskCompletionSource<string> responseAwaiter;

//    [SerializeField] private GameObject Canvas_menu;
//    [SerializeField] private GameObject Canvas_credits;

//    // Hand tracking per gesture detection
//    [Header("Hand Tracking")]
//    [SerializeField] private OVRHand leftHand;
//    [SerializeField] private OVRHand rightHand;
//    [SerializeField] private float pinchThreshold = 0.7f;

//    private bool wasLeftPinching = false;
//    private bool wasRightPinching = false;

//    // Cooldown per evitare catture multiple
//    private float lastCaptureTime = 0f;
//    private float captureCooldown = 2f;

//    // TTS Management Migliorato
//    private bool isSpeaking = false;
//    private bool isCapturingFrames = false; // Flag per prevenire catture multiple
//    private Coroutine currentTTSCoroutine = null;
//    private Queue<string> ttsQueue = new Queue<string>();
//    private string currentTTSText = "";

//    private async void Start()
//    {
//        if (camManager == null)
//        {
//            Debug.LogError("WebCamTextureManager non assegnato!");
//            return;
//        }

//        // Inizializza TTS Speaker
//        InitializeTTSSpeaker();

//        UpdateStatus("Ricerca server...");

//        Debug.Log("🔍 Inizio discovery UDP...");
//        string serverIP = await ServerDiscovery.DiscoverServerIP();

//        if (string.IsNullOrEmpty(serverIP))
//        {
//            Debug.LogError("❌ Server non trovato nella rete locale.");
//            UpdateStatus("❌ Server non trovato");
//            SpeakImmediate("Server non trovato nella rete");
//            return;
//        }

//        string serverUrl = $"ws://{serverIP}:5001";
//        Debug.Log($"🌐 Connessione a: {serverUrl}");

//        Debug.Log("🎥 Avvio camera");
//        StartCoroutine(InitAndDisplay());

//        websocket = new WebSocket(serverUrl);

//        websocket.OnOpen += () =>
//        {
//            Debug.Log("✅ Connessione aperta!");
//            UpdateStatus("✅ Connesso - Premi A per 8 frame");
//            SpeakImmediate("Connesso al server. Sistema pronto per cattura multipla.");
//        };

//        websocket.OnError += (e) =>
//        {
//            Debug.LogError("❌ Errore: " + e);
//            UpdateStatus("❌ Errore connessione");
//            SpeakImmediate("Errore di connessione");
//        };

//        websocket.OnClose += (e) =>
//        {
//            Debug.Log("🔌 Connessione chiusa con codice: " + e);
//            UpdateStatus("🔌 Disconnesso");
//            SpeakImmediate("Connessione chiusa");
//        };

//        websocket.OnMessage += (bytes) =>
//        {
//            string message = Encoding.UTF8.GetString(bytes);
//            Debug.Log("📩 Ricevuto dal server: " + message);

//            latestServerResponse = message;
//            hasNewResponse = true;

//            responseAwaiter?.TrySetResult(message);
//        };

//        await websocket.Connect();
//    }

//    private void InitializeTTSSpeaker()
//    {
//        if (TTS != null)
//        {
//            Speaker = TTS.Find("TTSSpeaker")?.GetComponent<TTSSpeaker>();
//        }

//        if (Speaker == null)
//        {
//            Debug.LogWarning("TTSSpeaker non trovato! Verificare la configurazione.");
//            return;
//        }

//        Debug.Log("🔊 TTSSpeaker configurato correttamente");
//    }

//    private void UpdateStatus(string status)
//    {
//        if (statusText != null)
//        {
//            statusText.text = status;
//        }
//        Debug.Log($"[Status] {status}");
//    }

//    public void Start_Streaming()
//    {
//        SpeakImmediate("Sistema di analisi immagini attivo. Cattura automatica 8 frame.");
//        Canvas_menu.SetActive(false);
//        UpdateStatus("🎯 Premi A per catturare 8 frame");
//        Debug.Log("Menu nascosto, app pronta per cattura automatica 8 frame.");
//    }

//    public void Credits()
//    {
//        Canvas_credits.SetActive(true);
//    }

//    private IEnumerator InitAndDisplay()
//    {
//        while (camManager.WebCamTexture == null)
//            yield return null;

//        webcamTexture = camManager.WebCamTexture;
//        webcamTexture.Play();
//        Debug.Log("📹 Stream camera avviato.");

//        reusableFrame = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGB24, false);
//    }

//    private byte[] CaptureCurrentFrame()
//    {
//        if (webcamTexture == null || !webcamTexture.isPlaying)
//        {
//            Debug.LogWarning("⚠️ Camera non disponibile");
//            return null;
//        }

//        reusableFrame.SetPixels(webcamTexture.GetPixels());
//        reusableFrame.Apply();

//        byte[] imageBytes = reusableFrame.EncodeToJPG(85);
//        Debug.Log($"📸 Frame catturato: {imageBytes.Length} bytes");

//        return imageBytes;
//    }

//    private async Task CaptureAndSend8Frames()
//    {
//        if (websocket == null || websocket.State != WebSocketState.Open)
//        {
//            Debug.LogWarning("⚠️ Connessione WebSocket non disponibile.");
//            UpdateStatus("⚠️ Non connesso");
//            SpeakImmediate("Non connesso al server");
//            return;
//        }

//        // Verifica cooldown
//        if (Time.time - lastCaptureTime < captureCooldown)
//        {
//            float remainingTime = captureCooldown - (Time.time - lastCaptureTime);
//            Debug.Log($"⏱️ Attendi {remainingTime:F1}s prima della prossima cattura");
//            UpdateStatus($"⏱️ Attendi {remainingTime:F1}s");
//            return;
//        }

//        // Previeni catture multiple simultanamente
//        if (isCapturingFrames)
//        {
//            Debug.Log("⚠️ Cattura già in corso...");
//            UpdateStatus("⚠️ Cattura in corso...");
//            return;
//        }

//        isCapturingFrames = true;
//        lastCaptureTime = Time.time;

//        // Ferma TTS corrente prima di nuova cattura
//        StopCurrentTTS();

//        try
//        {
//            Debug.Log($"📸 Inizio cattura sequenziale di {framesToCapture} frame...");
//            UpdateStatus($"📸 Catturando {framesToCapture} frame...");

//            for (int i = 0; i < framesToCapture; i++)
//            {
//                UpdateStatus($"📸 Frame {i + 1}/{framesToCapture}...");

//                byte[] frame = CaptureCurrentFrame();
//                if (frame == null)
//                {
//                    Debug.LogError($"❌ Errore cattura frame {i + 1}");
//                    continue;
//                }

//                Debug.Log($"📤 Invio frame {i + 1}/{framesToCapture} ({frame.Length} bytes)");

//                try
//                {
//                    await websocket.Send(frame);
//                    Debug.Log($"✅ Frame {i + 1} inviato con successo");
//                }
//                catch (System.Exception e)
//                {
//                    Debug.LogError($"❌ Errore invio frame {i + 1}: {e}");
//                }

//                // Attesa tra i frame (tranne per l'ultimo)
//                if (i < framesToCapture - 1)
//                {
//                    await Task.Delay((int)(frameInterval * 1000));
//                }
//            }

//            Debug.Log("🎉 Tutti i frame inviati, attendo risposta dal server...");
//            UpdateStatus("🔍 Analisi in corso...");

//            // Aspetta risposta dal server
//            responseAwaiter = new TaskCompletionSource<string>();
//            string risposta = await responseAwaiter.Task;

//            Debug.Log("🎉 Analisi completata: " + risposta.Substring(0, Mathf.Min(100, risposta.Length)));
//            UpdateStatus("✅ Analisi completata");

//            if (serverResponseText != null)
//                serverResponseText.text = risposta;

//            // Gestione TTS migliorata
//            ProcessServerResponse(risposta);
//        }
//        catch (System.Exception e)
//        {
//            Debug.LogError($"❌ Errore durante cattura 8 frame: {e}");
//            UpdateStatus("❌ Errore cattura");
//            SpeakImmediate("Errore durante la cattura dei frame");
//        }
//        finally
//        {
//            isCapturingFrames = false;
//            UpdateStatus("🎯 Premi A per catturare 8 frame");
//        }
//    }

//    private void ProcessServerResponse(string response)
//    {
//        if (string.IsNullOrEmpty(response))
//            return;

//        // Estrai la parte vocale dal response del server
//        string ttsText = ExtractVocalDescription(response);

//        if (!string.IsNullOrEmpty(ttsText))
//        {
//            Debug.Log($"🔊 Preparazione TTS: {ttsText.Substring(0, Mathf.Min(50, ttsText.Length))}...");

//            // Avvia TTS immediatamente ma in modo fluido
//            StartCoroutine(DelayedTTS(ttsText));
//        }
//    }

//    private string ExtractVocalDescription(string serverResponse)
//    {
//        // Cerca la sezione "DESCRIZIONE VOCALE"
//        string[] lines = serverResponse.Split('\n');

//        bool inVocalSection = false;
//        StringBuilder vocalText = new StringBuilder();

//        foreach (string line in lines)
//        {
//            // Riconosce la sezione vocale
//            if (line.Contains("DESCRIZIONE VOCALE") || line.Contains("DESCRIZIONE SCENA"))
//            {
//                inVocalSection = true;
//                continue;
//            }

//            if (line.Contains("===") && inVocalSection)
//            {
//                break; // Fine sezione vocale
//            }

//            if (inVocalSection && !string.IsNullOrWhiteSpace(line))
//            {
//                vocalText.AppendLine(line.Trim());
//            }
//        }

//        string result = vocalText.ToString().Trim();

//        // Se non trovato, prendi una descrizione dai primi risultati
//        if (string.IsNullOrEmpty(result))
//        {
//            result = ExtractMainDescription(serverResponse);
//        }

//        return CleanTextForTTS(result);
//    }

//    private string ExtractMainDescription(string text)
//    {
//        // Estrai oggetti rilevati
//        string[] lines = text.Split('\n');
//        List<string> objects = new List<string>();

//        foreach (string line in lines)
//        {
//            if (line.StartsWith(" -") || line.Contains("rilevato"))
//            {
//                string cleanLine = line.Replace("-", "").Trim();
//                if (!string.IsNullOrEmpty(cleanLine))
//                {
//                    objects.Add(cleanLine);
//                }
//            }
//        }

//        if (objects.Count > 0)
//        {
//            if (objects.Count == 1)
//                return $"Rilevato: {objects[0]}";
//            else
//                return $"Rilevati: {string.Join(", ", objects)}";
//        }

//        return "Analisi completata";
//    }

//    private string CleanTextForTTS(string text)
//    {
//        if (string.IsNullOrEmpty(text))
//            return "";

//        // Pulisci caratteri problematici per TTS
//        text = Regex.Replace(text, @"[=\-*◆●▪►]", "");
//        text = Regex.Replace(text, @"\s+", " ");
//        text = text.Replace("confidenza:", "");
//        text = text.Replace("DESCRIZIONE VOCALE:", "");
//        text = text.Replace("Oggetti rilevati:", "");

//        // Miglioramenti per pronuncia italiana
//        text = text.Replace("person", "persona");
//        text = text.Replace("car", "auto");
//        text = text.Replace("dog", "cane");
//        text = text.Replace("cat", "gatto");

//        // Limita lunghezza per fluidità
//        if (text.Length > 150)
//        {
//            text = text.Substring(0, 150).TrimEnd() + ".";
//        }

//        return text.Trim();
//    }

//    private IEnumerator DelayedTTS(string text)
//    {
//        // Breve attesa per evitare sovrapposizioni
//        yield return new WaitForSeconds(ttsDelay);

//        // Avvia TTS anche se è già in corso (migliore gestione)
//        SpeakFluent(text);
//    }

//    private void SpeakFluent(string text)
//    {
//        if (Speaker == null || string.IsNullOrEmpty(text))
//        {
//            Debug.LogWarning("⚠️ TTSSpeaker non disponibile o testo vuoto");
//            return;
//        }

//        // Gestione fluida: ferma dolcemente il TTS precedente
//        if (isSpeaking)
//        {
//            StopCurrentTTS();
//            // Piccola pausa per transizione fluida
//            StartCoroutine(DelayedSpeakStart(text, 0.1f));
//        }
//        else
//        {
//            StartSpeaking(text);
//        }
//    }

//    private IEnumerator DelayedSpeakStart(string text, float delay)
//    {
//        yield return new WaitForSeconds(delay);
//        StartSpeaking(text);
//    }

//    private void StartSpeaking(string text)
//    {
//        Debug.Log($"🔊 Avvio TTS fluido: {text.Substring(0, Mathf.Min(50, text.Length))}...");

//        try
//        {
//            isSpeaking = true;
//            currentTTSText = text;
//            currentTTSCoroutine = StartCoroutine(TTSCoroutineImproved(text));
//        }
//        catch (System.Exception e)
//        {
//            Debug.LogError($"❌ Errore TTS: {e}");
//            isSpeaking = false;
//        }
//    }

//    private void SpeakImmediate(string text)
//    {
//        SpeakFluent(text);
//    }

//    private IEnumerator TTSCoroutineImproved(string text)
//    {
//        // senza try/catch attorno al yield
//        Speaker.Speak(text);

//        float baseDuration = text.Length * 0.08f;
//        float estimatedDuration = Mathf.Clamp(baseDuration, 1.5f, 8f);

//        float elapsed = 0f;
//        while (elapsed < estimatedDuration && isSpeaking)
//        {
//            yield return new WaitForSeconds(0.1f); // adesso compila
//            elapsed += 0.1f;
//        }

//        isSpeaking = false;
//        currentTTSCoroutine = null;
//        currentTTSText = "";
//    }


//    private void StopCurrentTTS()
//    {
//        if (currentTTSCoroutine != null)
//        {
//            StopCoroutine(currentTTSCoroutine);
//            currentTTSCoroutine = null;
//        }

//        if (Speaker != null && isSpeaking)
//        {
//            try
//            {
//                Speaker.Stop();
//                Debug.Log("🔊 TTS fermato dolcemente");
//            }
//            catch (Exception e)
//            {
//                Debug.LogWarning($"⚠️ Errore fermando TTS: {e}");
//            }
//        }

//        isSpeaking = false;
//        currentTTSText = "";
//    }

//    // Rileva gesture di pinch
//    private bool IsPinching(OVRHand hand)
//    {
//        if (hand == null || !hand.IsTracked)
//            return false;

//        return hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > pinchThreshold;
//    }

//    void Update()
//    {
//#if !UNITY_WEBGL || UNITY_EDITOR
//        websocket?.DispatchMessageQueue();
//#endif

//        if (hasNewResponse && serverResponseText != null)
//        {
//            serverResponseText.text = latestServerResponse;
//            hasNewResponse = false;
//        }

//        // Controllo input controller - Pulsante A (CATTURA 8 FRAME)
//        if (OVRInput.GetDown(OVRInput.Button.One))
//        {
//            audio.GetComponent<AudioSource>().Play();
//            Debug.Log("🎮 Pulsante A premuto - Avvio cattura 8 frame");
//            _ = CaptureAndSend8Frames();
//        }

//        // Controllo gesture pinch mano sinistra (CATTURA 8 FRAME)
//        bool isLeftPinching = IsPinching(leftHand);
//        if (isLeftPinching && !wasLeftPinching)
//        {
//            audio.GetComponent<AudioSource>().Play();
//            Debug.Log("👈 Pinch mano sinistra rilevato - Avvio cattura 8 frame");
//            _ = CaptureAndSend8Frames();
//        }
//        wasLeftPinching = isLeftPinching;

//        // Controllo gesture pinch mano destra (CATTURA 8 FRAME)
//        bool isRightPinching = IsPinching(rightHand);
//        if (isRightPinching && !wasRightPinching)
//        {
//            audio.GetComponent<AudioSource>().Play();
//            Debug.Log("👉 Pinch mano destra rilevato - Avvio cattura 8 frame");
//            _ = CaptureAndSend8Frames();
//        }
//        wasRightPinching = isRightPinching;

//        // Controllo input alternativo (pulsante B per fermare TTS)
//        if (OVRInput.GetDown(OVRInput.Button.Two))
//        {
//            audio.GetComponent<AudioSource>().Play();
//            StopCurrentTTS();
//            Debug.Log("🔄 Pulsante B premuto - TTS fermato");
//            UpdateStatus("🎯 Premi A per catturare 8 frame");
//        }
//    }

//    private async void OnApplicationQuit()
//    {
//        StopCurrentTTS();

//        if (reusableFrame != null)
//        {
//            Destroy(reusableFrame);
//            reusableFrame = null;
//        }

//        if (websocket != null)
//            await websocket.Close();
//    }

//    public void QuitGame()
//    {
//        Debug.Log("Quit Game!");
//        Application.Quit();
//    }
//}

//Frustum
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Meta.WitAi.TTS.Utilities;
using NativeWebSocket;
using PassthroughCameraSamples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class Streaming : MonoBehaviour
{
    [SerializeField] private WebCamTextureManager camManager;

    [Header("Server Settings")]
    [SerializeField] private GameObject audio;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI serverResponseText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("TTS - Configurazione Migliorata")]
    [SerializeField] private TTSSpeaker Speaker;
    [SerializeField] private Transform TTS;
    [SerializeField] private float ttsDelay = 0.3f; // Ridotto per maggiore reattività
    [SerializeField] private bool skipServerTTS = false;

    [Header("Cattura Frame")]
    [SerializeField] private int framesToCapture = 8;
    [SerializeField] private float frameInterval = 0.2f; // Intervallo tra i frame (200ms)

    private WebCamTexture webcamTexture;
    private Texture2D reusableFrame;
    private WebSocket websocket;

    private string latestServerResponse = null;
    private bool hasNewResponse = false;
    private TaskCompletionSource<string> responseAwaiter;

    [SerializeField] private GameObject Canvas_menu;
    [SerializeField] private GameObject Canvas_credits;

    // Hand tracking per gesture detection
    [Header("Hand Tracking")]
    [SerializeField] private OVRHand leftHand;
    [SerializeField] private OVRHand rightHand;
    [SerializeField] private float pinchThreshold = 0.7f;

    [Header("Ancore Spaziali")]
    [SerializeField] private GameObject virtualObjectPrefab;
    private GameObject currentVirtualObject = null;

    private bool wasLeftPinching = false;
    private bool wasRightPinching = false;

    // Cooldown per evitare catture multiple
    private float lastCaptureTime = 0f;
    private float captureCooldown = 2f;

    // TTS Management Migliorato
    private bool isSpeaking = false;
    private bool isCapturingFrames = false; // Flag per prevenire catture multiple
    private Coroutine currentTTSCoroutine = null;
    private Queue<string> ttsQueue = new Queue<string>();
    private string currentTTSText = "";

    private async void Start()
    {
        if (camManager == null)
        {
            Debug.LogError("WebCamTextureManager non assegnato!");
            return;
        }

        // Inizializza TTS Speaker
        InitializeTTSSpeaker();

        UpdateStatus("Ricerca server...");

        Debug.Log("🔍 Inizio discovery UDP...");
        string serverIP = await ServerDiscovery.DiscoverServerIP();

        if (string.IsNullOrEmpty(serverIP))
        {
            Debug.LogError("❌ Server non trovato nella rete locale.");
            UpdateStatus("❌ Server non trovato");
            SpeakImmediate("Server non trovato nella rete");
            return;
        }

        string serverUrl = $"ws://{serverIP}:5001";
        Debug.Log($"🌐 Connessione a: {serverUrl}");

        Debug.Log("🎥 Avvio camera");
        StartCoroutine(InitAndDisplay());

        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () =>
        {
            Debug.Log("✅ Connessione aperta!");
            UpdateStatus("✅ Connesso - Premi A per 8 frame");
            SpeakImmediate("Connesso al server. Sistema pronto per cattura multipla.");
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError("❌ Errore: " + e);
            UpdateStatus("❌ Errore connessione");
            SpeakImmediate("Errore di connessione");
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("🔌 Connessione chiusa con codice: " + e);
            UpdateStatus("🔌 Disconnesso");
            SpeakImmediate("Connessione chiusa");
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            Debug.Log("📩 Ricevuto dal server: " + message);

            latestServerResponse = message;
            hasNewResponse = true;

            responseAwaiter?.TrySetResult(message);
        };

        await websocket.Connect();
    }

    private void InitializeTTSSpeaker()
    {
        if (TTS != null)
        {
            Speaker = TTS.Find("TTSSpeaker")?.GetComponent<TTSSpeaker>();
        }

        if (Speaker == null)
        {
            Debug.LogWarning("TTSSpeaker non trovato! Verificare la configurazione.");
            return;
        }

        Debug.Log("🔊 TTSSpeaker configurato correttamente");
    }

    private void UpdateStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
        Debug.Log($"[Status] {status}");
    }

    public void Start_Streaming()
    {
        SpeakImmediate("Sistema di analisi immagini attivo. Cattura automatica 8 frame.");
        Canvas_menu.SetActive(false);
        UpdateStatus("🎯 Premi A per catturare 8 frame");
        Debug.Log("Menu nascosto, app pronta per cattura automatica 8 frame.");
    }

    public void Credits()
    {
        Canvas_credits.SetActive(true);
    }

    private IEnumerator InitAndDisplay()
    {
        while (camManager.WebCamTexture == null)
            yield return null;

        webcamTexture = camManager.WebCamTexture;
        webcamTexture.Play();
        Debug.Log("📹 Stream camera avviato.");

        reusableFrame = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGB24, false);
    }

    private byte[] CaptureCurrentFrame()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying)
        {
            Debug.LogWarning("⚠️ Camera non disponibile");
            return null;
        }

        reusableFrame.SetPixels(webcamTexture.GetPixels());
        reusableFrame.Apply();

        byte[] imageBytes = reusableFrame.EncodeToJPG(85);
        Debug.Log($"📸 Frame catturato: {imageBytes.Length} bytes");

        return imageBytes;
    }

    private async Task CaptureAndSend8Frames()
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("⚠️ Connessione WebSocket non disponibile.");
            UpdateStatus("⚠️ Non connesso");
            SpeakImmediate("Non connesso al server");
            return;
        }

        // Verifica cooldown
        if (Time.time - lastCaptureTime < captureCooldown)
        {
            float remainingTime = captureCooldown - (Time.time - lastCaptureTime);
            Debug.Log($"⏱️ Attendi {remainingTime:F1}s prima della prossima cattura");
            UpdateStatus($"⏱️ Attendi {remainingTime:F1}s");
            return;
        }

        // Previeni catture multiple simultanamente
        if (isCapturingFrames)
        {
            Debug.Log("⚠️ Cattura già in corso...");
            UpdateStatus("⚠️ Cattura in corso...");
            return;
        }

        isCapturingFrames = true;
        lastCaptureTime = Time.time;

        // Ferma TTS corrente prima di nuova cattura
        StopCurrentTTS();

        try
        {
            Debug.Log($"📸 Inizio cattura sequenziale di {framesToCapture} frame...");
            UpdateStatus($"📸 Catturando {framesToCapture} frame...");

            for (int i = 0; i < framesToCapture; i++)
            {
                UpdateStatus($"📸 Frame {i + 1}/{framesToCapture}...");

                byte[] frame = CaptureCurrentFrame();
                if (frame == null)
                {
                    Debug.LogError($"❌ Errore cattura frame {i + 1}");
                    continue;
                }

                Debug.Log($"📤 Invio frame {i + 1}/{framesToCapture} ({frame.Length} bytes)");

                try
                {
                    await websocket.Send(frame);
                    Debug.Log($"✅ Frame {i + 1} inviato con successo");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Errore invio frame {i + 1}: {e}");
                }

                // Attesa tra i frame (tranne per l'ultimo)
                if (i < framesToCapture - 1)
                {
                    await Task.Delay((int)(frameInterval * 1000));
                }
            }

            Debug.Log("🎉 Tutti i frame inviati, attendo risposta dal server...");
            UpdateStatus("🔍 Analisi in corso...");

            // Aspetta risposta dal server
            responseAwaiter = new TaskCompletionSource<string>();
            string risposta = await responseAwaiter.Task;

            Debug.Log("🎉 Analisi completata: " + risposta.Substring(0, Mathf.Min(100, risposta.Length)));
            UpdateStatus("✅ Analisi completata");

            if (serverResponseText != null)
                serverResponseText.text = risposta;

            // Gestione TTS migliorata
            ProcessServerResponse(risposta);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Errore durante cattura 8 frame: {e}");
            UpdateStatus("❌ Errore cattura");
            SpeakImmediate("Errore durante la cattura dei frame");
        }
        finally
        {
            isCapturingFrames = false;
            UpdateStatus("🎯 Premi A per catturare 8 frame");
        }
    }

    private void ProcessServerResponse(string response)
    {
        if (string.IsNullOrEmpty(response))
            return;

        // Estrai la parte vocale dal response del server
        string ttsText = ExtractVocalDescription(response);

        if (!string.IsNullOrEmpty(ttsText))
        {
            Debug.Log($"🔊 Preparazione TTS: {ttsText.Substring(0, Mathf.Min(50, ttsText.Length))}...");

            // Avvia TTS immediatamente ma in modo fluido
            StartCoroutine(DelayedTTS(ttsText));
        }
    }

    private string ExtractVocalDescription(string serverResponse)
    {
        // Cerca la sezione "DESCRIZIONE VOCALE"
        string[] lines = serverResponse.Split('\n');

        bool inVocalSection = false;
        StringBuilder vocalText = new StringBuilder();

        foreach (string line in lines)
        {
            // Riconosce la sezione vocale
            if (line.Contains("DESCRIZIONE VOCALE") || line.Contains("DESCRIZIONE SCENA"))
            {
                inVocalSection = true;
                continue;
            }

            if (line.Contains("===") && inVocalSection)
            {
                break; // Fine sezione vocale
            }

            if (inVocalSection && !string.IsNullOrWhiteSpace(line))
            {
                vocalText.AppendLine(line.Trim());
            }
        }

        string result = vocalText.ToString().Trim();

        // Se non trovato, prendi una descrizione dai primi risultati
        if (string.IsNullOrEmpty(result))
        {
            result = ExtractMainDescription(serverResponse);
        }

        return CleanTextForTTS(result);
    }

    private string ExtractMainDescription(string text)
    {
        // Estrai oggetti rilevati
        string[] lines = text.Split('\n');
        List<string> objects = new List<string>();

        foreach (string line in lines)
        {
            if (line.StartsWith(" -") || line.Contains("rilevato"))
            {
                string cleanLine = line.Replace("-", "").Trim();
                if (!string.IsNullOrEmpty(cleanLine))
                {
                    objects.Add(cleanLine);
                }
            }
        }

        if (objects.Count > 0)
        {
            if (objects.Count == 1)
                return $"Rilevato: {objects[0]}";
            else
                return $"Rilevati: {string.Join(", ", objects)}";
        }

        return "Analisi completata";
    }

    private string CleanTextForTTS(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        // Pulisci caratteri problematici per TTS
        text = Regex.Replace(text, @"[=\-*◆●▪►]", "");
        text = Regex.Replace(text, @"\s+", " ");
        text = text.Replace("confidenza:", "");
        text = text.Replace("DESCRIZIONE VOCALE:", "");
        text = text.Replace("Oggetti rilevati:", "");

        // Miglioramenti per pronuncia italiana
        text = text.Replace("person", "persona");
        text = text.Replace("car", "auto");
        text = text.Replace("dog", "cane");
        text = text.Replace("cat", "gatto");

        // Limita lunghezza per fluidità
        if (text.Length > 150)
        {
            text = text.Substring(0, 150).TrimEnd() + ".";
        }

        return text.Trim();
    }

    private IEnumerator DelayedTTS(string text)
    {
        // Breve attesa per evitare sovrapposizioni
        yield return new WaitForSeconds(ttsDelay);

        // Avvia TTS anche se è già in corso (migliore gestione)
        SpeakFluent(text);
    }

    private void SpeakFluent(string text)
    {
        if (Speaker == null || string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("⚠️ TTSSpeaker non disponibile o testo vuoto");
            return;
        }

        // Gestione fluida: ferma dolcemente il TTS precedente
        if (isSpeaking)
        {
            StopCurrentTTS();
            // Piccola pausa per transizione fluida
            StartCoroutine(DelayedSpeakStart(text, 0.1f));
        }
        else
        {
            StartSpeaking(text);
        }
    }

    private IEnumerator DelayedSpeakStart(string text, float delay)
    {
        yield return new WaitForSeconds(delay);
        StartSpeaking(text);
    }

    private void StartSpeaking(string text)
    {
        Debug.Log($"🔊 Avvio TTS fluido: {text.Substring(0, Mathf.Min(50, text.Length))}...");

        try
        {
            isSpeaking = true;
            currentTTSText = text;
            currentTTSCoroutine = StartCoroutine(TTSCoroutineImproved(text));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Errore TTS: {e}");
            isSpeaking = false;
        }
    }

    private void SpeakImmediate(string text)
    {
        SpeakFluent(text);
    }

    private IEnumerator TTSCoroutineImproved(string text)
    {
        // senza try/catch attorno al yield
        Speaker.Speak(text);

        float baseDuration = text.Length * 0.08f;
        float estimatedDuration = Mathf.Clamp(baseDuration, 1.5f, 8f);

        float elapsed = 0f;
        while (elapsed < estimatedDuration && isSpeaking)
        {
            yield return new WaitForSeconds(0.1f); // adesso compila
            elapsed += 0.1f;
        }

        isSpeaking = false;
        currentTTSCoroutine = null;
        currentTTSText = "";
    }


    private void StopCurrentTTS()
    {
        if (currentTTSCoroutine != null)
        {
            StopCoroutine(currentTTSCoroutine);
            currentTTSCoroutine = null;
        }

        if (Speaker != null && isSpeaking)
        {
            try
            {
                Speaker.Stop();
                Debug.Log("🔊 TTS fermato dolcemente");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ Errore fermando TTS: {e}");
            }
        }

        isSpeaking = false;
        currentTTSText = "";
    }

    // Rileva gesture di pinch
    private bool IsPinching(OVRHand hand)
    {
        if (hand == null || !hand.IsTracked)
            return false;

        return hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > pinchThreshold;
    }

    private void SpawnAndDestroyObject(Vector3 pinchPosition)
    {
        // 1. Distrugge l'oggetto precedente, se ne esiste uno
        if (currentVirtualObject != null)
        {
            Destroy(currentVirtualObject);
            currentVirtualObject = null;
        }

        // 2. Istanzia il nuovo oggetto virtuale
        if (virtualObjectPrefab != null)
        {
            currentVirtualObject = Instantiate(virtualObjectPrefab, pinchPosition, Quaternion.identity);
            Debug.Log("🎉 Oggetto virtuale creato al pinch!");
        }
        else
        {
            Debug.LogWarning("⚠️ Prefab dell'oggetto virtuale non assegnato!");
        }
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif

        if (hasNewResponse && serverResponseText != null)
        {
            serverResponseText.text = latestServerResponse;
            hasNewResponse = false;
        }

        // Controllo input controller - Pulsante A (CATTURA 8 FRAME)
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            audio.GetComponent<AudioSource>().Play();
            Debug.Log("🎮 Pulsante A premuto - Avvio cattura 8 frame");
            _ = CaptureAndSend8Frames();
        }

        // Controllo gesture pinch mano sinistra (CATTURA 8 FRAME)
        bool isLeftPinching = IsPinching(leftHand);
        if (isLeftPinching && !wasLeftPinching)
        {
            audio.GetComponent<AudioSource>().Play();
            Debug.Log("👈 Pinch mano sinistra rilevato - Avvio cattura 8 frame");
            _ = CaptureAndSend8Frames();
            Debug.Log("👈 Pinch mano sinistra rilevato - Creazione Ancora");
            SpawnAndDestroyObject(leftHand.transform.position); // Chiama la nuova funzione
        }
        wasLeftPinching = isLeftPinching;

        // Controllo gesture pinch mano destra (CATTURA 8 FRAME)
        bool isRightPinching = IsPinching(rightHand);
        if (isRightPinching && !wasRightPinching)
        {
            audio.GetComponent<AudioSource>().Play();
            Debug.Log("👉 Pinch mano destra rilevato - Avvio cattura 8 frame");
            _ = CaptureAndSend8Frames();
            Debug.Log("👉 Pinch mano destra rilevato - Creazione Ancora");
            SpawnAndDestroyObject(rightHand.transform.position); // Chiama la nuova funzione

        }
        wasRightPinching = isRightPinching;

        // Controllo input alternativo (pulsante B per fermare TTS)
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            audio.GetComponent<AudioSource>().Play();
            StopCurrentTTS();
            Debug.Log("🔄 Pulsante B premuto - TTS fermato");
            UpdateStatus("🎯 Premi A per catturare 8 frame");
        }
    }

    private async void OnApplicationQuit()
    {
        StopCurrentTTS();

        if (reusableFrame != null)
        {
            Destroy(reusableFrame);
            reusableFrame = null;
        }

        if (websocket != null)
            await websocket.Close();
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game!");
        Application.Quit();
    }
}


