using UnityEngine;
using Meta.WitAi.TTS;
using Meta.WitAi.TTS.Utilities;


public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance { get; private set; }

    [Header("TTS Settings")]
    [SerializeField] private static TTSSpeaker speaker;
    [SerializeField] private Transform TTS;
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            SetUp();
        }
    }

    public static void Speak(string text)
    {
        if (speaker != null) speaker.Speak(text);
        else Debug.LogWarning("TTSSpeaker non assegnato!");
    }

    public static void Stop()
    {
        if (speaker != null) speaker.Pause();
        else Debug.LogWarning("TTSSpeaker non assegnato!");
    }

    public static void Play()
    {
        if (speaker != null) speaker.Resume();
        else Debug.LogWarning("TTSSpeaker non assegnato!");
    }

    public static void Cancel()
    {
        if (speaker != null) speaker.Stop();
        else Debug.LogWarning("TTSSpeaker non assegnato!");
    }

    private void SetUp()
    {
        speaker = TTS.Find("TTSSpeaker").GetComponent<TTSSpeaker>();
    }
}