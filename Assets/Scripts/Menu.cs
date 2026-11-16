using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public void ComenzarJuego() 
    {
        SceneManager.LoadScene(1);
    }

    public void SalirJuego()
    {
        Debug.Log("Salir");
        Application.Quit();
    }
}
