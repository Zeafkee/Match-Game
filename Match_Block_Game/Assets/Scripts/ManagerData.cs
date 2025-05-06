public class ManagerData
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public int Colours { get; set; }
    public int A { get; set; }
    public int B { get; set; }
    public int C { get; set; }

    public ManagerData(int rows, int columns, int colours, int a, int b, int c)
    {
        Rows = rows;
        Columns = columns;
        Colours = colours;
        A = a;
        B = b;
        C = c;
    }
   
}
