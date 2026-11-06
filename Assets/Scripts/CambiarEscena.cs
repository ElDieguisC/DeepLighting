using UnityEngine;
using UnityEngine.SceneManagement; // Requerido para gestionar escenas

public class CambiarEscena : MonoBehaviour
{
    [SerializeField] private string DEMO; // Nombre de la escena a la que quieres ir

    // Se activa cuando otro objeto con Collider entra en el Trigger
    private void OnTriggerEnter(Collider other)
    {
        // Verifica si el objeto que entró es el jugador (debe tener la etiqueta "Player")
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(DEMO);
        }
    }
}