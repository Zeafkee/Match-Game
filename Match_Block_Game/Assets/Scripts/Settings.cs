using System.Drawing;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;


public class Settings : MonoBehaviour
{
    public InputField row;
    public InputField column;
    public InputField colour;
    public InputField A;
    public InputField B;
    public InputField C;
    public Text errorMessageText;
    public GameObject PauseMenu;
    public int[] def;

    
    private void Start()
    {
        ManagerData def = GameManager.Instance.GetManager();

        row.text = def.Rows.ToString();
        column.text = def.Columns.ToString();
        colour.text = def.Colours.ToString();
        A.text = def.A.ToString();
        B.text = def.B.ToString();
        C.text = def.C.ToString();

    }
    public void Open(GameObject obj)
    {
        obj.SetActive(true);
    }
    public void Close(GameObject obj)
    {
        obj.SetActive(false);
    }
    public void Save_Restart()
    {
        int r, c, col, a, b, cc;
        
        // Parse inputs safely
        if (!int.TryParse(row.text, out r) || 
            !int.TryParse(column.text, out c) || 
            !int.TryParse(colour.text, out col) || 
            !int.TryParse(A.text, out a) || 
            !int.TryParse(B.text, out b) || 
            !int.TryParse(C.text, out cc))
        {
            errorMessageText.text = "Invalid Input: Please enter numbers only.";
            return;
        }

        if (r < 2 || r > 10 || c < 2 || c > 10)
        {
            errorMessageText.text = "Rows and Columns must be between 2 and 10!";
        }
        else if (col <= 0 || col > 6)
        {
            errorMessageText.text = "Colors must be between 1 and 6!";
        }
        else if (a <= 0)
        {
            errorMessageText.text = "A must be greater than 0!";
        }
        else if (b <= a)    
        {
            errorMessageText.text = "B must be greater than A!";
        }
        else if (cc <= b)
        {
            errorMessageText.text = "C must be greater than B!";
        }
        else
        {
            // Inputs are valid
            errorMessageText.text = "";
            GameManager.Instance.ClearBoard();
            
            GameManager.Instance.SetManager(new ManagerData(r, c, col, a, b, cc));
            
            Close(PauseMenu);
            GameManager.Instance.Start();
        }
    }
}