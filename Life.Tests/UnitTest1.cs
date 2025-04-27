using Xunit;
using cli_life;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Life.Tests
{
    public class CellTests
    {
        [Fact]
        public void Cell_DetermineNextLiveState1()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 1));
            cell.DetermineNextLiveState();
            Assert.False(cell.IsAliveNext);
        }

        [Fact]
        public void Cell_DetermineNextLiveState2()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 2));
            cell.DetermineNextLiveState();
            Assert.True(cell.IsAliveNext);
        }

        [Fact]
        public void Cell_DetermineNextLiveState3()
        {
            var cell = new Cell { IsAlive = false };
            cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 3));
            cell.DetermineNextLiveState();
            Assert.True(cell.IsAliveNext);
        }

        [Fact]
        public void Cell_DetermineNextLiveState4()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 4));
            cell.DetermineNextLiveState();
            Assert.False(cell.IsAliveNext);
        }

        [Fact]
        public void Cell_DetermineNextLiveState5()
        {
            var cell = new Cell { IsAlive = false };
            cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 3));
            cell.DetermineNextLiveState();
            Assert.True(cell.IsAliveNext);
        }
    }

    public class BoardTests
    {
        [Fact]
        public void Board_InitializesCorrectSize()
        {
            var board = new Board(100, 100, 10);
            Assert.Equal(10, board.Columns);
            Assert.Equal(10, board.Rows);
        }

        [Fact]
        public void BoardNeighbors()
        {
            var board = new Board(3, 3, 1);
            var centerCell = board.Cells[1, 1];
            Assert.Equal(8, centerCell.neighbors.Count);
        }

        [Fact]
        public void Randomize_SetsAliveCells()
        {
            var board = new Board(100, 100, 10);
            board.Randomize(0.5);
            Assert.Contains(board.Cells.Cast<Cell>(), c => c.IsAlive);
        }

        [Fact]
        public void CountElements1()
        {
            var board = new Board(6, 6, 1);
            for (int i = 0; i < 6; i++)
                for (int j = 0; j < 6; j++)
                    board.Cells[i, j].IsAlive = false;

            board.Cells[0, 0].IsAlive = true;
            board.Cells[0, 1].IsAlive = true;
            board.Cells[1, 0].IsAlive = true;
            board.Cells[1, 1].IsAlive = true;

            board.Cells[3, 1].IsAlive = true;
            board.Cells[3, 2].IsAlive = true;
            board.Cells[3, 3].IsAlive = true;

            var (count_cell, count_figures) = board.CountElements();
            Assert.Equal(7, count_cell);
            Assert.Equal(2, count_figures);
        }
    }

    public class TextControllerTests
    {
        readonly string tst_dir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Life");

        [Fact]
        public void Save_LifeFile()
        {
            var board = new Board(40, 20, 1);
            string filePath = Path.Combine(tst_dir, "LifeBoard.txt");
            board.Randomize(0.5);
            TextController.Save_life(board.Cells, filePath);
            Assert.True(File.Exists(filePath));
            string fileContent = File.ReadAllText(filePath);
            Assert.False(string.IsNullOrWhiteSpace(fileContent));
            Assert.Contains("1", fileContent);
        }

        [Fact]
        public void Read_LifeFile()
        {
            var board = new Board(40, 20, 1);
            string filePath = Path.Combine(tst_dir, "LifeBoard.txt");
            TextController.Read_life(board.Cells, filePath);
            Assert.True(File.Exists(filePath));
            Assert.NotNull(board.Cells);
        }
    }

    public class JSONControllerTests
    {
        readonly string tst_dir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Life");

        [Fact]
        public void Load_from_fson()
        {
            string property_path = Path.Combine(tst_dir, "Property.json");
            LifeProperty life_property = JSONController.Load_from_fson(property_path);
            Assert.Equal(40, life_property.BoardWidth);
        }
    }

    public class PatternClassifierTests
    {
        readonly string tst_dir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Life", "figures");

        [Fact]
        public void Classify_UnknownPattern()
        {
            var classifier = new PatternClassifier(tst_dir);
            var pattern = new bool[,] { { true, false }, { false, true } };
            string name = classifier.Classify(pattern);
            Assert.Equal("Unknown", name);
        }

        [Fact]
        public void Classify_BlockPattern()
        {
            var classifier = new PatternClassifier(tst_dir);
            var pattern = new bool[,] { { true, true }, { true, true } };
            string name = classifier.Classify(pattern);
            Assert.Equal("Block", name);
        }

        [Fact]
        public void Classify_BlinkerPattern()
        {
            var classifier = new PatternClassifier(tst_dir);
            var pattern = new bool[,] { { true }, { true }, { true } };
            string name = classifier.Classify(pattern);
            Assert.Equal("Blinker", name);
        }

        [Fact]
        public void Classify_Figures1()
        {
            var board = new Board(6, 6, 1);
            for (int i = 0; i < 6; i++)
                for (int j = 0; j < 6; j++)
                    board.Cells[i, j].IsAlive = false;

            board.Cells[0, 0].IsAlive = true;
            board.Cells[0, 1].IsAlive = true;
            board.Cells[1, 0].IsAlive = true;
            board.Cells[1, 1].IsAlive = true;

            board.Cells[2, 3].IsAlive = true;
            board.Cells[3, 3].IsAlive = true;
            board.Cells[4, 3].IsAlive = true;

            var classifier = new PatternClassifier(tst_dir);
            var results = classifier.ClassifyBoard(board);

            Dictionary<string, int> expected = new()
            {
                ["Block"] = 1,
                ["Blinker"] = 1,
                ["Hive"] = 0,
                ["Glider"] = 0,
                ["Boat"] = 0,
                ["Unknown"] = 0
            };

            Assert.Equal(expected, results);
        }

        [Fact]
        public void Classify_Figures2()
        {
            var board = new Board(7, 9, 1);
            for (int i = 0; i < 7; i++)
                for (int j = 0; j < 9; j++)
                    board.Cells[i, j].IsAlive = false;

            board.Cells[1, 2].IsAlive = true;
            board.Cells[1, 3].IsAlive = true;
            board.Cells[2, 1].IsAlive = true;
            board.Cells[2, 4].IsAlive = true;
            board.Cells[3, 2].IsAlive = true;
            board.Cells[3, 3].IsAlive = true;

            board.Cells[3, 7].IsAlive = true;
            board.Cells[4, 5].IsAlive = true;
            board.Cells[4, 7].IsAlive = true;
            board.Cells[5, 6].IsAlive = true;
            board.Cells[5, 7].IsAlive = true;

            board.Cells[0, 6].IsAlive = true;
            board.Cells[0, 7].IsAlive = true;
            board.Cells[1, 6].IsAlive = true;
            board.Cells[1, 7].IsAlive = true;

            var classifier = new PatternClassifier(tst_dir);
            var results = classifier.ClassifyBoard(board);

            Dictionary<string, int> expected = new()
            {
                ["Block"] = 1,
                ["Blinker"] = 0,
                ["Hive"] = 0,
                ["Glider"] = 1,
                ["Boat"] = 1,
                ["Unknown"] = 0
            };

            Assert.Equal(expected, results);
        }
    }
}
