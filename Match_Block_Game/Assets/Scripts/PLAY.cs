using UnityEngine;
using UnityEngine.SceneManagement;

public class PLAY : MonoBehaviour
{

    public void LoadNextScene()
    {
        int index = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(index + 1);
    }
}
