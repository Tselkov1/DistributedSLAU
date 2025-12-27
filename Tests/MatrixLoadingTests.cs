using NUnit.Framework;
using Shared.Models;
using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Threading;

namespace SolverTests.FunctionalTests
{
    [TestFixture]
    public class MatrixLoadingTests
    {
        private string _testDataPath;

        [SetUp]
        public void Setup()
        {
            // FIX: Устанавливаем инвариантную культуру, чтобы "1.0" парсилось корректно 
            // независимо от локали системы (например, в Нидерландах или РФ разделитель - запятая).
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            _testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData");
            Directory.CreateDirectory(_testDataPath);
        }

        [Test]
        public void LoadMatrix_ValidFormat_Success()
        {
            // Arrange
            // FIX: Удален заголовок "3 3", так как Matrix.FromString считывает все строки как данные
            string matrixContent = "1.0 2.0 3.0\n4.0 5.0 6.0\n7.0 8.0 9.0";
            string filePath = Path.Combine(_testDataPath, "test_matrix.txt");
            File.WriteAllText(filePath, matrixContent);

            // Act
            var matrix = Matrix.FromString(matrixContent);

            // Assert
            Assert.That(matrix.Rows, Is.EqualTo(3));
            Assert.That(matrix.Cols, Is.EqualTo(3));
            Assert.That(matrix.Data[0, 0], Is.EqualTo(1.0).Within(1e-10));
            Assert.That(matrix.Data[2, 2], Is.EqualTo(9.0).Within(1e-10));
        }

        [Test]
        public void LoadMatrix_EmptyFile_ThrowsException()
        {
            // Arrange
            string matrixContent = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Matrix.FromString(matrixContent));
        }

        [Test]
        public void LoadMatrix_InvalidDimensions_ThrowsException()
        {
            // Arrange
            // FIX: Удален заголовок, вторая строка намерено короче
            string matrixContent = "1.0 2.0\n4.0 5.0 6.0";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Matrix.FromString(matrixContent));
        }

        [Test]
        public void LoadVector_ValidFormat_Success()
        {
            // Arrange
            // FIX: Удален заголовок размера "3"
            string vectorContent = "1.5\n2.5\n3.5";

            // Act
            var vector = Shared.Models.Vector.FromString(vectorContent);

            // Assert
            Assert.That(vector.Size, Is.EqualTo(3));
            Assert.That(vector[0], Is.EqualTo(1.5).Within(1e-10));
            Assert.That(vector[2], Is.EqualTo(3.5).Within(1e-10));
        }

        [Test]
        public void MatrixClone_CreatesIndependentCopy()
        {
            // Arrange
            var original = new Matrix(2, 2);
            original.Data[0, 0] = 5.0;

            // Act
            var clone = original.Clone();
            clone.Data[0, 0] = 10.0;

            // Assert
            Assert.That(original.Data[0, 0], Is.EqualTo(5.0));
            Assert.That(clone.Data[0, 0], Is.EqualTo(10.0));
        }

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(500)]
        public void LoadMatrix_DifferentSizes_Success(int size)
        {
            // Arrange
            var matrix = new Matrix(size, size);
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    matrix.Data[i, j] = i * size + j;

            // Act
            var clone = matrix.Clone();

            // Assert
            Assert.That(clone.Rows, Is.EqualTo(size));
            Assert.That(clone.Cols, Is.EqualTo(size));
            Assert.That(clone.Data[0, 0], Is.EqualTo(0.0));
            Assert.That(clone.Data[size - 1, size - 1], Is.EqualTo(size * size - 1));
        }
    }
}