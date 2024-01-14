
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
    
public class MainMenuButtonManager : MonoBehaviour
{
    private void destoryThis()
    {
        gameObject.SetActive(false);
        
        // Destroy(gameObject);
        // foreach (Transform child in transform)
        // {
        //     Destroy(child.gameObject);
        // }
    }
    
    
    public void OnHostClicked()
    {
        Debug.Log("Hosting server...");
        
        // SceneManager.LoadScene("TunnelsMain");
        destoryThis();
        NetworkManager.Singleton.StartHost();
        destoryThis();
    }
    
    public void OnConnectClicked()
    {
        Debug.Log("Connecting to server...");
        
        // SceneManager.LoadScene("TunnelsMain");
        destoryThis();
        NetworkManager.Singleton.StartClient();
        destoryThis();
    }
}
