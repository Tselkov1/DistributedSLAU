using Shared.Models;
using System;

namespace Shared.Utils
{
    /// <summary>
    /// Вспомогательные математические функции
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// Проверка, является ли матрица симметричной
        /// </summary>
        public static bool IsSymmetric(Matrix matrix, double tolerance = 1e-10)
        {
            if (matrix.Rows != matrix.Cols)
                return false;

            for (int i = 0; i < matrix.Rows; i++)
            {
                for (int j = i + 1; j < matrix.Cols; j++)
                {
                    if (Math.Abs(matrix.Data[i, j] - matrix.Data[j, i]) > tolerance)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Проверка, является ли матрица положительно определённой (упрощённая проверка через диагональ)
        /// </summary>
        public static bool IsPositiveDefinite(Matrix matrix, double tolerance = 1e-10)
        {
            if (matrix.Rows != matrix.Cols)
                return false;

            // Упрощённая проверка: все диагональные элементы должны быть положительными
            // Для полной проверки нужно вычислять собственные значения или критерий Сильвестра
            for (int i = 0; i < matrix.Rows; i++)
            {
                if (matrix.Data[i, i] <= tolerance)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Вычисление нормы Фробениуса матрицы
        /// </summary>
        public static double FrobeniusNorm(Matrix matrix)
        {
            double sum = 0;
            for (int i = 0; i < matrix.Rows; i++)
            {
                for (int j = 0; j < matrix.Cols; j++)
                {
                    sum += matrix.Data[i, j] * matrix.Data[i, j];
                }
            }
            return Math.Sqrt(sum);
        }

        /// <summary>
        /// Вычисление числа обусловленности (приближённое через норму)
        /// </summary>
        public static double ConditionNumber(Matrix matrix)
        {
            // Упрощённая оценка: отношение максимального к минимальному диагональному элементу
            double maxDiag = double.MinValue;
            double minDiag = double.MaxValue;

            for (int i = 0; i < Math.Min(matrix.Rows, matrix.Cols); i++)
            {
                double val = Math.Abs(matrix.Data[i, i]);
                maxDiag = Math.Max(maxDiag, val);
                minDiag = Math.Min(minDiag, val);
            }

            return minDiag > 1e-10 ? maxDiag / minDiag : double.PositiveInfinity;
        }

        /// <summary>
        /// Создание единичной матрицы
        /// </summary>
        public static Matrix CreateIdentity(int size)
        {
            var matrix = new Matrix(size, size);
            for (int i = 0; i < size; i++)
            {
                matrix.Data[i, i] = 1.0;
            }
            return matrix;
        }

        /// <summary>
        /// Создание диагональной матрицы из вектора
        /// </summary>
        public static Matrix CreateDiagonal(Vector diagonal)
        {
            var matrix = new Matrix(diagonal.Size, diagonal.Size);
            for (int i = 0; i < diagonal.Size; i++)
            {
                matrix.Data[i, i] = diagonal[i];
            }
            return matrix;
        }

        /// <summary>
        /// Транспонирование матрицы
        /// </summary>
        public static Matrix Transpose(Matrix matrix)
        {
            var result = new Matrix(matrix.Cols, matrix.Rows);
            for (int i = 0; i < matrix.Rows; i++)
            {
                for (int j = 0; j < matrix.Cols; j++)
                {
                    result.Data[j, i] = matrix.Data[i, j];
                }
            }
            return result;
        }

        /// <summary>
        /// Сложение матриц
        /// </summary>
        public static Matrix Add(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Cols != b.Cols)
                throw new ArgumentException("Размеры матриц не совпадают");

            var result = new Matrix(a.Rows, a.Cols);
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < a.Cols; j++)
                {
                    result.Data[i, j] = a.Data[i, j] + b.Data[i, j];
                }
            }
            return result;
        }

        /// <summary>
        /// Умножение матрицы на скаляр
        /// </summary>
        public static Matrix Multiply(Matrix matrix, double scalar)
        {
            var result = new Matrix(matrix.Rows, matrix.Cols);
            for (int i = 0; i < matrix.Rows; i++)
            {
                for (int j = 0; j < matrix.Cols; j++)
                {
                    result.Data[i, j] = matrix.Data[i, j] * scalar;
                }
            }
            return result;
        }

        /// <summary>
        /// Умножение матриц
        /// </summary>
        public static Matrix Multiply(Matrix a, Matrix b)
        {
            if (a.Cols != b.Rows)
                throw new ArgumentException("Количество столбцов первой матрицы должно совпадать с количеством строк второй");

            var result = new Matrix(a.Rows, b.Cols);
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < b.Cols; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < a.Cols; k++)
                    {
                        sum += a.Data[i, k] * b.Data[k, j];
                    }
                    result.Data[i, j] = sum;
                }
            }
            return result;
        }

        /// <summary>
        /// Линейная интерполяция между двумя значениями
        /// </summary>
        public static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// Зажать значение в диапазон [min, max]
        /// </summary>
        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Проверка приближённого равенства чисел с плавающей точкой
        /// </summary>
        public static bool AreEqual(double a, double b, double tolerance = 1e-10)
        {
            return Math.Abs(a - b) < tolerance;
        }

        /// <summary>
        /// Вычисление среднего значения массива
        /// </summary>
        public static double Mean(double[] values)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentException("Массив не может быть пустым");

            double sum = 0;
            foreach (var value in values)
            {
                sum += value;
            }
            return sum / values.Length;
        }

        /// <summary>
        /// Вычисление стандартного отклонения
        /// </summary>
        public static double StandardDeviation(double[] values)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentException("Массив не может быть пустым");

            double mean = Mean(values);
            double sumSquaredDiff = 0;

            foreach (var value in values)
            {
                double diff = value - mean;
                sumSquaredDiff += diff * diff;
            }

            return Math.Sqrt(sumSquaredDiff / values.Length);
        }

        /// <summary>
        /// Форматирование числа в научной нотации
        /// </summary>
        public static string FormatScientific(double value, int precision = 2)
        {
            return value.ToString($"E{precision}");
        }

        /// <summary>
        /// Безопасное деление (возвращает 0 при делении на 0)
        /// </summary>
        public static double SafeDivide(double numerator, double denominator, double defaultValue = 0.0)
        {
            return Math.Abs(denominator) > 1e-14 ? numerator / denominator : defaultValue;
        }
    }
}