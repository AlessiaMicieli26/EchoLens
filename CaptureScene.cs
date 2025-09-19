using UnityEngine;
using UnityEngine.UI;
using PassthroughCameraSamples; // Namespace del sample

public class CaptureScene : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;              // UI
    [SerializeField] private WebCamTextureManager camManager; // Assegnalo dal prefab

    private WebCamTexture webcamTexture;

    private void Start()
    {
        if (camManager == null)
        {
            Debug.LogError("📌 RawImage o WebCamTextureManager non assegnati!");
            return;
        }

        StartCoroutine(InitAndDisplay());
    }

    private System.Collections.IEnumerator InitAndDisplay()
    {
        // Attendi inizializzazione e permessi
        while (camManager.WebCamTexture == null)
        {
            yield return null;
        }

        webcamTexture = camManager.WebCamTexture;
        rawImage.texture = webcamTexture;
        rawImage.material.mainTexture = webcamTexture;

        webcamTexture.Play();
        Debug.Log("✅ Stream camera avviato.");
    }

    public void SaveRawImage()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying)
        {
            Debug.LogError("RawImage o WebCamTexture non disponibili, impossibile salvare.");
            return;
        }

        var tex2D = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGB24, false);
        tex2D.SetPixels(webcamTexture.GetPixels());
        tex2D.Apply();

        byte[] pngData = tex2D.EncodeToPNG();
        string fileName = $"capture_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string path = System.IO.Path.Combine(Application.persistentDataPath, fileName);

        try
        {
            System.IO.File.WriteAllBytes(path, pngData);
            Debug.Log($"📸 Screenshot salvato: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Errore salvataggio: {e.Message}");
        }

        Destroy(tex2D);
    }
}