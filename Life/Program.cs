using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace cli_life
{
    public class LifeProperty
    {
        public LifeProperty() { }
        public LifeProperty(int BoardWidth, int BoardHeight, int BoardCellSize, double LifeDensity)
        {
            this.BoardWidth = BoardWidth;
            this.BoardHeight = BoardHeight;
            this.BoardCellSize = BoardCellSize;
            this.LifeDensity = LifeDensity;
        }
        public int BoardWidth { get; set; }
        public int BoardHeight { get; set; }
        public int BoardCellSize { get; set; }
        public double LifeDensity { get; set; }
    }

    public class JSONController
    {
        public static LifeProperty Load_from_fson(string file_name)
        {
            string jsonString = File.ReadAllText(file_name);

            return JsonSerializer.Deserialize<LifeProperty>(jsonString)!;
        }
    }
    public class TextController
    {
        public static void Save_life(Cell[,] the_cells, string file_name)
        {
            using StreamWriter save_life = new StreamWriter(file_name);

            for (int row = 0; row < the_cells.GetLength(1); row++)
            {
                for (int col = 0; col < the_cells.GetLength(0); col++)
                {
                    save_life.Write(the_cells[col, row].IsAlive ? '1' : '0');
                }
                save_life.Write("\n");
            }
        }

        public static void Read_life(Cell[,] the_cells, string file_name)
        {
            var lines = File.ReadAllLines(file_name);
            int rows = the_cells.GetLength(1);
            int col = the_cells.GetLength(0);

            for (int y = 0; y < rows && y < lines.Length; y++)
            {
                for (int x = 0; x < col && x < lines[y].Length; x++)
                {
                    the_cells[x, y].IsAlive = lines[y][x] == '1';
                }
            }
        }

        public static void Load_figure(Cell[,] the_cells, string figureFilePath)
        {
            var lines = File.ReadAllLines(figureFilePath);
            int figureHeight = lines.Length;
            int figureWidth = lines[0].Length;

            Random rand = new();
            int startX = rand.Next(0, the_cells.GetLength(0) - figureWidth + 1);
            int startY = rand.Next(0, the_cells.GetLength(1) - figureHeight + 1);

            for (int y = 0; y < figureHeight; y++)
            {
                for (int x = 0; x < figureWidth; x++)
                {
                    if (startX + x < the_cells.GetLength(0) && startY + y < the_cells.GetLength(1))
                    {
                        the_cells[startX + x, startY + y].IsAlive = lines[y][x] == '1';
                    }
                }
            }
        }
    }
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = [];
        public bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }
    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;
        public bool[,] visited;

        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();
            ConnectNeighbors();
            Randomize(liveDensity);
            visited = new bool[Columns, Rows];
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }
        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }
        public (int totalCells, int combinations) CountElements()
        {
            int totalCells = 0;
            int combinations = 0;

            Array.Clear(visited, 0, visited.Length);

            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (!visited[x, y] && Cells[x, y].IsAlive)
                    {
                        int combinationSize = ExploreCombination(x, y);
                        totalCells += combinationSize;

                        if (combinationSize > 1)
                        {
                            combinations++;
                        }
                    }
                }
            }

            return (totalCells, combinations);
        }

        private int ExploreCombination(int x, int y)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows || visited[x, y] || !Cells[x, y].IsAlive)
                return 0;
            visited[x, y] = true;
            int size = 1;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = (x + dx + Columns) % Columns;
                    int ny = (y + dy + Rows) % Rows;

                    size += ExploreCombination(nx, ny);
                }
            }
            return size;
        }

    }

    public class PatternClassifier
    {
        private readonly string path_figure;
        private static readonly Dictionary<string, string> FigureFiles = new()
        {
            ["Block"] = "block.txt",
            ["Blinker"] = "blinker.txt",
            ["Hive"] = "hive.txt",
            ["Glider"] = "glider.txt",
            ["Boat"] = "ellipse.txt"
        };

        private readonly Dictionary<string, bool[,]> _patterns;
        private bool[,] _visited;

        public PatternClassifier(string path_figure)
        {
            this.path_figure = path_figure;
            _patterns = LoadPatterns(this.path_figure);
        }

        private Dictionary<string, bool[,]> LoadPatterns(string path_figure)
        {
            var patterns = new Dictionary<string, bool[,]>();

            foreach (var kvp in FigureFiles)
            {
                string path = Path.Combine(path_figure, kvp.Value);
                if (File.Exists(path))
                {
                    patterns[kvp.Key] = ReadPattern(path);
                }
            }
            return patterns;
        }

        private bool[,] ReadPattern(string path)
        {
            var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            var pattern = new bool[lines[0].Length, lines.Length];

            for (int y = 0; y < lines.Length; y++)
                for (int x = 0; x < lines[y].Length; x++)
                    pattern[x, y] = lines[y][x] == '1';

            return pattern;
        }

        public Dictionary<string, int> ClassifyBoard(Board board)
        {
            var result = _patterns.Keys.ToDictionary(k => k, _ => 0);
            result["Unknown"] = 0;
            _visited = new bool[board.Columns, board.Rows];

            for (int x = 0; x < board.Columns; x++)
                for (int y = 0; y < board.Rows; y++)
                    if (!_visited[x, y] && board.Cells[x, y].IsAlive)
                    {
                        var cells = FindCells(board, x, y);
                        if (cells.Count > 1)
                            result[Classify(CreatePattern(cells))]++;
                    }

            return result;
        }

        private List<(int x, int y)> FindCells(Board board, int startX, int startY)
        {
            var cells = new List<(int x, int y)>();
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((startX, startY));

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                if (x < 0 || x >= board.Columns || y < 0 || y >= board.Rows ||
                    _visited[x, y] || !board.Cells[x, y].IsAlive) continue;

                _visited[x, y] = true;
                cells.Add((x, y));

                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if (dx != 0 || dy != 0)
                            queue.Enqueue(((x + dx + board.Columns) % board.Columns,
                                        (y + dy + board.Rows) % board.Rows));
            }
            return cells;
        }

        private bool[,] CreatePattern(List<(int x, int y)> cells)
        {
            int minX = cells.Min(c => c.x), maxX = cells.Max(c => c.x);
            int minY = cells.Min(c => c.y), maxY = cells.Max(c => c.y);
            var pattern = new bool[maxX - minX + 1, maxY - minY + 1];

            foreach (var (x, y) in cells)
                pattern[x - minX, y - minY] = true;

            return pattern;
        }

        public string Classify(bool[,] pattern)
        {
            foreach (var kvp in _patterns)
                if (PatternEquals(pattern, kvp.Value))
                    return kvp.Key;

            return "Unknown";
        }

        private bool PatternEquals(bool[,] a, bool[,] b)
        {
            if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1))
                return false;

            for (int x = 0; x < a.GetLength(0); x++)
                for (int y = 0; y < a.GetLength(1); y++)
                    if (a[x, y] != b[x, y])
                        return false;

            return true;
        }
    }
    class Program
    {
        static Board board;
        static int stable_phases = 0;
        static int combinations = 0;
        static int generation = 0;
        static int min_stable_phases = 10;
        static readonly Dictionary<ConsoleKey, string> figureMap = new()
        {
            { ConsoleKey.D1, "glider.txt" },
            { ConsoleKey.D2, "blinker.txt" },
            { ConsoleKey.D3, "block.txt" },
            { ConsoleKey.D4, "ellipse.txt" },
            { ConsoleKey.D5, "hive.txt" }
        };

        static void Main(string[] args)
        {
            string proj_path = Directory.GetCurrentDirectory();
            string prop_path = Path.Combine(proj_path, "Property.json");
            string file_path = Path.Combine(proj_path, "LifeBoard.txt");
            LifeProperty life_property = JSONController.Load_from_fson(prop_path);
            Reset(life_property);
            RunGameLoop(file_path);
            //Avg_gen_stab(prop_path, file_path);
            //For_data(prop_path, file_path);

        }
        static void Avg_gen_stab(string prop_path, string file_path)
        {
            double density = 0.1;
            int number_files = 10;
            List<int> generation_density = [];
            LifeProperty life_property = JSONController.Load_from_fson(prop_path);

            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "AvgGenerationStab");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            while (density < 0.9)
            {
                life_property.LifeDensity = density;
                generation_density.Clear();
                string fileName = $"density_{density:0.0}.txt";
                string filePath = Path.Combine(outputDir, fileName);
                string line;

                for (int i = 0; i < number_files; i++)
                {
                    Reset(life_property);
                    generation = 0;
                    stable_phases = 0;
                    combinations = 0;
                    RunGameLoop(file_path);
                    generation_density.Add(generation);
                    line = $"{i + 1} - запуск: количество поколений: {generation}\n";
                    File.AppendAllText(filePath, line);
                }
                line = $"Среднее колличество поколений: {Math.Round(generation_density.Average())}\n";
                File.AppendAllText(filePath, line);
                density += 0.1;
            }
        }
        static void For_data(string prop_path, string file_path)
        {
            double density = 0;
            double step_density = 0.02;
            LifeProperty life_property = JSONController.Load_from_fson(prop_path);
            string outputfile = Path.Combine(Directory.GetCurrentDirectory(), "data.txt");
            string line = "Density  Generation\n";
            File.AppendAllText(outputfile, line);
            while (density < 1.01)
            {
                life_property.LifeDensity = density;
                Reset(life_property);
                generation = 0;
                stable_phases = 0;
                combinations = 0;
                RunGameLoop(file_path);
                line = $"{Math.Round(density, 2)} {generation}\n";
                File.AppendAllText(outputfile, line);
                density += step_density;
            }
        }

        static bool RunGameLoop(string file_path)
        {
            while (true)
            {
                if (!Click_handler(file_path))
                    break;

                if (UpdateGame()) return true;
            }
            return false;
        }

        static bool UpdateGame()
        {
            Render();
            generation += 1;
            Console.WriteLine($"\n Поколение: {generation}");
            (int totalCells, int combinations_check) = DisplayElementCounts();
            DisplayClassification();
            if (Check_stability(combinations_check))
            {
                Console.WriteLine("\n Достигнуто состояние стабильности");
                return true;
            }
            board.Advance();
            Thread.Sleep(1000);
            return false;
        }

        static private void Reset(LifeProperty LifeProperty)
        {
            board = new Board(
                LifeProperty.BoardWidth,
                LifeProperty.BoardHeight,
                LifeProperty.BoardCellSize,
                LifeProperty.LifeDensity);
        }

        static void Render()
        {
            Console.Clear();
            var output = new StringBuilder();
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    output.Append(board.Cells[col, row].IsAlive ? '*' : ' ');
                }
                output.AppendLine();
            }
            Console.Write(output);
        }

        static bool Click_handler(string file_path)
        {
            if (!Console.KeyAvailable)
                return true;
            var button = Console.ReadKey(true).Key;
            switch (button)
            {
                case ConsoleKey.L:
                    TextController.Read_life(board.Cells, file_path);
                    stable_phases = 0;
                    combinations = 0;
                    generation = 0;
                    break;
                case ConsoleKey.S:
                    TextController.Save_life(board.Cells, file_path);
                    break;
                case ConsoleKey.E:
                    return false;
                default:
                    HandleFigureLoading(button);
                    break;
            }
            return true;
        }

        static void HandleFigureLoading(ConsoleKey button)
        {
            if (figureMap.TryGetValue(button, out string figureName))
            {
                string figurePath = Path.Combine(Directory.GetCurrentDirectory(), "figures", figureName);
                TextController.Load_figure(board.Cells, figurePath);
            }
        }
        static (int totalCells, int combinations) DisplayElementCounts()
        {
            var (totalCells, combinations) = board.CountElements();
            Console.WriteLine($"Одиночные клетки: {totalCells}, Комбинации: {combinations}");
            return (totalCells, combinations);
        }
        static void DisplayClassification()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "figures");
            var classifier = new PatternClassifier(dir);
            var results = classifier.ClassifyBoard(board);

            Console.WriteLine("\n Найденные фигуры:");
            foreach (var kvp in results.Where(r => r.Value > 0))
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value}");
            }
        }
        static bool Check_stability(int combinations_check)
        {
            if (stable_phases == 0)
            {
                stable_phases += 1;
                combinations = combinations_check;
            }
            else
            {
                if (combinations == combinations_check)
                {
                    stable_phases += 1;
                    if (stable_phases == min_stable_phases)
                    {
                        return true;
                    }
                }
                else if (generation >= 1000 && Math.Abs(combinations - combinations_check) == 1)
                {
                    stable_phases += 1;
                    if (stable_phases == min_stable_phases)
                    {
                        return true;
                    }
                }
                else
                {
                    stable_phases = 1;
                    combinations = combinations_check;
                }
            }
            return false;
        }
    }
}
