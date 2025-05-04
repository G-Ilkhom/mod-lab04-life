using Xunit;
using cli_life;
using System.Collections.Generic;
using System.Linq;

namespace Life.Tests
{
    public class LifeBoardTests
    {
        private LifeBoard CreateBoard(int w, int h) => new LifeBoard(w, h, 1, 0);

        private static HashSet<(int x, int y)> GetAlive(LifeBoard b)
        {
            var alive = new HashSet<(int x, int y)>();
            int width = b.Grid.GetLength(0);
            int height = b.Grid.GetLength(1);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (b.Grid[x, y].IsAlive)
                        alive.Add((x, y));
            return alive;
        }

        private static void AssertCellsEqual(IEnumerable<(int x, int y)> expected, HashSet<(int x, int y)> actual)
        {
            var exp = expected.OrderBy(c => c.x).ThenBy(c => c.y).ToList();
            var act = actual.OrderBy(c => c.x).ThenBy(c => c.y).ToList();
            Assert.Equal(exp, act);
        }

        [Fact]
        public void DeadBoard_RemainsEmpty()
        {
            var b = CreateBoard(5, 5);
            b.Advance();
            Assert.Empty(GetAlive(b));
        }

        [Fact]
        public void SingleCell_DiesNextGeneration()
        {
            var b = CreateBoard(5, 5);
            b.Grid[2, 2].IsAlive = true;
            b.Advance();
            Assert.Empty(GetAlive(b));
        }

        [Fact]
        public void PairCells_BothDie()
        {
            var b = CreateBoard(5, 5);
            b.Grid[1, 1].IsAlive = true;
            b.Grid[1, 2].IsAlive = true;
            b.Advance();
            Assert.Empty(GetAlive(b));
        }

        [Fact]
        public void ThreeHorizontal_FormsVertical()
        {
            var b = CreateBoard(5, 5);
            var init = new[] { (1, 2), (2, 2), (3, 2) };
            foreach (var (x, y) in init)
                b.Grid[x, y].IsAlive = true;
            b.Advance();
            var expectedVert = new[] { (2, 1), (2, 2), (2, 3) };
            AssertCellsEqual(expectedVert, GetAlive(b));
        }

        [Fact]
        public void ThreeVertical_Periodic_ReturnsToInitialAfterTwoAdvances()
        {
            var b = CreateBoard(5, 5);
            var init = new[] { (2, 1), (2, 2), (2, 3) };
            foreach (var (x, y) in init)
                b.Grid[x, y].IsAlive = true;
            b.Advance();
            b.Advance();
            AssertCellsEqual(init, GetAlive(b));
        }

        [Fact]
        public void Block_StaysStable()
        {
            var b = CreateBoard(4, 4);
            var init = new[] { (1, 1), (1, 2), (2, 1), (2, 2) };
            foreach (var c in init) b.Grid[c.Item1, c.Item2].IsAlive = true;
            b.Advance();
            AssertCellsEqual(init, GetAlive(b));
        }

        [Fact]
        public void Boat_StaysStable()
        {
            var b = CreateBoard(5, 5);
            var init = new[] { (1, 1), (2, 1), (1, 2), (3, 2), (2, 3) };
            foreach (var c in init) b.Grid[c.Item1, c.Item2].IsAlive = true;
            b.Advance();
            AssertCellsEqual(init, GetAlive(b));
        }

        [Fact]
        public void Tub_StaysStable()
        {
            var b = CreateBoard(5, 5);
            var init = new[] { (2, 1), (1, 2), (3, 2), (2, 3) };
            foreach (var c in init) b.Grid[c.Item1, c.Item2].IsAlive = true;
            b.Advance();
            AssertCellsEqual(init, GetAlive(b));
        }

        [Fact]
        public void Beehive_StaysStable()
        {
            var b = CreateBoard(6, 6);
            var init = new[] { (2, 1), (3, 1), (1, 2), (4, 2), (2, 3), (3, 3) };
            foreach (var c in init) b.Grid[c.Item1, c.Item2].IsAlive = true;
            b.Advance();
            AssertCellsEqual(init, GetAlive(b));
        }

        [Fact]
        public void Glider_MovesDiagonallyAfterFourGenerations()
        {
            var b = CreateBoard(5, 5);
            var glider = new[] { (1, 0), (2, 1), (0, 2), (1, 2), (2, 2) };
            foreach (var c in glider) b.Grid[c.Item1, c.Item2].IsAlive = true;
            for (int i = 0; i < 4; i++) b.Advance();
            var expected = new[] { (2, 1), (3, 2), (1, 3), (2, 3), (3, 3) };
            AssertCellsEqual(expected, GetAlive(b));
        }

        [Fact]
        public void WrapAround_DetectsNeighborsAcrossEdges()
        {
            var b = CreateBoard(3, 3);
            b.Grid[0, 0].IsAlive = true;
            b.Grid[2, 0].IsAlive = true;
            b.Advance();
            Assert.Empty(GetAlive(b));
        }

        [Fact]
        public void Analyze_FullBoardOneCluster()
        {
            var b = CreateBoard(3, 3);
            for (int x = 0; x < b.Grid.GetLength(0); x++)
                for (int y = 0; y < b.Grid.GetLength(1); y++)
                    b.Grid[x, y].IsAlive = true;
            var (alive, clusters) = b.Analyze();
            Assert.Equal(9, alive);
            Assert.Equal(1, clusters);
        }

        [Fact]
        public void Analyze_DisconnectedClusters()
        {
            var b = CreateBoard(6, 6);
            var clusterA = new[] { (1, 1), (1, 2) };
            var clusterB = new[] { (4, 4), (5, 4), (4, 5) };
            foreach (var c in clusterA.Concat(clusterB)) b.Grid[c.Item1, c.Item2].IsAlive = true;
            var (alive, clusters) = b.Analyze();
            Assert.Equal(5, alive);
            Assert.Equal(2, clusters);
        }

        [Fact]
        public void Analyze_MultipleClustersCount()
        {
            var b = CreateBoard(3, 3);
            var coords = new[] { (0, 0), (0, 1), (0, 2), (2, 2) };
            foreach (var c in coords) b.Grid[c.Item1, c.Item2].IsAlive = true;
            var (_, clusters) = b.Analyze();
            Assert.Equal(1, clusters);
        }

        [Fact]
        public void ToadOscillator_TogglesBetweenPhases()
        {
            var b = CreateBoard(6, 6);
            var phase1 = new[] { (2, 2), (3, 2), (4, 2), (1, 3), (2, 3), (3, 3) };
            foreach (var c in phase1) b.Grid[c.Item1, c.Item2].IsAlive = true;
            b.Advance();
            var phase2 = new[] { (1, 2), (1, 3), (2, 4), (3, 1), (4, 2), (4, 3) };
            AssertCellsEqual(phase2, GetAlive(b));
        }

        [Fact]
        public void BeaconOscillator_TogglesBetweenPhases()
        {
            var b = CreateBoard(6, 6);
            var phase1 = new[] { (1, 1), (2, 1), (1, 2), (2, 2), (3, 3), (4, 3), (3, 4), (4, 4) };
            foreach (var c in phase1) b.Grid[c.Item1, c.Item2].IsAlive = true;
            b.Advance();
            var phase2 = new[] { (1, 1), (2, 1), (1, 2), (4, 3), (3, 4), (4, 4) };
            AssertCellsEqual(phase2, GetAlive(b));
        }

        [Fact]
        public void Blinker_PeriodTwo_ReturnsToInitial()
        {
            var b = CreateBoard(5, 5);
            var initial = new[] { (1, 2), (2, 2), (3, 2) };
            foreach (var c in initial) b.Grid[c.Item1, c.Item2].IsAlive = true;
            b.Advance();
            b.Advance();
            AssertCellsEqual(initial, GetAlive(b));
        }
    }
}
