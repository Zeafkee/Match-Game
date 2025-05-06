using UnityEngine;

public class Colours : MonoBehaviour
{
    public static Colours Instance;
    public Sprite[] Sprites_default;
    public Sprite[] Sprites_A;
    public Sprite[] Sprites_B;
    public Sprite[] Sprites_C;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
