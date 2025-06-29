using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [SerializeField] private string localSceneName   = "LocalGameScene";
    [SerializeField] private string networkSceneName = "NetworkGameScene";


    public void OnLocalGamePressed()
    {
        SceneManager.LoadScene(localSceneName);
    }


    public void OnNetworkGamePressed()
    {
 
        SceneManager.LoadScene(networkSceneName);
    }

    public void OnQuitPressed()
    {
        Application.Quit();
    }
}
