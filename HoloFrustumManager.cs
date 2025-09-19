using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manager per la creazione e gestione di múltipli frustum ologrammi
/// Ogni pinch crea un nuovo frustum che rimane fisso nello spazio
/// Supporta creazione di istanze multiple alla posizione del pinch
/// </summary>
public class HoloFrustumManager : MonoBehaviour
{
    [Header("Frustum Settings")]
    public Camera targetCamera;
    public float nearPlane = 0.3f;
    public float farPlane = 2f; // Ridotto per frustum più piccoli
    public float fieldOfView = 60f;
    public float aspect = 16f / 9f;

    [Header("Visual Settings")]
    public Color hologramColor = new Color(0f, 1f, 1f, 0.8f);
    public float lineWidth = 0.015f; // Linee più sottili
    public bool animateGlow = true;
    public float glowSpeed = 2f;

    [Header("Frustum Management")]
    public int maxFrustums = 10;
    public float frustumLifetime = 30f;
    public bool fadeOutOldFrustums = true;
    public bool autoCleanup = true;

    [Header("Debug")]
    public bool showDebugSphere = false;
    public bool logFrustumEvents = true;

    private List<HoloFrustumInstance> activeFrustums = new List<HoloFrustumInstance>();
    private int frustumCounter = 0;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (logFrustumEvents)
            Debug.Log("HoloFrustumManager inizializzato");
    }

    void Update()
    {
        UpdateActiveFrustums();

        if (autoCleanup)
        {
            RemoveExpiredFrustums();
        }
    }

    /// <summary>
    /// Crea un nuovo frustum ologramma alla posizione corrente della camera
    /// </summary>
    public void OnPinchCapture(Camera camera = null)
    {
        if (camera != null)
            targetCamera = camera;

        if (targetCamera == null)
        {
            Debug.LogWarning("Nessuna camera disponibile per il frustum");
            return;
        }

        CreateFrustumAtCurrentPosition();
    }

    /// <summary>
    /// Crea una nuova istanza di frustum alla posizione specificata
    /// NUOVO METODO per supportare posizioni custom del pinch
    /// </summary>
    public void CreateNewFrustumAtPosition(Camera camera, Vector3 pinchPosition)
    {
        if (camera != null)
            targetCamera = camera;

        if (targetCamera == null)
        {
            Debug.LogWarning("Nessuna camera disponibile per il frustum");
            return;
        }

        CreateFrustumAtSpecificPosition(pinchPosition);
    }

    /// <summary>
    /// Crea un nuovo frustum ologramma alla posizione corrente della camera
    /// </summary>
    void CreateFrustumAtCurrentPosition()
    {
        Vector3 position = targetCamera.transform.position + targetCamera.transform.forward * 0.5f;
        CreateFrustumAtSpecificPosition(position);
    }

    /// <summary>
    /// Crea un nuovo frustum ologramma alla posizione specificata
    /// </summary>
    void CreateFrustumAtSpecificPosition(Vector3 position)
    {
        // Crea GameObject per il nuovo frustum
        GameObject frustumObj = new GameObject($"HoloFrustum_{frustumCounter:D3}");
        frustumObj.transform.SetParent(transform);

        // Posiziona il frustum alla posizione specificata
        frustumObj.transform.position = position;

        // Aggiungi il componente HoloFrustumInstance
        HoloFrustumInstance instance = frustumObj.AddComponent<HoloFrustumInstance>();

        // Inizializza con i parametri correnti
        instance.Initialize(
            targetCamera,
            hologramColor,
            lineWidth,
            animateGlow,
            glowSpeed,
            frustumLifetime,
            fadeOutOldFrustums,
            nearPlane,
            farPlane,
            fieldOfView,
            aspect
        );

        // Cattura il frustum alla posizione specificata
        instance.CaptureCurrentFrustumAtPosition(position);

        // Aggiungi alla lista
        activeFrustums.Add(instance);
        frustumCounter++;

        if (logFrustumEvents)
            Debug.Log($"Nuovo frustum creato alla posizione {position}: {frustumObj.name} (Totale: {activeFrustums.Count})");

        // Rimuovi il più vecchio se necessario
        if (activeFrustums.Count > maxFrustums)
        {
            RemoveOldestFrustum();
        }
    }

    /// <summary>
    /// Aggiorna tutti i frustum attivi
    /// </summary>
    void UpdateActiveFrustums()
    {
        for (int i = activeFrustums.Count - 1; i >= 0; i--)
        {
            if (activeFrustums[i] == null || activeFrustums[i].gameObject == null)
            {
                activeFrustums.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Rimuove frustum scaduti
    /// </summary>
    void RemoveExpiredFrustums()
    {
        for (int i = activeFrustums.Count - 1; i >= 0; i--)
        {
            if (activeFrustums[i] != null && activeFrustums[i].IsExpired())
            {
                if (logFrustumEvents)
                    Debug.Log($"Rimozione frustum scaduto: {activeFrustums[i].name}");

                DestroyFrustum(activeFrustums[i]);
                activeFrustums.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Rimuove il frustum più vecchio (il primo nella lista)
    /// NUOVO METODO per supporto gestione multiple istanze
    /// </summary>
    public bool RemoveOldestFrustum()
    {
        if (activeFrustums.Count > 0)
        {
            HoloFrustumInstance oldest = activeFrustums[0];

            if (logFrustumEvents)
                Debug.Log("Rimozione frustum più vecchio per limite massimo");

            DestroyFrustum(oldest);
            activeFrustums.RemoveAt(0);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Rimuove l'ultimo frustum creato (l'ultimo nella lista)
    /// NUOVO METODO per supporto gestione multiple istanze
    /// </summary>
    public bool RemoveLatestFrustum()
    {
        if (activeFrustums.Count > 0)
        {
            int lastIndex = activeFrustums.Count - 1;
            HoloFrustumInstance latest = activeFrustums[lastIndex];

            if (logFrustumEvents)
                Debug.Log($"Rimozione ultimo frustum: {latest.name}");

            DestroyFrustum(latest);
            activeFrustums.RemoveAt(lastIndex);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Distrugge un frustum specifico
    /// </summary>
    void DestroyFrustum(HoloFrustumInstance frustum)
    {
        if (frustum != null && frustum.gameObject != null)
        {
            frustum.Cleanup();
            Destroy(frustum.gameObject);
        }
    }

    /// <summary>
    /// Pulisce tutti i frustum attivi e restituisce il numero rimosso
    /// METODO MODIFICATO per supporto gestione multiple istanze
    /// </summary>
    public int ClearAllFrustums()
    {
        int count = activeFrustums.Count;

        for (int i = activeFrustums.Count - 1; i >= 0; i--)
        {
            DestroyFrustum(activeFrustums[i]);
        }

        activeFrustums.Clear();

        if (logFrustumEvents)
            Debug.Log($"Tutti i {count} frustum rimossi");

        return count;
    }

    /// <summary>
    /// Ottiene il numero di frustum attivi
    /// </summary>
    public int GetActiveFrustumCount()
    {
        // Pulisci eventuali riferimenti null prima di restituire il conteggio
        UpdateActiveFrustums();
        return activeFrustums.Count;
    }

    /// <summary>
    /// Rimuove frustum più vecchi del tempo specificato
    /// NUOVO METODO per gestione avanzata
    /// </summary>
    public int RemoveFrustumsOlderThan(float maxAge)
    {
        int removedCount = 0;

        for (int i = activeFrustums.Count - 1; i >= 0; i--)
        {
            if (activeFrustums[i] != null && activeFrustums[i].GetAge() > maxAge)
            {
                if (logFrustumEvents)
                    Debug.Log($"Rimozione frustum troppo vecchio: {activeFrustums[i].name} (età: {activeFrustums[i].GetAge():F1}s)");

                DestroyFrustum(activeFrustums[i]);
                activeFrustums.RemoveAt(i);
                removedCount++;
            }
        }

        return removedCount;
    }

    /// <summary>
    /// Ottiene tutti i frustum attivi
    /// NUOVO METODO per accesso avanzato
    /// </summary>
    public List<HoloFrustumInstance> GetAllActiveFrustums()
    {
        UpdateActiveFrustums();
        return new List<HoloFrustumInstance>(activeFrustums);
    }

    /// <summary>
    /// Cambia il colore di tutti i frustum attivi
    /// NUOVO METODO per personalizzazione
    /// </summary>
    public void ChangeAllFrustumsColor(Color newColor)
    {
        hologramColor = newColor;

        foreach (var frustum in activeFrustums)
        {
            if (frustum != null)
            {
                frustum.ChangeColor(newColor);
            }
        }

        if (logFrustumEvents)
            Debug.Log($"Colore di tutti i frustum cambiato a: {newColor}");
    }

    void OnDrawGizmos()
    {
        if (showDebugSphere)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.1f);

            // Disegna numero di frustum attivi
            if (activeFrustums.Count > 0)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, 0.05f * activeFrustums.Count);
            }

            // Disegna le posizioni di tutti i frustum attivi
            foreach (var frustum in activeFrustums)
            {
                if (frustum != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(frustum.transform.position, 0.03f);
                }
            }
        }
    }

    void OnDestroy()
    {
        ClearAllFrustums();
    }
}

/// <summary>
/// Classe per gestire una singola istanza di frustum ologramma
/// CLASSE ESTESA per supportare posizioni custom e nuove funzionalità
/// </summary>
public class HoloFrustumInstance : MonoBehaviour
{
    private Vector3 cameraPosition;
    private Quaternion cameraRotation;
    private Vector3[] frustumCorners = new Vector3[8];
    private LineRenderer[] lineRenderers = new LineRenderer[12];

    private Color baseColor;
    private Color currentColor;
    private float lineWidth;
    private bool animateGlow;
    private float glowSpeed;
    private float lifetime;
    private bool fadeOut;
    private float nearPlane;
    private float farPlane;
    private float fieldOfView;
    private float aspectRatio;

    private float creationTime;
    private Material[] materials = new Material[12];

    // Indici delle 12 linee del frustum
    private readonly int[,] lineIndices = new int[12, 2]
    {
        // Near plane edges (4 linee)
        {0, 1}, {1, 2}, {2, 3}, {3, 0},
        // Far plane edges (4 linee)  
        {4, 5}, {5, 6}, {6, 7}, {7, 4},
        // Connecting edges (4 linee)
        {0, 4}, {1, 5}, {2, 6}, {3, 7}
    };

    /// <summary>
    /// Inizializza l'istanza del frustum con tutti i parametri
    /// </summary>
    public void Initialize(Camera camera, Color color, float width, bool glow, float speed,
                          float life, bool fade, float near, float far, float fov, float aspect)
    {
        baseColor = color;
        currentColor = color;
        lineWidth = width;
        animateGlow = glow;
        glowSpeed = speed;
        lifetime = life;
        fadeOut = fade;
        nearPlane = near;
        farPlane = far;
        fieldOfView = fov;
        aspectRatio = aspect;
        creationTime = Time.time;

        SetupLineRenderers();
    }

    void Update()
    {
        if (animateGlow)
        {
            AnimateHologramGlow();
        }

        if (fadeOut)
        {
            ApplyFadeOut();
        }
    }

    /// <summary>
    /// Cattura il frustum della camera specificata
    /// </summary>
    public void CaptureCurrentFrustum()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("Nessuna camera disponibile per catturare il frustum");
            return;
        }

        // Snapshot della posizione e rotazione della camera
        cameraPosition = cam.transform.position;
        cameraRotation = cam.transform.rotation;

        // Usa i parametri configurati o quelli della camera
        float fov = fieldOfView > 0 ? fieldOfView : cam.fieldOfView;
        float near = nearPlane > 0 ? nearPlane : cam.nearClipPlane;
        float far = farPlane > 0 ? farPlane : cam.farClipPlane;
        float aspect = aspectRatio > 0 ? aspectRatio : cam.aspect;

        CalculateFrustumCorners(near, far, fov, aspect);
        DrawFrustumLines();
    }

    /// <summary>
    /// Cattura il frustum della camera alla posizione specificata
    /// NUOVO METODO per supportare posizioni custom del pinch
    /// </summary>
    public void CaptureCurrentFrustumAtPosition(Vector3 position)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("Nessuna camera disponibile per catturare il frustum");
            return;
        }

        // Usa la posizione specificata invece della posizione della camera
        cameraPosition = position;
        cameraRotation = cam.transform.rotation;

        // Usa i parametri configurati o quelli della camera
        float fov = fieldOfView > 0 ? fieldOfView : cam.fieldOfView;
        float near = nearPlane > 0 ? nearPlane : cam.nearClipPlane;
        float far = farPlane > 0 ? farPlane : cam.farClipPlane;
        float aspect = aspectRatio > 0 ? aspectRatio : cam.aspect;

        CalculateFrustumCorners(near, far, fov, aspect);
        DrawFrustumLines();
    }

    /// <summary>
    /// Calcola gli 8 punti che definiscono il frustum
    /// </summary>
    void CalculateFrustumCorners(float near, float far, float fov, float aspect)
    {
        float halfFOV = fov * 0.5f * Mathf.Deg2Rad;

        // Dimensioni dei piani near e far
        float nearHeight = 2f * Mathf.Tan(halfFOV) * near;
        float nearWidth = nearHeight * aspect;
        float farHeight = 2f * Mathf.Tan(halfFOV) * far;
        float farWidth = farHeight * aspect;

        // Punti del piano near (in spazio camera)
        Vector3[] nearCorners = new Vector3[4]
        {
            new Vector3(-nearWidth * 0.5f, -nearHeight * 0.5f, near),  // Bottom-left
            new Vector3(nearWidth * 0.5f, -nearHeight * 0.5f, near),   // Bottom-right
            new Vector3(nearWidth * 0.5f, nearHeight * 0.5f, near),    // Top-right
            new Vector3(-nearWidth * 0.5f, nearHeight * 0.5f, near)    // Top-left
        };

        // Punti del piano far (in spazio camera)
        Vector3[] farCorners = new Vector3[4]
        {
            new Vector3(-farWidth * 0.5f, -farHeight * 0.5f, far),     // Bottom-left
            new Vector3(farWidth * 0.5f, -farHeight * 0.5f, far),      // Bottom-right
            new Vector3(farWidth * 0.5f, farHeight * 0.5f, far),       // Top-right
            new Vector3(-farWidth * 0.5f, farHeight * 0.5f, far)       // Top-left
        };

        // Trasforma i punti da camera space a world space
        for (int i = 0; i < 4; i++)
        {
            frustumCorners[i] = cameraPosition + cameraRotation * nearCorners[i];
            frustumCorners[i + 4] = cameraPosition + cameraRotation * farCorners[i];
        }
    }

    /// <summary>
    /// Configura i 12 LineRenderer per le linee del frustum
    /// </summary>
    void SetupLineRenderers()
    {
        for (int i = 0; i < 12; i++)
        {
            GameObject lineObj = new GameObject($"Line_{i:D2}");
            lineObj.transform.SetParent(transform);

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            Material mat = CreateHologramMaterial();
            lr.material = mat;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.sortingOrder = 100;

            lineRenderers[i] = lr;
            materials[i] = mat;
        }
    }

    /// <summary>
    /// Disegna le 12 linee che formano il frustum
    /// </summary>
    void DrawFrustumLines()
    {
        for (int i = 0; i < 12; i++)
        {
            int startIndex = lineIndices[i, 0];
            int endIndex = lineIndices[i, 1];

            lineRenderers[i].SetPosition(0, frustumCorners[startIndex]);
            lineRenderers[i].SetPosition(1, frustumCorners[endIndex]);
        }
    }

    /// <summary>
    /// Crea il materiale per l'effetto ologramma
    /// </summary>
    Material CreateHologramMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));

        // Prova shader più avanzati se disponibili
        if (Shader.Find("Universal Render Pipeline/Unlit"))
            mat.shader = Shader.Find("Universal Render Pipeline/Unlit");
        else if (Shader.Find("Unlit/Color"))
            mat.shader = Shader.Find("Unlit/Color");

        mat.color = currentColor;

        // Configurazione trasparenza
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        return mat;
    }

    /// <summary>
    /// Anima l'effetto glow dell'ologramma
    /// </summary>
    void AnimateHologramGlow()
    {
        float glow = 0.7f + 0.3f * Mathf.Sin(Time.time * glowSpeed);
        Color animatedColor = currentColor * glow;

        // Applica fade se necessario
        if (fadeOut)
        {
            float age = Time.time - creationTime;
            float fadeAlpha = Mathf.Clamp01(1f - (age / lifetime));
            animatedColor.a *= fadeAlpha;
        }

        // Applica il colore a tutti i materiali
        for (int i = 0; i < 12; i++)
        {
            if (materials[i] != null)
            {
                materials[i].color = animatedColor;
            }
        }
    }

    /// <summary>
    /// Applica l'effetto fade-out basato sull'età del frustum
    /// </summary>
    void ApplyFadeOut()
    {
        float age = Time.time - creationTime;
        float fadeAlpha = Mathf.Clamp01(1f - (age / lifetime));

        Color fadedColor = currentColor;
        fadedColor.a *= fadeAlpha;

        for (int i = 0; i < 12; i++)
        {
            if (materials[i] != null)
            {
                materials[i].color = fadedColor;
            }
        }
    }

    /// <summary>
    /// Cambia il colore del frustum
    /// NUOVO METODO per personalizzazione
    /// </summary>
    public void ChangeColor(Color newColor)
    {
        baseColor = newColor;
        currentColor = newColor;

        for (int i = 0; i < 12; i++)
        {
            if (materials[i] != null)
            {
                materials[i].color = currentColor;
            }
        }
    }

    /// <summary>
    /// Verifica se il frustum è scaduto e dovrebbe essere rimosso
    /// </summary>
    public bool IsExpired()
    {
        return Time.time - creationTime > lifetime;
    }

    /// <summary>
    /// Ottiene l'età del frustum in secondi
    /// </summary>
    public float GetAge()
    {
        return Time.time - creationTime;
    }

    /// <summary>
    /// Ottiene il tempo rimanente prima della scadenza
    /// NUOVO METODO per informazioni avanzate
    /// </summary>
    public float GetRemainingTime()
    {
        return Mathf.Max(0, lifetime - GetAge());
    }

    /// <summary>
    /// Pulisce tutte le risorse per evitare memory leak
    /// </summary>
    public void Cleanup()
    {
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null)
            {
                DestroyImmediate(materials[i]);
                materials[i] = null;
            }
        }

        for (int i = 0; i < lineRenderers.Length; i++)
        {
            if (lineRenderers[i] != null)
            {
                lineRenderers[i] = null;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (frustumCorners != null && frustumCorners.Length == 8)
        {
            // Disegna i punti del frustum come sfere piccole
            Gizmos.color = Color.cyan;
            for (int i = 0; i < 8; i++)
            {
                Gizmos.DrawWireSphere(frustumCorners[i], 0.02f);
            }

            // Disegna la posizione della camera quando è stato catturato
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(cameraPosition, 0.05f);
        }
    }
}