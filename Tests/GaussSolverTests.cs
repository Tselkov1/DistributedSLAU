using NUnit.Framework;
using Server.Services;
using Shared.Models;
using System;

namespace SolverTests.FunctionalTests
{
    [TestFixture]
    public class GaussSolverTests
    {
        private GaussSolverLocal _solver;

        [SetUp]
        public void Setup()
        {
            _solver = new GaussSolverLocal();
        }

        [Test]
        public void Solve_DiagonalMatrix_ReturnsCorrectSolution()
        {
            // Arrange - диагональная система 3x3
            var A = new Matrix(3, 3);
            A.Data[0, 0] = 2.0;
            A.Data[1, 1] = 3.0;
            A.Data[2, 2] = 4.0;
            var b = new Vector(new[] { 4.0, 9.0, 16.0 });
            var expected = new Vector(new[] { 2.0, 3.0, 4.0 });

            // Act
            var result = _solver.Solve(A, b);

            // Assert
            Assert.That(result.Success, Is.True);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(result.Solution[i], Is.EqualTo(expected[i]).Within(1e-6));
            }
        }

        [Test]
        public void Solve_TridiagonalSystem_ConvergesToSolution()
        {
            // Arrange - трёхдиагональная матрица 4x4
            var A = new Matrix(4, 4);
            A.Data[0, 0] = 4.0; A.Data[0, 1] = 1.0;
            A.Data[1, 0] = 1.0; A.Data[1, 1] = 4.0; A.Data[1, 2] = 1.0;
            A.Data[2, 1] = 1.0; A.Data[2, 2] = 4.0; A.Data[2, 3] = 1.0;
            A.Data[3, 2] = 1.0; A.Data[3, 3] = 4.0;
            var b = new Vector(new[] { 5.0, 6.0, 6.0, 5.0 });

            // Act
            var result = _solver.Solve(A, b);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Residual, Is.LessThan(1e-6));
        }

        [Test]
        public void Solve_SingularMatrix_ReturnsFailure()
        {
            // Arrange - вырожденная матрица (строки линейно зависимы)
            var A = new Matrix(3, 3);
            A.Data[0, 0] = 1.0; A.Data[0, 1] = 2.0; A.Data[0, 2] = 3.0;
            A.Data[1, 0] = 2.0; A.Data[1, 1] = 4.0; A.Data[1, 2] = 6.0;
            A.Data[2, 0] = 1.0; A.Data[2, 1] = 1.0; A.Data[2, 2] = 1.0;
            var b = new Vector(new[] { 6.0, 12.0, 3.0 });

            // Act
            var result = _solver.Solve(A, b);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("вырождена"));
        }

        [Test]
        public void Solve_AnalyticalTestCase_MatchesExpected()
        {
            // Arrange - известное аналитическое решение: [1, 2, 3]
            var A = new Matrix(3, 3);
            A.Data[0, 0] = 3.0; A.Data[0, 1] = 2.0; A.Data[0, 2] = 1.0;
            A.Data[1, 0] = 2.0; A.Data[1, 1] = 5.0; A.Data[1, 2] = 2.0;
            A.Data[2, 0] = 1.0; A.Data[2, 1] = 2.0; A.Data[2, 2] = 4.0;
            // FIX: Исправлено значение во второй строке с 19.0 на 18.0 (2*1 + 5*2 + 2*3 = 18)
            var b = new Vector(new[] { 10.0, 18.0, 17.0 });
            var expected = new Vector(new[] { 1.0, 2.0, 3.0 });

            // Act
            var result = _solver.Solve(A, b);

            // Assert
            Assert.That(result.Success, Is.True);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(result.Solution[i], Is.EqualTo(expected[i]).Within(1e-6));
            }
        }

        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(500)]
        public void Solve_DifferentMatrixSizes_CompletesSuccessfully(int n)
        {
            // Arrange - диагонально доминирующая матрица размера n
            var A = new Matrix(n, n);
            var random = new Random(42);

            for (int i = 0; i < n; i++)
            {
                double rowSum = 0;
                for (int j = 0; j < n; j++)
                {
                    if (i != j)
                    {
                        A.Data[i, j] = random.NextDouble() * 2 - 1;
                        rowSum += Math.Abs(A.Data[i, j]);
                    }
                }
                A.Data[i, i] = rowSum + 5.0 + random.NextDouble() * 5.0;
            }

            var xTrue = Vector.Ones(n);
            var b = A.Multiply(xTrue);

            // Act
            var startTime = DateTime.UtcNow;
            var result = _solver.Solve(A, b);
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Assert
            Assert.That(result.Success, Is.True, $"Failed for matrix size {n}x{n}");
            Assert.That(result.Residual, Is.LessThan(1e-5), $"Residual too high for size {n}");

            TestContext.WriteLine($"Size {n}x{n}: {elapsed:F2} ms, Residual: {result.Residual:E3}");
        }

        [TestCase(10, 1000)]
        [TestCase(50, 5000)]
        [TestCase(100, 10000)]
        [TestCase(200, 30000)]
        public void Solve_PerformanceTest_MeetsTimeConstraints(int n, double maxTimeMs)
        {
            // Arrange
            var A = new Matrix(n, n);
            var random = new Random(42);

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    A.Data[i, j] = (i == j) ? 10.0 : random.NextDouble();
                }
            }

            var b = Vector.Ones(n);

            // Act
            var startTime = DateTime.UtcNow;
            var result = _solver.Solve(A, b);
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(elapsed, Is.LessThan(maxTimeMs),
                $"Size {n}x{n} took {elapsed:F2} ms, expected < {maxTimeMs} ms");

            TestContext.WriteLine($"Size {n}x{n}: {elapsed:F2} ms (limit: {maxTimeMs} ms)");
        }
    }
}