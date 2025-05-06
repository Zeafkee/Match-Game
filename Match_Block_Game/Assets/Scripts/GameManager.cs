using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [SerializeField]
    private int Rows;
    [SerializeField]
    private int Columns;
    [SerializeField]
    private int Colours;
    [SerializeField] private int A;
    [SerializeField] private int B;
    [SerializeField] private int C;
    [SerializeField]
    private GameObject cellPrefab;
    [SerializeField]
    private Cell[,] board;
    [SerializeField]
    private bool[,] visited;
    private int count;
    public Transform CanvasTransform;
    [SerializeField]
    private int BlastGroupCounter;
    [SerializeField]
    public List<List<Cell>> blastableGroups;
    private bool Animating;

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

    public void Start()
    {
        BlastGroupCounter = 0;
        blastableGroups = new List<List<Cell>>();
        CreateBoard(Rows, Columns);
        CheckConnectedBlocks();
    }

    void CreateBoard(int rows, int columns)
    {
        if (rows > 10 || columns > 10)
        {
            Debug.LogWarning("Rows and Columns can't be larger than 10");
            return;
        }

        board = new Cell[rows, columns];
        visited = new bool[rows, columns];
        float cellSize = 2.23f;

        int totalCells = rows * columns;

        int blocksPerColour = Mathf.CeilToInt((float)totalCells / Colours);

        int remainingCells = totalCells % Colours;

        List<int> availableColours = new List<int>();

        for (int i = 0; i < Colours; i++)
        {
            for (int j = 0; j < blocksPerColour; j++)
            {
                availableColours.Add(i);
            }
        }

        for (int i = 0; i < remainingCells; i++)
        {
            availableColours.Add(Random.Range(0, Colours));
        }

        ShuffleList(availableColours);

        int index = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (index >= availableColours.Count) break;

                int chosenColour = availableColours[index];
                index++;

                Vector2 position = new Vector2(col * cellSize, row * -cellSize);
                GameObject cellObject = Instantiate(cellPrefab, position, Quaternion.identity);
                cellObject.GetComponent<SpriteRenderer>().sortingOrder = rows - row - 1;
                cellObject.transform.SetParent(transform);

                Cell cellScript = cellObject.GetComponent<Cell>();
                cellScript.SetIndex(new Vector2Int(row, col));
                cellScript.SetColour(chosenColour);
                board[row, col] = cellScript;
            }
        }

        EnsureValidBoard(rows, columns);

        Camera.main.transform.position = new Vector3(((columns - 1) * cellSize) / 2, (rows - 1) * -cellSize / 2, Camera.main.transform.position.z);
    }

    void ShuffleList(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    void EnsureValidBoard(int rows, int columns)
    {
            if (blastableGroups.Count == 0)
            {
                Debug.Log("Deadlock detected. Swapping colors.");
            SwapColors( rows, columns);
            }
            
        
    }
    void SwapColors(int rows, int columns)
    {
        List<Vector2Int> selectedCells = new List<Vector2Int>();

        int numberOfCellsToSwap = Mathf.CeilToInt((float)(rows * columns) / Colours);

        for (int i = 0; i < numberOfCellsToSwap; i++)
        {
            int row = Random.Range(0, rows);
            int col = Random.Range(0, columns);

            int neighborRow = row;
            int neighborCol = col;

            if (col < columns - 1)
            {
                neighborCol = col + 1;
            }
            else
            {
                neighborCol = col - 1;
            }

            if (board[row, col] != null && board[neighborRow, neighborCol] != null)
            {
                Debug.Log("11111");
                Debug.Log(board[row, col].GetIndex());
                Debug.Log(board[neighborRow, neighborCol].GetIndex());
                Debug.Log(board[row, col].GetColour());
                Debug.Log(board[neighborRow, neighborCol].GetColour());
                int tempColor = board[row, col].GetColour();
                board[row, col].SetColour(board[neighborRow, neighborCol].GetColour());
                board[neighborRow, neighborCol].SetColour(tempColor);
            }
        }
    }


    public void CheckConnectedBlocks()
    {
        visited = new bool[Rows, Columns];
        blastableGroups.Clear();
        BlastGroupCounter = 0;

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                if (!visited[row, col])
                {
                    List<Cell> connectedBlocks = new List<Cell>();
                    count = 0;
                    DFS(row, col, board[row, col].GetColour(), connectedBlocks);

                    if (connectedBlocks.Count >= 2)
                    {
                        BlastGroupCounter++;
                        foreach (Cell cell in connectedBlocks)
                        {
                            cell.SetBlastable(true);
                            cell.SetBlastGroup(BlastGroupCounter);
                            cell.SetBlastableColour(count, A, B, C);
                        }

                        blastableGroups.Add(connectedBlocks);
                    }
                    else
                    {
                        connectedBlocks[0].SetBlastable(false);
                        connectedBlocks[0].SetBlastableColour(count,A, B, C);
                    }
                }
            }
        }
    }

    public void BlastGroup(List<Cell> group)
    {
        if (Animating) return;

        HashSet<int> affectedRows = new HashSet<int>();
        HashSet<int> affectedCols = new HashSet<int>();

        foreach (Cell cell in group)
        {
            Vector2Int cellIndex = cell.GetIndex();
            board[cellIndex.x, cellIndex.y] = null;
            Destroy(cell.gameObject);

            affectedRows.Add(cellIndex.x);
            affectedCols.Add(cellIndex.y);
        }

        ApplyGravity(affectedRows, affectedCols);
        StartCoroutine(WaitAndFill(0.6f));
    }

    private IEnumerator WaitAndFill(float delay)
    {
        Animating = true;
        FillEmptySpaces();
        yield return new WaitForSeconds(delay);
        CheckConnectedBlocks();
        Animating = false;
    }

    

    private void DFS(int row, int col, int colour, List<Cell> connectedBlocks)
    {
        if (row < 0 || col < 0 || row >= Rows || col >= Columns || visited[row, col] || board[row, col].GetColour() != colour)
            return;

        visited[row, col] = true;
        connectedBlocks.Add(board[row, col]);
        count++;

        DFS(row + 1, col, colour, connectedBlocks);
        DFS(row - 1, col, colour, connectedBlocks);
        DFS(row, col + 1, colour, connectedBlocks);
        DFS(row, col - 1, colour, connectedBlocks);
    }

    void ApplyGravity(HashSet<int> affectedRows, HashSet<int> affectedCols)
    {
        float cellSize = 2.23f;

        foreach (int col in affectedCols)
        {
            int emptyRow = Rows - 1;

            for (int row = Rows - 1; row >= 0; row--)
            {
                if (board[row, col] != null)
                {
                    if (emptyRow != row)
                    {
                        Vector2 startPosition = new Vector2(col * cellSize, row * -cellSize);
                        Vector2 endPosition = new Vector2(col * cellSize, emptyRow * -cellSize);

                        board[emptyRow, col] = board[row, col];
                        board[row, col] = null;

                        board[emptyRow, col].SetIndex(new Vector2Int(emptyRow, col));

                        StartCoroutine(AnimateDrop(board[emptyRow, col].transform, startPosition, endPosition));
                    }

                    emptyRow--;
                }
            }
        }

        foreach (int row in affectedRows)
        {
            for (int col = 0; col < Columns; col++)
            {
                if (board[row, col] == null)
                {
                    Vector2 startPosition = new Vector2(col * cellSize, (Rows + 1) * cellSize);
                    Vector2 endPosition = new Vector2(col * cellSize, row * -cellSize);

                    GameObject cellObject = Instantiate(cellPrefab, startPosition, Quaternion.identity, transform);
                    Cell newCell = cellObject.GetComponent<Cell>();
                    newCell.SetIndex(new Vector2Int(row, col));
                    newCell.SetColour(Random.Range(0, Colours));

                    board[row, col] = newCell;

                    StartCoroutine(AnimateDrop(cellObject.transform, startPosition, endPosition));
                }
            }
        }
    }

    void FillEmptySpaces()
    {
        float cellSize = 2.23f;

        for (int col = 0; col < Columns; col++)
        {
            for (int row = Rows - 1; row >= 0; row--)
            {
                if (board[row, col] == null)
                {
                    Vector2 startPosition = new Vector2(col * cellSize, (Rows + 1) * cellSize);
                    Vector2 endPosition = new Vector2(col * cellSize, row * -cellSize);

                    GameObject cellObject = Instantiate(cellPrefab, startPosition, Quaternion.identity, transform);

                    Cell newCell = cellObject.GetComponent<Cell>();
                    newCell.SetIndex(new Vector2Int(row, col));
                    newCell.SetColour(Random.Range(0, Colours));

                    board[row, col] = newCell;

                    StartCoroutine(AnimateDrop(cellObject.transform, startPosition, endPosition));
                }
            }
        }
    }

    private IEnumerator AnimateDrop(Transform cellTransform, Vector2 startPosition, Vector2 endPosition)
    {
        float duration = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < duration && cellTransform != null)
        {
            cellTransform.position = Vector2.Lerp(startPosition, endPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cellTransform.position = endPosition;
    }

    public void ClearBoard()
    {
        Debug.Log("clear");
        if (board != null)
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    if (board[row, col] != null)
                    {
                        Destroy(board[row, col].gameObject);
                    }
                }
            }
        }
        board = null;
        blastableGroups.Clear();
    }

    public void SetManager(ManagerData managerData)
    {
        this.Rows = managerData.Rows;
        this.Columns = managerData.Columns;
        this.Colours = managerData.Colours;
        this.A = managerData.A;
        this.B = managerData.B;
        this.C = managerData.C;
    }


    public ManagerData GetManager()
    {
        return new ManagerData(Rows, Columns, Colours, A, B, C);
    }

}
