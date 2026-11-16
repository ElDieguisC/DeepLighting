using UnityEngine;
using UnityEngine.Rendering;

// Va en el jugador (junto a PlayerMovment).
// Cuando el jugador apaga su luz, el ambiente se oscurece y la niebla se vuelve
// mas densa y oscura, de forma gradual. Al encenderla, todo vuelve a como estaba
// configurado en la escena (Window > Rendering > Lighting).
public class DarknessController : MonoBehaviour
{
    [Header("Luz ambiental (con la luz apagada)")]
    [Tooltip("Multiplicador de la luz ambiental. 0 = negro total, 1 = sin cambio.")]
    [Range(0f, 1f)][SerializeField] private float ambienteOscuro = 0.15f;

    [Header("Niebla (con la luz apagada)")]
    [Tooltip("Multiplicador de la densidad de niebla. >1 = se ve menos lejos.")]
    [SerializeField] private float densidadNieblaOscura = 2.5f;
    [Tooltip("Multiplicador del color de la niebla. 0 = niebla negra, 1 = sin cambio.")]
    [Range(0f, 1f)][SerializeField] private float colorNieblaOscura = 0.3f;

    [Header("Otras luces de la escena (opcional)")]
    [Tooltip("Luces del mapa que tambien se atenuan (ej. una Directional Light).")]
    [SerializeField] private Light[] lucesEscena;
    [Range(0f, 1f)][SerializeField] private float lucesEscenaOscuro = 0.2f;

    [Header("Transicion")]
    [Tooltip("Segundos que tarda en oscurecerse o aclararse.")]
    [SerializeField] private float duracionTransicion = 1.5f;

    private PlayerMovment playerMovement;

    // Valores originales de la escena
    private SphericalHarmonicsL2 ambientProbeOriginal;
    private Color ambientLightOriginal;
    private float fogDensityOriginal;
    private Color fogColorOriginal;
    private float[] intensidadesOriginales;

    private float factor = 1f; // 1 = luz encendida, 0 = totalmente oscuro

    void Start()
    {
        playerMovement = GetComponent<PlayerMovment>();

        ambientProbeOriginal = RenderSettings.ambientProbe;
        ambientLightOriginal = RenderSettings.ambientLight;
        fogDensityOriginal = RenderSettings.fogDensity;
        fogColorOriginal = RenderSettings.fogColor;

        if (lucesEscena == null) lucesEscena = new Light[0];
        intensidadesOriginales = new float[lucesEscena.Length];
        for (int i = 0; i < lucesEscena.Length; i++)
            if (lucesEscena[i] != null) intensidadesOriginales[i] = lucesEscena[i].intensity;

        // Empezar ya en el estado correcto, sin transicion
        factor = LuzEncendida() ? 1f : 0f;
        Aplicar();
    }

    void Update()
    {
        float objetivo = LuzEncendida() ? 1f : 0f;
        if (Mathf.Approximately(factor, objetivo)) return;

        float paso = duracionTransicion > 0f ? Time.unscaledDeltaTime / duracionTransicion : 1f;
        factor = Mathf.MoveTowards(factor, objetivo, paso);
        Aplicar();
    }

    private bool LuzEncendida()
    {
        if (playerMovement == null || playerMovement.light == null) return true;
        return playerMovement.light.activeInHierarchy;
    }

    private void Aplicar()
    {
        // Curva suave para que la transicion no se sienta lineal
        float t = Mathf.SmoothStep(0f, 1f, factor);

        // Luz ambiental (funciona con modo Skybox, Gradient o Color)
        float amb = Mathf.Lerp(ambienteOscuro, 1f, t);
        RenderSettings.ambientProbe = ambientProbeOriginal * amb;
        RenderSettings.ambientLight = ambientLightOriginal * amb;

        // Niebla (solo si la escena la tiene activada)
        if (RenderSettings.fog)
        {
            RenderSettings.fogDensity = fogDensityOriginal * Mathf.Lerp(densidadNieblaOscura, 1f, t);
            Color c = fogColorOriginal * Mathf.Lerp(colorNieblaOscura, 1f, t);
            c.a = fogColorOriginal.a;
            RenderSettings.fogColor = c;
        }

        // Luces de la escena
        for (int i = 0; i < lucesEscena.Length; i++)
        {
            if (lucesEscena[i] == null) continue;
            lucesEscena[i].intensity = intensidadesOriginales[i] * Mathf.Lerp(lucesEscenaOscuro, 1f, t);
        }
    }

    // Restaurar la escena si se quita el script o se sale de Play
    void OnDisable()
    {
        if (intensidadesOriginales == null) return; // Start aun no corrio
        factor = 1f;
        Aplicar();
    }

    void OnEnable()
    {
        // Evita aplicar antes de que Start haya guardado los valores originales
        if (intensidadesOriginales != null)
        {
            factor = LuzEncendida() ? 1f : 0f;
            Aplicar();
        }
    }
}
