using NUnit.Framework;
using Shared.Models;
using Shared.Utils;
using System.Linq;
using System.Globalization;
using System.Threading;

namespace SolverTests.FunctionalTests
{
    [TestFixture]
    public class StressCalculationTests
    {
        [SetUp]
        public void Setup()
        {
            // FIX: Устанавливаем инвариантную культуру для всех тестов в этом классе,
            // чтобы числа форматировались с точкой (1.23) и стандартным поведением ToString().
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [Test]
        public void ComputeResidual_ExactSolution_ReturnsZero()
        {
            // Arrange
            var onesVector = new Vector(new[] { 1.0, 1.0, 1.0 });
            var A = Matrix.CreateDiagonal(onesVector);

            var x = new Vector(new[] { 1.0, 2.0, 3.0 });
            var b = A.Multiply(x);

            // Act
            double residual = SolverResult.ComputeResidual(A, x, b);

            // Assert
            Assert.That(residual, Is.LessThan(1e-12));
        }

        [Test]
        public void ComputeResidual_ApproximateSolution_ReturnsNonZero()
        {
            // Arrange
            var A = new Matrix(2, 2);
            // Прямой доступ к Data классом Matrix
            A.Data[0, 0] = 2.0; A.Data[0, 1] = 1.0;
            A.Data[1, 0] = 1.0; A.Data[1, 1] = 3.0;

            var xApprox = new Vector(new[] { 1.1, 0.9 });
            var b = new Vector(new[] { 3.0, 3.0 });

            // Act
            double residual = SolverResult.ComputeResidual(A, xApprox, b);

            // Assert
            Assert.That(residual, Is.GreaterThan(1e-6));
        }

        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(500)]
        public void ComputeResidual_DifferentSizes_ComputesCorrectly(int n)
        {
            // Arrange
            // Генерируем вектор из n единиц
            var onesData = Enumerable.Repeat(1.0, n).ToArray();
            var x = new Vector(onesData);

            // Единичная матрица размера n
            var A = Matrix.CreateDiagonal(x);

            var b = A.Multiply(x);

            // Act
            double residual = SolverResult.ComputeResidual(A, x, b);

            // Assert
            Assert.That(residual, Is.LessThan(1e-12), $"Failed for size {n}");
        }

        [Test]
        public void FormatScientific_SmallNumber_FormatsCorrectly()
        {
            // Arrange
            double value = 1.23456e-8;

            // Act
            string formatted = MathHelper.FormatScientific(value);

            // Assert
            // Проверяем наличие 'e' или 'E' 
            Assert.That(formatted.ToLower(), Does.Contain("e"));

            // FIX: Добавлена проверка на формат с тремя нулями (-008), 
            // так как "-008" технически не содержит подстроку "-08" (из-за лишнего нуля посередине).
            Assert.That(formatted, Does.Contain("-08")
                                   .Or.Contain("-8")
                                   .Or.Contain("-008"));
        }
    }
}