using NUnit.Framework;
using Server.Services;
using Shared.Models;
using Shared.Utils;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Moq;

namespace SolverTests.FunctionalTests
{
    [TestFixture]
    public class ConjugateGradientDistributedTests
    {
        // Хелпер для создания мока сервера
        private Mock<TcpMasterServer> CreateMockMaster(Matrix A)
        {
            // Передаем порт 5000 в конструктор, чтобы Moq мог создать экземпляр класса,
            // так как у TcpMasterServer нет конструктора без параметров.
            var mockMaster = new Mock<TcpMasterServer>(MockBehavior.Loose, 5000);

            // 1. Имитация инициализации
            mockMaster.Setup(m => m.InitializeWorkersAsync(It.IsAny<Matrix>(), It.IsAny<Vector>()))
                      .Returns(Task.CompletedTask);

            // 2. Имитация распределенного умножения матрицы на вектор (считаем локально)
            mockMaster.Setup(m => m.ComputeMatrixVectorAsync(It.IsAny<Vector>()))
                      .ReturnsAsync((Vector v) => A.Multiply(v));

            // 3. Имитация скалярного произведения (считаем локально)
            mockMaster.Setup(m => m.ComputeDotProductAsync(It.IsAny<Vector>(), It.IsAny<Vector>()))
                      .ReturnsAsync((Vector v1, Vector v2) => v1.Dot(v2));

            // 4. Имитация свойства Workers
            mockMaster.SetupGet(m => m.Workers).Returns(new List<WorkerNode>());

            return mockMaster;
        }

        [Test]
        public async Task SolveAsync_SymmetricPDMatrix_ConvergesToSolution()
        {
            // Arrange
            var A = new Matrix(3, 3);
            A.Data[0, 0] = 4.0; A.Data[0, 1] = 1.0; A.Data[0, 2] = 0.0;
            A.Data[1, 0] = 1.0; A.Data[1, 1] = 3.0; A.Data[1, 2] = 1.0;
            A.Data[2, 0] = 0.0; A.Data[2, 1] = 1.0; A.Data[2, 2] = 2.0;

            // ИСПРАВЛЕНИЕ: Генерируем b на основе ожидаемого решения {1,1,1}, 
            // чтобы математика сходилась. A * {1,1,1} = {5, 5, 3}.
            var xExpected = Vector.Ones(3);
            var b = A.Multiply(xExpected);

            var mockMaster = CreateMockMaster(A);

            var solver = new ConjugateGradientSolverDistributed(mockMaster.Object, A, b);

            // Act
            var result = await solver.SolveAsync(maxIterations: 1000, tolerance: 1e-6);

            // Assert
            Assert.That(result.Success, Is.True, "Решатель должен успешно завершиться");
            Assert.That(result.Residual, Is.LessThan(1e-6), "Невязка должна быть меньше допуска");

            // Проверяем отклонение от единичного вектора
            Assert.That(Math.Abs(result.Solution[0] - 1.0), Is.LessThan(1e-5), "x[0] неверный");
            Assert.That(Math.Abs(result.Solution[1] - 1.0), Is.LessThan(1e-5), "x[1] неверный");
            Assert.That(Math.Abs(result.Solution[2] - 1.0), Is.LessThan(1e-5), "x[2] неверный");
        }

        [Test]
        public async Task SolveAsync_Determinism_ProducesSameResult()
        {
            // Arrange
            int n = 50;
            var A = CreateDiagonallyDominantSymmetric(n, 0.5);
            var xTrue = Vector.Ones(n);
            var b = A.Multiply(xTrue);

            // Создаем два независимых решателя
            var mockMaster1 = CreateMockMaster(A);
            var solverInstance1 = new ConjugateGradientSolverDistributed(mockMaster1.Object, A, b);

            var mockMaster2 = CreateMockMaster(A);
            var solverInstance2 = new ConjugateGradientSolverDistributed(mockMaster2.Object, A, b);

            // Act
            var result1 = await solverInstance1.SolveAsync(maxIterations: 1000, tolerance: 1e-6);
            var result2 = await solverInstance2.SolveAsync(maxIterations: 1000, tolerance: 1e-6);

            // Assert
            Assert.That(result1.Success, Is.True);
            Assert.That(result2.Success, Is.True);

            double difference = 0.0;
            for (int i = 0; i < n; i++)
            {
                difference += Math.Abs(result1.Solution[i] - result2.Solution[i]);
            }
            Assert.That(difference / n, Is.LessThan(1e-10), "Результаты детерминированных запусков должны совпадать");
        }

        [TestCase(20, 500)]
        [TestCase(50, 1000)]
        public async Task SolveAsync_DifferentSizes_ConvergesSuccessfully(int n, int maxIterations)
        {
            // Arrange
            var A = CreateDiagonallyDominantSymmetric(n, 0.3);
            var xTrue = Vector.Ones(n);
            var b = A.Multiply(xTrue);

            var mockMaster = CreateMockMaster(A);
            var solver = new ConjugateGradientSolverDistributed(mockMaster.Object, A, b);

            // Act
            var result = await solver.SolveAsync(maxIterations: maxIterations, tolerance: 1e-6);

            // Assert
            Assert.That(result.Success, Is.True, $"Ошибка сходимости для матрицы размером {n}x{n}");
            Assert.That(result.Residual, Is.LessThan(1e-5));
            Assert.That(result.Iterations, Is.LessThanOrEqualTo(maxIterations));
        }

        [TestCase(50, 0.1)]
        [TestCase(50, 0.5)]
        public async Task SolveAsync_VaryingSparsity_ConvergesCorrectly(int n, double sparsity)
        {
            // Arrange
            var A = CreateDiagonallyDominantSymmetric(n, sparsity);
            var xTrue = Vector.Ones(n);
            var b = A.Multiply(xTrue);

            var mockMaster = CreateMockMaster(A);
            var solver = new ConjugateGradientSolverDistributed(mockMaster.Object, A, b);

            // Act
            var result = await solver.SolveAsync(maxIterations: 1000, tolerance: 1e-6);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Residual, Is.LessThan(1e-5));
        }

        private Matrix CreateDiagonallyDominantSymmetric(int n, double sparsity)
        {
            var matrix = new Matrix(n, n);
            var random = new Random(42); // Фиксированный seed для детерминизма
            int totalElements = n * n;
            int nonZeroCount = (int)(totalElements * sparsity);

            int count = 0;
            int safety = 0;
            while (count < nonZeroCount && safety < nonZeroCount * 10)
            {
                safety++;
                int i = random.Next(n);
                int j = random.Next(n);
                if (i != j && Math.Abs(matrix.Data[i, j]) < double.Epsilon)
                {
                    double value = random.NextDouble() * 2 - 1;
                    matrix.Data[i, j] = value;
                    matrix.Data[j, i] = value; // Симметрия
                    count += 2;
                }
            }

            for (int i = 0; i < n; i++)
            {
                double rowSum = 0;
                for (int j = 0; j < n; j++)
                {
                    if (i != j)
                        rowSum += Math.Abs(matrix.Data[i, j]);
                }
                // Диагональное преобладание
                matrix.Data[i, i] = rowSum + random.NextDouble() * 10 + 5;
            }

            return matrix;
        }
    }
}