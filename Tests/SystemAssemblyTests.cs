using NUnit.Framework;
using Shared.Models;
using Shared.Utils;
using System;

namespace SolverTests.FunctionalTests
{
    [TestFixture]
    public class SystemAssemblyTests
    {
        [Test]
        public void MatrixMultiplication_IdentityMatrix_ReturnsOriginalVector()
        {
            // Arrange
            var ones = new Vector(new[] { 1.0, 1.0, 1.0 });
            var identity = Matrix.CreateDiagonal(ones);

            var vector = new Vector(new[] { 1.0, 2.0, 3.0 });

            // Act
            var result = identity.Multiply(vector);

            // Assert
            for (int i = 0; i < 3; i++)
            {
                Assert.That(result[i], Is.EqualTo(vector[i]).Within(1e-10));
            }
        }

        [Test]
        public void IsSymmetric_SymmetricMatrix_ReturnsTrue()
        {
            // Arrange
            var matrix = new Matrix(3, 3);
            matrix.Data[0, 0] = 4.0; matrix.Data[0, 1] = 1.0; matrix.Data[0, 2] = 2.0;
            matrix.Data[1, 0] = 1.0; matrix.Data[1, 1] = 5.0; matrix.Data[1, 2] = 3.0;
            matrix.Data[2, 0] = 2.0; matrix.Data[2, 1] = 3.0; matrix.Data[2, 2] = 6.0;

            // Act
            bool isSymmetric = MathHelper.IsSymmetric(matrix);

            // Assert
            Assert.That(isSymmetric, Is.True);
        }

        [Test]
        public void IsSymmetric_AsymmetricMatrix_ReturnsFalse()
        {
            // Arrange
            var matrix = new Matrix(2, 2);
            matrix.Data[0, 0] = 1.0; matrix.Data[0, 1] = 2.0;
            matrix.Data[1, 0] = 3.0; matrix.Data[1, 1] = 4.0;

            // Act
            bool isSymmetric = MathHelper.IsSymmetric(matrix);

            // Assert
            Assert.That(isSymmetric, Is.False);
        }

        [Test]
        public void IsPositiveDefinite_PDMatrix_ReturnsTrue()
        {
            // Arrange
            var matrix = new Matrix(3, 3);
            matrix.Data[0, 0] = 10.0; matrix.Data[0, 1] = 1.0; matrix.Data[0, 2] = 1.0;
            matrix.Data[1, 0] = 1.0; matrix.Data[1, 1] = 10.0; matrix.Data[1, 2] = 1.0;
            matrix.Data[2, 0] = 1.0; matrix.Data[2, 1] = 1.0; matrix.Data[2, 2] = 10.0;

            // Act
            bool isPD = MathHelper.IsPositiveDefinite(matrix);

            // Assert
            Assert.That(isPD, Is.True);
        }

        [Test]
        public void VectorOperations_AddSubtract_Consistent()
        {
            // Arrange
            var v1 = new Vector(new[] { 1.0, 2.0, 3.0 });
            var v2 = new Vector(new[] { 4.0, 5.0, 6.0 });

            // Act
            var sum = v1.Add(v2);
            var diff = sum.Subtract(v2);

            // Assert
            for (int i = 0; i < 3; i++)
            {
                Assert.That(diff[i], Is.EqualTo(v1[i]).Within(1e-10));
            }
        }

        [Test]
        public void Serialization_ToJsonJaggedArray_ReturnsCorrectStructure()
        {
            // Arrange
            var matrix = new Matrix(2, 2);
            matrix.Data[0, 0] = 1.0; matrix.Data[0, 1] = 2.0;
            matrix.Data[1, 0] = 3.0; matrix.Data[1, 1] = 4.0;

            // Act
            var jagged = matrix.ToJaggedArray();

            // Assert
            Assert.That(jagged.Length, Is.EqualTo(2));
            Assert.That(jagged[0].Length, Is.EqualTo(2));
            Assert.That(jagged[0][1], Is.EqualTo(2.0));
            Assert.That(jagged[1][0], Is.EqualTo(3.0));
        }
    }
}