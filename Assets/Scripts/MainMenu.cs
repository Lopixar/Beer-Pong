
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject arHud;

    [Header("AR")]
    public TablePlacer tablePlacer;

    void Start()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        arHud.SetActive(false);

        tablePlacer.enabled = false;
    }

    public void PlayGame()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        arHud.SetActive(true);

        tablePlacer.enabled = true;
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void BackToMenu()
    {
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
    public void ExitGame()
    {
        Application.Quit();
    }
    public void ReturnToMenu()
    {
        // Hide the gameplay interface first.
        arHud.SetActive(false);

        // Stop AR placement.
        tablePlacer.enabled = false;

        // Remove the placed table.
        tablePlacer.UndoTable();

        // Show the main menu.
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

}
