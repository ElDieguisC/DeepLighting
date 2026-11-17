using UnityEngine;

// Pon UNO por escena (en un GameObject vacío, ej. "MusicaTension").
// Mientras algún EnemyAI esté persiguiendo al jugador, sube la música de tensión;
// cuando todos lo pierden, la baja suavemente. Al morir el jugador se corta rápido.
// Opcional: baja el volumen de la música ambiente mientras suena la tensión.
[RequireComponent(typeof(AudioSource))]
public class TensionMusic : MonoBehaviour
{
    [Header("Música de tensión")]
    [SerializeField] private AudioClip musicaTension;
    [Range(0f, 1f)][SerializeField] private float volumen = 0.8f;
    [SerializeField] private float tiempoEntrada = 0.5f;  // Segundos en llegar al volumen máximo
    [SerializeField] private float tiempoSalida = 2.5f;   // Segundos en apagarse cuando te pierden
    [Tooltip("Espera antes de empezar a bajar, para que no se corte si te pierden un instante.")]
    [SerializeField] private float esperaAntesDeSalir = 1f;
    [SerializeField] private float tiempoSalidaAlMorir = 0.4f;

    [Header("Música ambiente (opcional)")]
    [Tooltip("AudioSources de la música normal que se bajan mientras suena la tensión (ej. el objeto 'Musica').")]
    [SerializeField] private AudioSource[] musicaAmbiente;
    [Range(0f, 1f)][SerializeField] private float volumenAmbienteDuranteTension = 0.2f;

    private AudioSource fuente;
    private float[] volumenesAmbiente;
    private float factor;           // 0 = sin tensión, 1 = tensión al máximo
    private float tiempoSinPersecucion;
    private bool jugadorMuerto;

    private void Awake()
    {
        fuente = GetComponent<AudioSource>();
        fuente.clip = musicaTension;
        fuente.loop = true;
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        fuente.volume = 0f;

        volumenesAmbiente = new float[musicaAmbiente != null ? musicaAmbiente.Length : 0];
        for (int i = 0; i < volumenesAmbiente.Length; i++)
            volumenesAmbiente[i] = musicaAmbiente[i] != null ? musicaAmbiente[i].volume : 0f;
    }

    private void OnEnable()  { PlayerGameOver.OnPlayerDied += AlMorir; }
    private void OnDisable() { PlayerGameOver.OnPlayerDied -= AlMorir; }

    private void AlMorir() { jugadorMuerto = true; }

    private void Update()
    {
        if (musicaTension == null) return;

        bool persiguiendo = EnemyAI.AnyChasing && !jugadorMuerto;
        tiempoSinPersecucion = persiguiendo ? 0f : tiempoSinPersecucion + Time.unscaledDeltaTime;

        // La espera solo cuenta si la tensión ya estaba sonando
        bool enEspera = factor > 0f && tiempoSinPersecucion < esperaAntesDeSalir;
        float objetivo = (persiguiendo || enEspera) && !jugadorMuerto ? 1f : 0f;
        float duracion = objetivo > factor ? tiempoEntrada : (jugadorMuerto ? tiempoSalidaAlMorir : tiempoSalida);
        factor = Mathf.MoveTowards(factor, objetivo, Time.unscaledDeltaTime / Mathf.Max(duracion, 0.01f));

        // Arranca desde el inicio cada persecución nueva; se detiene al quedar en silencio
        if (factor > 0f && !fuente.isPlaying) fuente.Play();
        else if (factor <= 0f && fuente.isPlaying) fuente.Stop();

        fuente.volume = volumen * factor;

        for (int i = 0; i < volumenesAmbiente.Length; i++)
        {
            if (musicaAmbiente[i] == null) continue;
            float bajo = volumenesAmbiente[i] * volumenAmbienteDuranteTension;
            musicaAmbiente[i].volume = Mathf.Lerp(volumenesAmbiente[i], bajo, factor);
        }
    }
}
