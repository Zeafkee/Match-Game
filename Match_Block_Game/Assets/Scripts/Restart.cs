using UnityEngine;

public class Restart : MonoBehaviour
{
    public void RestartScene()
    {
        GameManager.Instance.ClearBoard();
        GameManager.Instance.Start();
    }
    
}
