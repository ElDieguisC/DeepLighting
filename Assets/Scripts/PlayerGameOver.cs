using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Va en el mismo GameObject que el jugador (el que tiene PlayerMovment + Rigidbody + Collider).
// Al chocar con cualquier objeto que tenga EnemyAI (en el o en un padre):
//   1. Aparece un marco rojo en los bordes de la pantalla.
//   2. La pantalla se oscurece hasta negro.
//   3. Se reinicia la escena automaticamente.
// No necesita configurar ninguna UI: el Canvas se crea por codigo.
public class PlayerGameOver : MonoBehaviour
{
    [Header("Marco rojo")]
    [SerializeField] private Color colorMarco = new Color(0.8f, 0f, 0f, 1f);
    [Tooltip("Que tan hacia el centro llega el rojo (0 = solo bordes, 1 = toda la pantalla).")]
    [Range(0.1f, 1f)][SerializeField] private float grosorMarco = 0.6f;
    [SerializeField] private float duracionMarco = 0.4f;   // Tiempo en aparecer el marco rojo
    [SerializeField] private float esperaConMarco = 0.8f;  // Tiempo que se queda el rojo antes de oscurecer

    [Header("Oscurecer pantalla")]
    [SerializeField] private float duracionFundido = 1.5f; // Tiempo en llegar a negro
    [SerializeField] private float esperaEnNegro = 0.5f;   // Tiempo en negro antes de reiniciar

    [Header("Apagar luz del personaje")]
    [Tooltip("Opcional: material que se pone al modelo cuando muere (ej. version sin emision).")]
    [SerializeField] private Material materialApagado;
    [SerializeField] private int parpadeos = 3;              // Parpadeos antes de apagarse
    [SerializeField] private float duracionApagado = 0.6f;   // Tiempo en que la luz baja a 0

    private bool isGameOver;
    private Image marcoRojo;
    private Image pantallaNegra;

    private void OnCollisionEnter(Collision collision)
    {
        CheckEnemy(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckEnemy(other);
    }

    private void CheckEnemy(Collider other)
    {
        if (isGameOver) return;

        if (other.GetComponentInParent<EnemyAI>() != null)
        {
            TriggerGameOver();
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // Desactivar el control del jugador
        PlayerMovment movement = GetComponent<PlayerMovment>();
        if (movement != null)
        {
            movement.enabled = false; // Tambien evita que se pueda volver a prender con F
            StartCoroutine(ApagarLuz(movement.light, movement.light_mat));
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        CrearOverlay();
        StartCoroutine(SecuenciaGameOver());
    }

    private IEnumerator SecuenciaGameOver()
    {
        // 1. Marco rojo
        yield return Fade(marcoRojo, 0f, 1f, duracionMarco);
        yield return new WaitForSecondsRealtime(esperaConMarco);

        // 2. Oscurecer
        yield return Fade(pantallaNegra, 0f, 1f, duracionFundido);
        yield return new WaitForSecondsRealtime(esperaEnNegro);

        // 3. Reiniciar escena
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // --- Parpadeo y apagado de la luz + emision del material ---
    private IEnumerator ApagarLuz(GameObject luzGO, Material mat)
    {
        Light[] luces = luzGO != null ? luzGO.GetComponentsInChildren<Light>(true) : new Light[0];
        float[] intensidades = new float[luces.Length];
        for (int i = 0; i < luces.Length; i++) intensidades[i] = luces[i].intensity;

        bool tieneEmision = mat != null && mat.HasProperty("_EmissionColor");
        Color emisionInicial = tieneEmision ? mat.GetColor("_EmissionColor") : Color.black;

        // 1. Parpadeo irregular, como una luz que falla
        for (int p = 0; p < parpadeos; p++)
        {
            SetLuz(luces, intensidades, mat, tieneEmision, emisionInicial, 0.1f);
            yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.12f));
            SetLuz(luces, intensidades, mat, tieneEmision, emisionInicial, 1f);
            yield return new WaitForSecondsRealtime(Random.Range(0.05f, 0.15f));
        }

        // 2. Bajar gradualmente hasta 0
        float t = 0f;
        while (t < duracionApagado)
        {
            t += Time.unscaledDeltaTime;
            SetLuz(luces, intensidades, mat, tieneEmision, emisionInicial, 1f - t / duracionApagado);
            yield return null;
        }
        SetLuz(luces, intensidades, mat, tieneEmision, emisionInicial, 0f);

        // 3. Apagar del todo
        if (luzGO != null) luzGO.SetActive(false);

        if (materialApagado != null)
        {
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null) rend.material = materialApagado;
        }
    }

    private void SetLuz(Light[] luces, float[] intensidades, Material mat, bool tieneEmision, Color emision, float factor)
    {
        for (int i = 0; i < luces.Length; i++) luces[i].intensity = intensidades[i] * factor;
        if (tieneEmision) mat.SetColor("_EmissionColor", emision * factor);
    }

    private IEnumerator Fade(Image img, float desde, float hasta, float duracion)
    {
        Color c = img.color;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(desde, hasta, t / duracion);
            img.color = c;
            yield return null;
        }
        c.a = hasta;
        img.color = c;
    }

    // --- Crea el Canvas con el marco rojo y la pantalla negra ---
    private void CrearOverlay()
    {
        GameObject canvasGO = new GameObject("GameOverOverlay");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Encima de cualquier otra UI

        marcoRojo = CrearImagen(canvasGO.transform, "MarcoRojo", colorMarco, CrearTexturaVineta());
        pantallaNegra = CrearImagen(canvasGO.transform, "PantallaNegra", Color.black, null);
    }

    private Image CrearImagen(Transform parent, string nombre, Color color, Sprite sprite)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        color.a = 0f; // Empieza invisible
        img.color = color;
        return img;
    }

    // Textura blanca transparente en el centro y opaca en los bordes (se tiñe con colorMarco)
    private Sprite CrearTexturaVineta()
    {
        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 centro = new Vector2(size / 2f, size / 2f);
        float radioMax = size / 2f * 1.414f; // Distancia del centro a la esquina
        float inicio = 1f - grosorMarco;     // Donde empieza a aparecer el rojo

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), centro) / radioMax;
                float a = Mathf.Clamp01((d - inicio) / (1f - inicio));
                a = a * a; // Transicion mas suave
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
