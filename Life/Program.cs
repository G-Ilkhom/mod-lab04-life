using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Text.Json;

namespace cli_life
{
    public class GameSettings
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int CellSize { get; set; }
        public double InitialDensity { get; set; }

        public GameSettings() { }
        public GameSettings(int width, int height, int cellSize, double density)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            InitialDensity = density;
        }
    }

    public static class ConfigLoader
    {
        public static GameSettings LoadFromJson(string path)
        {
            string content = File.ReadAllText(path);
            return JsonSerializer.Deserialize<GameSettings>(content)!;
        }
    }

    public static class FileManager
    {
        public static void SaveBoard(Cell[,] grid, string path)
        {
            using var writer = new StreamWriter(path);
            int rows = grid.GetLength(1);
            int cols = grid.GetLength(0);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    writer.Write(grid[x, y].IsAlive ? '1' : '0');
                }
                writer.WriteLine();
            }
        }

        public static void LoadBoard(Cell[,] grid, string path)
        {
            var lines = File.ReadAllLines(path);
            for (int y = 0; y < lines.Length && y < grid.GetLength(1); y++)
            {
                for (int x = 0; x < lines[y].Length && x < grid.GetLength(0); x++)
                {
                    grid[x, y].IsAlive = lines[y][x] == '1';
                }
            }
        }

        public static void LoadFigure(Cell[,] grid, string figurePath)
        {
            var lines = File.ReadAllLines(figurePath);
            int height = lines.Length;
            int width = lines[0].Length;
            var rand = new Random();
            int offsetX = rand.Next(0, grid.GetLength(0) - width);
            int offsetY = rand.Next(0, grid.GetLength(1) - height);

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    grid[offsetX + x, offsetY + y].IsAlive = lines[y][x] == '1';
        }
    }

    public class Cell
    {
        public bool IsAlive;
        public bool NextState;
    }

    public class LifeBoard
    {
        public Cell[,] Grid;
        public readonly int CellSize;
        private readonly Random _rng = new();
        public bool[,] Visited;

        public int Columns => Grid.GetLength(0);
        public int Rows => Grid.GetLength(1);

        public LifeBoard(int width, int height, int size, double density)
        {
            CellSize = size;
            Grid = new Cell[width / size, height / size];

            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Grid[x, y] = new Cell();

            Randomize(density);
            Visited = new bool[Columns, Rows];
        }

        public void Randomize(double density)
        {
            foreach (var cell in Grid)
                cell.IsAlive = _rng.NextDouble() < density;
        }

        public void Advance()
        {
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                {
                    int aliveNeighbors = 0;
                    foreach ((int dx, int dy) in NeighborOffsets())
                    {
                        int nx = (x + dx + Columns) % Columns;
                        int ny = (y + dy + Rows) % Rows;
                        if (Grid[nx, ny].IsAlive) aliveNeighbors++;
                    }
                    var cell = Grid[x, y];
                    cell.NextState = cell.IsAlive ? aliveNeighbors is 2 or 3 : aliveNeighbors == 3;
                }

            foreach (var cell in Grid)
                cell.IsAlive = cell.NextState;
        }

        public (int aliveCount, int clusters) Analyze()
        {
            int total = 0, groupCount = 0;
            Array.Clear(Visited, 0, Visited.Length);

            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    if (!Visited[x, y] && Grid[x, y].IsAlive)
                    {
                        int size = FloodFillIterative(x, y);
                        total += size;
                        if (size > 1) groupCount++;
                    }

            return (total, groupCount);
        }

        private int FloodFillIterative(int startX, int startY)
        {
            int count = 0;
            Stack<(int, int)> stack = new();
            stack.Push((startX, startY));

            while (stack.Count > 0)
            {
                var (x, y) = stack.Pop();
                if (Visited[x, y] || !Grid[x, y].IsAlive) continue;

                Visited[x, y] = true;
                count++;

                foreach ((int dx, int dy) in NeighborOffsets())
                {
                    int nx = (x + dx + Columns) % Columns;
                    int ny = (y + dy + Rows) % Rows;
                    if (!Visited[nx, ny] && Grid[nx, ny].IsAlive)
                        stack.Push((nx, ny));
                }
            }
            return count;
        }

        private IEnumerable<(int, int)> NeighborOffsets()
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0)
                        yield return (dx, dy);
        }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            string baseDir = Directory.GetCurrentDirectory();
            string configPath = Path.Combine(baseDir, "Property.json");
            string savePath = Path.Combine(baseDir, "Board.txt");
            var game = new Game(configPath, savePath);
            game.GenerateDensityData();
            game.RunBatchAnalysis();
        }
    }

    public class Game
    {
        private LifeBoard _board;
        private int _stableCounter = 0;
        private int _previousClusters = 0;
        private int _generation = 0;
        private const int StabilityThreshold = 5;

        private readonly string _configPath;
        private readonly string _savePath;

        public Game(string configPath, string savePath)
        {
            _configPath = configPath;
            _savePath = savePath;
            var settings = ConfigLoader.LoadFromJson(configPath);
            _board = new LifeBoard(settings.Width, settings.Height, settings.CellSize, settings.InitialDensity);
        }

        public void Run()
        {
            while (true)
            {
                if (!HandleInput()) break;
                if (Update()) break;
            }
        }

        private bool HandleInput()
        {
            if (!Console.KeyAvailable) return true;
            var key = Console.ReadKey(true).Key;
            switch (key)
            {
                case ConsoleKey.S:
                    FileManager.SaveBoard(_board.Grid, _savePath);
                    break;
                case ConsoleKey.L:
                    FileManager.LoadBoard(_board.Grid, _savePath);
                    _stableCounter = 0;
                    _generation = 0;
                    break;
                case ConsoleKey.E:
                    return false;
                default:
                    TryLoadFigure(key);
                    break;
            }
            return true;
        }

        private void TryLoadFigure(ConsoleKey key)
        {
            var map = new Dictionary<ConsoleKey, string>
            {
                [ConsoleKey.D1] = "glider.txt",
                [ConsoleKey.D2] = "blinker.txt",
                [ConsoleKey.D3] = "block.txt",
                [ConsoleKey.D4] = "ellipse.txt",
                [ConsoleKey.D5] = "hive.txt"
            };

            if (map.TryGetValue(key, out var file))
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "figures", file);
                FileManager.LoadFigure(_board.Grid, path);
            }
        }

        private bool Update()
        {
            Console.Clear();
            Render();
            _generation++;
            Console.WriteLine($"\nGeneration: {_generation}");

            var (alive, clusters) = _board.Analyze();
            Console.WriteLine($"Alive cells: {alive}, Clusters: {clusters}");

            if (CheckStability(clusters))
            {
                Console.WriteLine("\n Stable state reached.");
                return true;
            }

            _board.Advance();
            Thread.Sleep(1000);
            return false;
        }

        private void Render()
        {
            var sb = new StringBuilder();
            for (int y = 0; y < _board.Rows; y++)
            {
                for (int x = 0; x < _board.Columns; x++)
                    sb.Append(_board.Grid[x, y].IsAlive ? '*' : ' ');
                sb.AppendLine();
            }
            Console.Write(sb);
        }

        private bool CheckStability(int clusters)
        {
            if (_stableCounter == 0 || _previousClusters != clusters)
            {
                _stableCounter = 1;
                _previousClusters = clusters;
            }
            else
            {
                _stableCounter++;
                if (_stableCounter >= StabilityThreshold)
                    return true;
            }
            return false;
        }

        public void RunBatchAnalysis()
        {
            double startDensity = 0.1;
            double maxDensity = 0.9;
            int samplesPerDensity = 10;

            var settings = ConfigLoader.LoadFromJson(_configPath);
            string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Results");
            Directory.CreateDirectory(outputDirectory);

            while (startDensity < maxDensity)
            {
                settings.InitialDensity = startDensity;
                string fileName = $"avg_res{startDensity:0.0}.txt";
                string filePath = Path.Combine(outputDirectory, fileName);
                List<int> generations = new();

                for (int i = 0; i < samplesPerDensity; i++)
                {
                    _board = new LifeBoard(settings.Width, settings.Height, settings.CellSize, settings.InitialDensity);
                    _generation = 0;
                    _stableCounter = 0;
                    _previousClusters = 0;
                    Run();
                    generations.Add(_generation);
                    File.AppendAllText(filePath, $"Run {i + 1}: Generations = {_generation}\n");
                }
                double avg = generations.Average();
                File.AppendAllText(filePath, $"Average Generations: {Math.Round(avg)}\n");
                startDensity += 0.1;
            }
        }

        public void GenerateDensityData()
        {
            double density = 0.0;
            double step = 0.02;
            string output = Path.Combine(Directory.GetCurrentDirectory(), "data.txt");
            File.WriteAllText(output, "Density  Generation\n");

            var settings = ConfigLoader.LoadFromJson(_configPath);

            while (density <= 1.0)
            {
                settings.InitialDensity = density;
                _board = new LifeBoard(settings.Width, settings.Height, settings.CellSize, settings.InitialDensity);
                _generation = 0;
                _stableCounter = 0;
                _previousClusters = 0;
                Run();
                File.AppendAllText(output, $"{Math.Round(density, 2)} {_generation}\n");
                density += step;
            }
        }
    }
}
