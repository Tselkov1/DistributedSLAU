using System;
using System.Text;

namespace Shared.Models
{
    /// <summary>
    /// Представляет матрицу для СЛАУ
    /// </summary>
    /// <summary>
    /// Представляет матрицу для СЛАУ
    /// </summary>
    public class Matrix
    {
        public int Rows { get; private set; }
        public int Cols { get; private set; }
        public double[,] Data { get; private set; }

        public Matrix(int rows, int cols)
        {
            if (rows <= 0 || cols <= 0)
                throw new ArgumentException("Размеры матрицы должны быть положительными");

            Rows = rows;
            Cols = cols;
            Data = new double[rows, cols];
        }

        public Matrix(double[,] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Rows = data.GetLength(0);
            Cols = data.GetLength(1);
            Data = (double[,])data.Clone();
        }

        // --- ДОБАВЛЕН НОВЫЙ КОНСТРУКТОР ДЛЯ ДЕСЕРИАЛИЗАЦИИ ---
        /// <summary>
        /// Создаёт матрицу из зубчатого массива (double[][]), полученного из JSON.
        /// </summary>
        public Matrix(double[][] data)
        {
            if (data == null || data.Length == 0)
            {
                Rows = 0;
                Cols = 0;
                Data = new double[0, 0];
                return;
            }

            Rows = data.Length;
            Cols = data[0].Length;
            Data = new double[Rows, Cols];

            for (int i = 0; i < Rows; i++)
            {
                if (data[i].Length != Cols)
                    throw new ArgumentException("Все строки в зубчатом массиве должны иметь одинаковую длину.");
                for (int j = 0; j < Cols; j++)
                {
                    Data[i, j] = data[i][j];
                }
            }
        }

        // --- ДОБАВЛЕН НОВЫЙ МЕТОД ДЛЯ СЕРИАЛИЗАЦИИ ---
        /// <summary>
        /// Конвертирует матрицу в зубчатый массив (double[][]), который поддерживается JSON-сериализатором.
        /// </summary>
        public double[][] ToJaggedArray()
        {
            var result = new double[Rows][];
            for (int i = 0; i < Rows; i++)
            {
                result[i] = new double[Cols];
                for (int j = 0; j < Cols; j++)
                {
                    result[i][j] = Data[i, j];
                }
            }
            return result;
        }

        /// <summary>
        /// Получить подматрицу по строкам
        /// </summary>
        public Matrix GetSubMatrix(int startRow, int endRow)
        {
            if (startRow < 0 || endRow >= Rows || startRow > endRow)
                throw new ArgumentException("Некорректный диапазон строк");

            int rowCount = endRow - startRow + 1;
            var subMatrix = new Matrix(rowCount, Cols);

            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    subMatrix.Data[i, j] = Data[startRow + i, j];
                }
            }

            return subMatrix;
        }

        /// <summary>
        /// Умножение матрицы на вектор
        /// </summary>
        public Vector Multiply(Vector vector)
        {
            if (Cols != vector.Size)
                throw new ArgumentException("Размерность вектора не соответствует количеству столбцов матрицы");

            var result = new Vector(Rows);

            for (int i = 0; i < Rows; i++)
            {
                double sum = 0;
                for (int j = 0; j < Cols; j++)
                {
                    sum += Data[i, j] * vector[j];
                }
                result[i] = sum;
            }

            return result;
        }

        /// <summary>
        /// Транспонирование матрицы
        /// </summary>
        public Matrix Transpose()
        {
            var result = new Matrix(Cols, Rows);

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    result.Data[j, i] = Data[i, j];
                }
            }

            return result;
        }

        /// <summary>
        /// Создать диагональную матрицу
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
        /// Создать случайную симметричную положительно определённую матрицу
        /// </summary>
        public static Matrix CreateRandomSymmetricPositiveDefinite(int size, Random random = null)
        {
            random ??= new Random();
            var matrix = new Matrix(size, size);

            // Создаём случайную матрицу
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    matrix.Data[i, j] = random.NextDouble() * 10 - 5;
                }
            }

            // Делаем симметричной: A = A + A^T
            for (int i = 0; i < size; i++)
            {
                for (int j = i + 1; j < size; j++)
                {
                    double avg = (matrix.Data[i, j] + matrix.Data[j, i]) / 2;
                    matrix.Data[i, j] = avg;
                    matrix.Data[j, i] = avg;
                }
            }

            // Добавляем к диагонали для положительной определённости
            for (int i = 0; i < size; i++)
            {
                matrix.Data[i, i] += size;
            }

            return matrix;
        }

        /// <summary>
        /// Загрузить матрицу из строки
        /// </summary>
        public static Matrix FromString(string data)
        {
            var lines = data.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
                throw new ArgumentException("Пустые данные матрицы");

            int rows = lines.Length;
            var firstRow = lines[0].Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
            int cols = firstRow.Length;

            var matrix = new Matrix(rows, cols);

            for (int i = 0; i < rows; i++)
            {
                var values = lines[i].Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length != cols)
                    throw new ArgumentException($"Строка {i} имеет некорректное количество элементов");

                for (int j = 0; j < cols; j++)
                {
                    if (!double.TryParse(values[j], out double value))
                        throw new ArgumentException($"Некорректное значение в позиции [{i}, {j}]");

                    matrix.Data[i, j] = value;
                }
            }

            return matrix;
        }

        /// <summary>
        /// Сериализация в строку
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    sb.Append(Data[i, j].ToString("F6"));
                    if (j < Cols - 1)
                        sb.Append(" ");
                }
                if (i < Rows - 1)
                    sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Клонирование матрицы
        /// </summary>
        public Matrix Clone()
        {
            return new Matrix((double[,])Data.Clone());
        }
    }
}