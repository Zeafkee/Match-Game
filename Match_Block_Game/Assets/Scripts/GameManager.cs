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

    private Queue<GameObject> pool = new Queue<GameObject>();

    private List<Transform> animTransforms = new List<Transform>(100);
    private List<Vector2> animStarts = new List<Vector2>(100);
    private List<Vector2> animEnds = new List<Vector2>(100);
    private List<float> animTimers = new List<float>(100);
    private const float AnimationDuration = 0.5f;

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

    private void Update()
    {
        ProcessAnimations();
    }

    private void ProcessAnimations()
    {
        if (animTransforms.Count == 0) return;

        for (int i = animTransforms.Count - 1; i >= 0; i--)
        {
            if (animTransforms[i] == null || !animTransforms[i].gameObject.activeSelf)
            {
                RemoveAnimationAt(i);
                continue;
            }

            animTimers[i] += Time.deltaTime;
            float t = animTimers[i] / AnimationDuration;

            if (t >= 1f)
            {
                animTransforms[i].position = animEnds[i];
                RemoveAnimationAt(i);
            }
            else
            {
                animTransforms[i].position = Vector2.Lerp(animStarts[i], animEnds[i], t);
            }
        }
    }

    private void RegisterDropAnimation(Transform t, Vector2 start, Vector2 end)
    {
        animTransforms.Add(t);
        animStarts.Add(start);
        animEnds.Add(end);
        animTimers.Add(0f);
    }

    private void RemoveAnimationAt(int index)
    {
        animTransforms.RemoveAt(index);
        animStarts.RemoveAt(index);
        animEnds.RemoveAt(index);
        animTimers.RemoveAt(index);
    }

    private GameObject GetFromPool(Vector2 position, Quaternion rotation)
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            return obj;
        }
        else
        {
            GameObject obj = Instantiate(cellPrefab, position, rotation, transform);
            return obj;
        }
    }

    private void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
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
                GameObject cellObject = GetFromPool(position, Quaternion.identity);
                cellObject.GetComponent<SpriteRenderer>().sortingOrder = rows - row - 1;

                if (cellObject.transform.parent != transform)
                    cellObject.transform.SetParent(transform);

                Cell cellScript = cellObject.GetComponent<Cell>();
                cellScript.SetIndex(new Vector2Int(row, col));
                cellScript.SetColour(chosenColour);
                board[row, col] = cellScript;
            }
        }

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
                        connectedBlocks[0].SetBlastableColour(count, A, B, C);
                    }
                }
            }
        }

        if (blastableGroups.Count == 0)
        {
            Debug.Log("Deadlock detected. Shuffling board.");
            ShuffleBoard();
        }
    }

    private void ShuffleBoard()
    {
        List<Cell> allCells = new List<Cell>();
        List<int> colors = new List<int>();

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                if (board[row, col] != null)
                {
                    allCells.Add(board[row, col]);
                    colors.Add(board[row, col].GetColour());
                }
            }
        }

        for (int i = 0; i < colors.Count; i++)
        {
            int temp = colors[i];
            int r = Random.Range(i, colors.Count);
            colors[i] = colors[r];
            colors[r] = temp;
        }

        for (int i = 0; i < allCells.Count; i++)
        {
            allCells[i].SetColour(colors[i]);
        }

        if (!HasAnyMatch())
        {
            ForceMatch(allCells);
        }
        ScanAfterShuffle();
    }

    private void ScanAfterShuffle()
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
                        connectedBlocks[0].SetBlastableColour(count, A, B, C);
                    }
                }
            }
        }
    }

    private bool HasAnyMatch()
    {
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                int color = board[r, c].GetColour();
                if (c + 1 < Columns && board[r, c + 1].GetColour() == color) return true;
                if (r + 1 < Rows && board[r + 1, c].GetColour() == color) return true;
            }
        }
        return false;
    }

    private void ForceMatch(List<Cell> allCells)
    {
        if (Rows < 2 && Columns < 2) return;

        Cell targetA = board[0, 0];
        Cell targetB = (Columns > 1) ? board[0, 1] : board[1, 0];

        int targetColor = targetA.GetColour();

        foreach (var cell in allCells)
        {
            if (cell == targetA || cell == targetB) continue;

            if (cell.GetColour() == targetColor)
            {
                int tempColor = targetB.GetColour();
                targetB.SetColour(targetColor);
                cell.SetColour(tempColor);
                return;
            }
        }

        targetB.SetColour(targetColor);
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
            
            ReturnToPool(cell.gameObject);

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
                        
                        board[emptyRow, col].GetComponent<SpriteRenderer>().sortingOrder = Rows - emptyRow - 1;

                        RegisterDropAnimation(board[emptyRow, col].transform, startPosition, endPosition);
                    }

                    emptyRow--;
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

                    GameObject cellObject = GetFromPool(startPosition, Quaternion.identity);

                    cellObject.GetComponent<SpriteRenderer>().sortingOrder = Rows - row - 1;

                    Cell newCell = cellObject.GetComponent<Cell>();
                    newCell.SetIndex(new Vector2Int(row, col));
                    newCell.SetColour(Random.Range(0, Colours));

                    board[row, col] = newCell;

                    RegisterDropAnimation(cellObject.transform, startPosition, endPosition);
                }
            }
        }
    }

    public void ClearBoard()
    {
        animTransforms.Clear();
        animStarts.Clear();
        animEnds.Clear();
        animTimers.Clear();

        Debug.Log("clear");
        if (board != null)
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    if (board[row, col] != null)
                    {
                        ReturnToPool(board[row, col].gameObject);
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