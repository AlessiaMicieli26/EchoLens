using UnityEngine;

public class menu_controller : MonoBehaviour
{
    public GameObject mainMenuPanel;   // Il pannello del menu
    public GameObject streamingSystem; // L’oggetto che gestisce lo streaming

    // Questo metodo lo colleghi al bottone Start
    public void StartApp()
    {
        // Nascondi il menu
        mainMenuPanel.SetActive(false);

        // Attiva lo streaming
        //streamingSystem.SetActive(true);
    }
}
