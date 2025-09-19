using UnityEngine;

public class HoloFrustum : MonoBehaviour
{
    [Header("Frustum Settings")]
    public Camera targetCamera;
    public float nearPlane = 0.3f;
    public float farPlane = 10f;
    public float fieldOfView = 60f;
    public float aspect = 16f / 9f;

    [Header("Visual Settings")]
    public Color hologramColor = new Color(0f, 1f, 1f, 0.8f); // Ciano
    public float lineWidth = 0.02f;
    public bool animateGlow = true;
    public float glowSpeed = 2f;

    [Header("Debug")]
    public bool showDebugSphere = false;

    // Frustum snapshot data (catturato al momento del pinch)
    private Vector3 cameraPosition;
    private Quaternion cameraRotation;
    private Vector3[] frustumCorners = new Vector3[8];
    private LineRenderer[] lineRenderers = new LineRenderer[12];

    // Line indices for the 12 edges of frustum
    private readonly int[,] lineIndices = new int[12, 2]
    {
        // Near plane edges (4 lines)
        {0, 1}, {1, 2}, {2, 3}, {3, 0},
        // Far plane edges (4 lines)  
        {4, 5}, {5, 6}, {6, 7}, {7, 4},
        // Connecting edges (4 lines)
        {0, 4}, {1, 5}, {2, 6}, {3, 7}
    };

    void Start()
    {
        // Se non è stata assegnata una camera, usa la main camera
        if (targetCamera == null)
            targetCamera = Camera.main;

        // Cattura lo snapshot del frustum della camera
        CaptureCurrentFrustum();

        // Setup dei LineRenderer per le 12 linee
        SetupLineRenderers();

        // Disegna il frustum
        DrawFrustumLines();
    }

    void Update()
    {
        // Anima il glow se abilitato
        if (animateGlow)
        {
            AnimateHologramGlow();
        }
    }

    /// <summary>
    /// Cattura la posizione e rotazione corrente della camera e calcola i punti del frustum
    /// </summary>
    public void CaptureCurrentFrustum()
    {
        if (targetCamera == null) return;

        // Snapshot della camera
        cameraPosition = targetCamera.transform.position;
        cameraRotation = targetCamera.transform.rotation;

        // Usa i parametri della camera o quelli override
        float fov = targetCamera.fieldOfView;
        float near = targetCamera.nearClipPlane;
        float far = targetCamera.farClipPlane;
        float aspectRatio = targetCamera.aspect;

        // Override se specificati
        if (fieldOfView > 0) fov = fieldOfView;
        if (nearPlane > 0) near = nearPlane;
        if (farPlane > 0) far = farPlane;
        if (aspect > 0) aspectRatio = aspect;

        CalculateFrustumCorners(near, far, fov, aspectRatio);
    }

    /// <summary>
    /// Calcola gli 8 punti del frustum
    /// </summary>
    void CalculateFrustumCorners(float near, float far, float fov, float aspectRatio)
    {
        float halfFOV = fov * 0.5f * Mathf.Deg2Rad;

        // Calcola dimensioni dei piani near e far
        float nearHeight = 2f * Mathf.Tan(halfFOV) * near;
        float nearWidth = nearHeight * aspectRatio;
        float farHeight = 2f * Mathf.Tan(halfFOV) * far;
        float farWidth = farHeight * aspectRatio;

        // Near plane corners (in camera space)
        Vector3[] nearCorners = new Vector3[4]
        {
            new Vector3(-nearWidth * 0.5f, -nearHeight * 0.5f, near),  // Bottom-left
            new Vector3(nearWidth * 0.5f, -nearHeight * 0.5f, near),   // Bottom-right
            new Vector3(nearWidth * 0.5f, nearHeight * 0.5f, near),    // Top-right
            new Vector3(-nearWidth * 0.5f, nearHeight * 0.5f, near)    // Top-left
        };

        // Far plane corners (in camera space)
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
    /// Setup dei 12 LineRenderer per le linee del frustum
    /// </summary>
    void SetupLineRenderers()
    {
        for (int i = 0; i < 12; i++)
        {
            GameObject lineObj = new GameObject($"FrustumLine_{i:D2}");
            lineObj.transform.SetParent(transform);

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = CreateHologramMaterial();
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.sortingOrder = 100; // Sopra agli altri oggetti

            lineRenderers[i] = lr;
        }
    }

    /// <summary>
    /// Disegna le 12 linee del frustum
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
    /// Crea il materiale ologramma
    /// </summary>
    Material CreateHologramMaterial()
    {
        // Crea un materiale trasparente di base
        Material mat = new Material(Shader.Find("Sprites/Default"));

        // Prova a usare shader più avanzati se disponibili
        if (Shader.Find("Universal Render Pipeline/Unlit"))
            mat.shader = Shader.Find("Universal Render Pipeline/Unlit");
        else if (Shader.Find("Unlit/Color"))
            mat.shader = Shader.Find("Unlit/Color");

        mat.color = hologramColor;

        // Setup trasparenza
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
    /// Anima il glow dell'ologramma
    /// </summary>
    void AnimateHologramGlow()
    {
        float glow = 0.5f + 0.3f * Mathf.Sin(Time.time * glowSpeed);
        Color animatedColor = hologramColor * glow;

        for (int i = 0; i < 12; i++)
        {
            if (lineRenderers[i] != null && lineRenderers[i].material != null)
            {
                lineRenderers[i].material.color = animatedColor;
            }
        }
    }

    /// <summary>
    /// Metodo pubblico per aggiornare il frustum (chiamato dal gesture manager)
    /// </summary>
    public void UpdateFrustumFromCamera(Camera camera)
    {
        targetCamera = camera;
        CaptureCurrentFrustum();
        DrawFrustumLines();
    }

    void OnDrawGizmos()
    {
        if (showDebugSphere)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < 8; i++)
            {
                if (frustumCorners != null && i < frustumCorners.Length)
                {
                    Gizmos.DrawSphere(frustumCorners[i], 0.1f);
                }
            }

            // Disegna la posizione della camera
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(cameraPosition, 0.15f);
        }
    }

    /// <summary>
    /// Cleanup quando l'oggetto viene distrutto
    /// </summary>
    void OnDestroy()
    {
        // Pulisci i materiali per evitare memory leaks
        for (int i = 0; i < lineRenderers.Length; i++)
        {
            if (lineRenderers[i] != null && lineRenderers[i].material != null)
            {
                DestroyImmediate(lineRenderers[i].material);
            }
        }
    }
}