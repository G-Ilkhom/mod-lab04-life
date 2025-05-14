using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Globalization;

namespace cli_life
{
    public class SimulationConfig
    {
        public int HorizontalUnits { get; set; }
        public int VerticalUnits { get; set; }
        public int UnitDimension { get; set; }
        public double InitialDensity { get; set; }

        public SimulationConfig() { }
        public SimulationConfig(int horizontalUnits, int verticalUnits, int unitDimension, double density)
        {
            HorizontalUnits = horizontalUnits;
            VerticalUnits = verticalUnits;
            UnitDimension = unitDimension;
            InitialDensity = density;
        }
    }

    public static class ConfigLoader
    {
        public static SimulationConfig LoadFromJson(string path)
        {
            string content = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SimulationConfig>(content)!;
        }
    }

    public static class FileManager
    {
        public static void SaveBoard(GridElement[,] grid, string path)
        {
            using var writer = new StreamWriter(path);
            int rows = grid.GetLength(1);
            int cols = grid.GetLength(0);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    writer.Write(grid[x, y].FluxState ? '1' : '0');
                }
                writer.WriteLine();
            }
        }

        public static void LoadBoard(GridElement[,] grid, string path)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
                for (int y = 0; y < grid.GetLength(1); y++)
                    grid[x, y].FluxState = false;

            var cachedLines = File.ReadAllLines(path);
            for (int y = 0; y < cachedLines.Length && y < grid.GetLength(1); y++)
            {
                var line = cachedLines[y];
                for (int x = 0; x < line.Length && x < grid.GetLength(0); x++)
                {
                    grid[x, y].FluxState = line[x] == '1';
                }
            }
        }

        public static void LoadFigure(GridElement[,] grid, string figurePath)
        {
            var cachedLines = File.ReadAllLines(figurePath);
            int verticalUnits = cachedLines.Length;
            int horizontalUnits = cachedLines[0].Length;
            var rand = new Random();
            int offsetX = rand.Next(0, grid.GetLength(0) - horizontalUnits);
            int offsetY = rand.Next(0, grid.GetLength(1) - verticalUnits);

            for (int ordinate = 0; ordinate < verticalUnits; ordinate++)
                for (int abscissa = 0; abscissa < horizontalUnits; abscissa++)
                    grid[offsetX + abscissa, offsetY + ordinate].FluxState = cachedLines[ordinate][abscissa] == '1';
        }
    }

    public class GridElement
    {
        public bool FluxState;
        public bool NextState;
    }

    public class LifeBoard
    {
        public GridElement[,] Grid;
        public readonly int UnitDimension;
        private readonly Random rnd;
        public bool[,] Visited;

        public int TotalColumns => Grid.GetLength(0);
        public int TotalRows => Grid.GetLength(1);

        public LifeBoard(int horizontalUnits, int verticalUnits, int size, double density, int? seed = null)
        {
            UnitDimension = size;
            int columns = (horizontalUnits + size - 1) / size;
            int rows = (verticalUnits + size - 1) / size;

            Grid = new GridElement[columns, rows];
            Visited = new bool[columns, rows];

            for (int x = 0; x < columns; x++)
                for (int y = 0; y < rows; y++)
                    Grid[x, y] = new GridElement();

            rnd = seed is not null ? new Random(seed.Value) : new Random();
            Randomize(density);
        }

        public void Randomize(double density)
        {
            foreach (var cell in Grid)
                cell.FluxState = rnd.NextDouble() < density;
        }

        public void ProgressFrame()
        {
            for (int gridX = 0; gridX < TotalColumns; gridX++)
                for (int gridY = 0; gridY < TotalRows; gridY++)
                {
                    int aliveNeighbors = 0;
                    foreach ((int dx, int dy) in NeighborOffsets())
                    {
                        int offsetx = (gridX + dx + TotalColumns) % TotalColumns;
                        int offsety = (gridY + dy + TotalRows) % TotalRows;
                        if (Grid[offsetx, offsety].FluxState) aliveNeighbors++;
                    }
                    var cell = Grid[gridX, gridY];
                    cell.NextState = cell.FluxState
                        ? (aliveNeighbors == 2 || aliveNeighbors == 3)
                        : (aliveNeighbors == 3);
                }

            foreach (var cell in Grid)
                cell.FluxState = cell.NextState;
        }

        public (int aliveCount, int clusters) Analyze()
        {
            int total = 0, groupCount = 0;
            Array.Clear(Visited, 0, Visited.Length);

            for (int x = 0; x < TotalColumns; x++)
                for (int y = 0; y < TotalRows; y++)
                    if (!Visited[x, y] && Grid[x, y].FluxState)
                    {
                        int size = FloodFillIterative(x, y);
                        total += size;
                        if (size > 1) groupCount++;
                    }

            return (total, groupCount);
        }

        private int FloodFillIterative(int startX, int startY)
        {
            int neighborAccumulator = 0;
            var stack = new Stack<(int, int)>();
            stack.Push((startX, startY));

            while (stack.Count > 0)
            {
                var (x, y) = stack.Pop();
                if (Visited[x, y] || !Grid[x, y].FluxState) continue;

                Visited[x, y] = true;
                neighborAccumulator++;

                foreach ((int dx, int dy) in NeighborOffsets())
                {
                    int offsetx = (x + dx + TotalColumns) % TotalColumns;
                    int offsety = (y + dy + TotalRows) % TotalRows;
                    if (!Visited[offsetx, offsety] && Grid[offsetx, offsety].FluxState)
                        stack.Push((offsetx, offsety));
                }
            }
            return neighborAccumulator;
        }

        private IEnumerable<(int, int)> NeighborOffsets()
        {
            for (int deltax = -1; deltax <= 1; deltax++)
                for (int deltay = -1; deltay <= 1; deltay++)
                    if (deltax != 0 || deltay != 0)
                        yield return (deltax, deltay);
        }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            string projectRoot = GetProjectRootDirectory();
            string configPath = Path.Combine(projectRoot, "Property.json");

            var game = new Game(configPath, silent: true);
            game.GenerateDensityData();
        }

        public static string GetProjectRootDirectory()
        {
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\.."));
        }
    }

    public class Game
    {
        private LifeBoard _board;
        private int _stableCounter = 0;
        private int _previousClusters = 0;
        private int _generation = 0;

        private readonly string _configPath;
        private readonly bool _silent;

        public Game(string configPath, bool silent = true)
        {
            _configPath = configPath;
            _silent = silent;
            var settings = ConfigLoader.LoadFromJson(_configPath);
            _board = new LifeBoard(
                settings.HorizontalUnits,
                settings.VerticalUnits,
                settings.UnitDimension,
                settings.InitialDensity
            );
        }

        public void Run()
        {
            while (true)
            {
                if (!_silent && !HandleInput()) break;

                if (_generation >= 100) break;

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
                    FileManager.SaveBoard(
                        _board.Grid,
                        Path.Combine(Program.GetProjectRootDirectory(), "Board.txt")
                    );
                    break;
                case ConsoleKey.L:
                    FileManager.LoadBoard(
                        _board.Grid,
                        Path.Combine(Program.GetProjectRootDirectory(), "Board.txt")
                    );
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
                FileManager.LoadFigure(
                    _board.Grid,
                    Path.Combine(Directory.GetCurrentDirectory(), "figures", file)
                );
        }

        private bool Update()
        {
            if (!_silent)
            {
                Console.Clear();
                Render();
                Console.WriteLine($"\nGeneration: {_generation}");
            }

            _generation++;
            var (alive, clusters) = _board.Analyze();

            if (!_silent)
                Console.WriteLine($"Alive: {alive}, Clusters: {clusters}");

            if (CheckStability(clusters))
            {
                if (!_silent)
                    Console.WriteLine("Stable state reached.");
                return true;
            }

            _board.ProgressFrame();
            return false;
        }

        private void Render()
        {
            var sb = new StringBuilder();
            for (int y = 0; y < _board.TotalRows; y++)
            {
                for (int x = 0; x < _board.TotalColumns; x++)
                    sb.Append(_board.Grid[x, y].FluxState ? '*' : ' ');
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
                if (_stableCounter >= 5) return true;
            }
            return false;
        }

        public void GenerateDensityData()
        {
            double density = 0.0, step = 0.02;
            var output = Path.Combine(Program.GetProjectRootDirectory(), "data.txt");
            File.WriteAllText(output, "Density Generation\n");

            var settings = ConfigLoader.LoadFromJson(_configPath);

            while (density <= 1.0 + 1e-9)
            {
                settings.InitialDensity = density;
                _board = new LifeBoard(
                    settings.HorizontalUnits,
                    settings.VerticalUnits,
                    settings.UnitDimension,
                    settings.InitialDensity
                );

                _generation = 0;
                _stableCounter = 0;
                _previousClusters = 0;

                while (true)
                {
                    bool finished = Update();
                    if (finished) break;
                }

                string densityStr = density.ToString("0.##", CultureInfo.InvariantCulture);
                File.AppendAllText(output,
                    $"{densityStr} {_generation}{Environment.NewLine}");

                density += step;
            }
        }
    }
}
