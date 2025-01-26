using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject creditsPanel;

    public void Start()
    {
        creditsPanel.SetActive(false);
    }

    public void ToggleCredits()
    {
        bool newValue = !creditsPanel.activeInHierarchy;
        creditsPanel.SetActive(newValue);
    }

    public void StartGame()
    {
        //1 has to be the main gameplay loop
        SceneManager.LoadScene(1);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
