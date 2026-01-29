using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private bool blastable;
    [SerializeField] private int BlastGroup;
    [SerializeField] private Sprite Image;
    [SerializeField] private int colour;
    [SerializeField] private Vector2Int index;

    private SpriteRenderer spriteRenderer;

    public GameManager gameManager;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        gameManager = GameManager.Instance;
    }

    public void SetIndex(Vector2Int newIndex)
    {
        this.index = newIndex; 
    }

    public Vector2Int GetIndex()
    {
        return index;
    }
    public void SetBlastGroup(int group)
    {
        BlastGroup = group;
    }
    public int GetBlastGroup()
    {
        return BlastGroup;
    }

    public void SetColour(int colourIndex)
    {
        colour = colourIndex;
        Image = Colours.Instance.Sprites_default[colour];
        if (spriteRenderer != null)
            spriteRenderer.sprite = Image;
    }

    public int GetColour()
    {
        return colour;
    }

    public void SetBlastable(bool value)
    {
        blastable = value;
    }

    public bool GetBlastable()
    {
        return blastable;
    }

    public void SetBlastableColour(int count, int A, int B, int C)
    {
        int colour = GetColour();
        if (count < A)
        {
            Image = Colours.Instance.Sprites_default[colour];
        }

        else if (count >= A && count < B)
        {
            Image = Colours.Instance.Sprites_A[colour];
        }
        else if (count >= B && count < C)
        {
            Image = Colours.Instance.Sprites_B[colour];
        }
        else if (count >= C)
        {
            Image = Colours.Instance.Sprites_C[colour];
        }

        if(spriteRenderer != null)
            spriteRenderer.sprite = Image;
    }


    public void Blast()
    {
        if (blastable)
        {
            if (gameManager.blastableGroups == null) return;
            foreach (var group in gameManager.blastableGroups)
            {
                if (group.Contains(this))
                {
                    gameManager.BlastGroup(group); 
                    break;
                }
            }
        }
    }
}
