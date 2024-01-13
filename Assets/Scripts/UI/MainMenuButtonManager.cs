
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButtonManager : MonoBehaviour
{
    public void OnHostClicked()
    {
        Debug.Log("Hosting server...");
        
        SceneManager.LoadScene("TunnelsMain", LoadSceneMode.Additive);
        NetworkManager.Singleton.StartHost();
        Destroy(gameObject);
    }
    
    public void OnConnectClicked()
    {
        Debug.Log("Connecting to server...");
        
        SceneManager.LoadScene("TunnelsMain", LoadSceneMode.Additive);
        NetworkManager.Singleton.StartClient();
        Destroy(gameObject);
    }
}
