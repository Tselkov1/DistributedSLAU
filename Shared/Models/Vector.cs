using System;
using System.Linq;
using System.Text;

namespace Shared.Models
{
    /// <summary>
    /// Представляет вектор для СЛАУ
    /// </summary>
    public class Vector
    {
        public int Size { get; private set; }
        public double[] Data { get; private set; }

        public Vector(int size)
        {
            if (size <= 0)
                throw new ArgumentException("Размер вектора должен быть положительным");

            Size = size;
            Data = new double[size];
        }

        public Vector(double[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Size = data.Length;
            Data = (double[])data.Clone();
        }

        public double this[int index]
        {
            get
            {
                if (index < 0 || index >= Size)
                    throw new IndexOutOfRangeException();
                return Data[index];
            }
            set
            {
                if (index < 0 || index >= Size)
                    throw new IndexOutOfRangeException();
                Data[index] = value;
            }
        }

        /// <summary>
        /// Получить подвектор
        /// </summary>
        public Vector GetSubVector(int start, int end)
        {
            if (start < 0 || end >= Size || start > end)
                throw new ArgumentException("Некорректный диапазон");

            int length = end - start + 1;
            var subVector = new Vector(length);

            for (int i = 0; i < length; i++)
            {
                subVector.Data[i] = Data[start + i];
            }

            return subVector;
        }

        /// <summary>
        /// Скалярное произведение
        /// </summary>
        public double Dot(Vector other)
        {
            if (Size != other.Size)
                throw new ArgumentException("Размеры векторов не совпадают");

            double result = 0;
            for (int i = 0; i < Size; i++)
            {
                result += Data[i] * other.Data[i];
            }

            return result;
        }

        /// <summary>
        /// Сложение векторов
        /// </summary>
        public Vector Add(Vector other)
        {
            if (Size != other.Size)
                throw new ArgumentException("Размеры векторов не совпадают");

            var result = new Vector(Size);
            for (int i = 0; i < Size; i++)
            {
                result.Data[i] = Data[i] + other.Data[i];
            }

            return result;
        }

        /// <summary>
        /// Вычитание векторов
        /// </summary>
        public Vector Subtract(Vector other)
        {
            if (Size != other.Size)
                throw new ArgumentException("Размеры векторов не совпадают");

            var result = new Vector(Size);
            for (int i = 0; i < Size; i++)
            {
                result.Data[i] = Data[i] - other.Data[i];
            }

            return result;
        }

        /// <summary>
        /// Умножение на скаляр
        /// </summary>
        public Vector Multiply(double scalar)
        {
            var result = new Vector(Size);
            for (int i = 0; i < Size; i++)
            {
                result.Data[i] = Data[i] * scalar;
            }

            return result;
        }

        /// <summary>
        /// Евклидова норма (L2)
        /// </summary>
        public double Norm()
        {
            double sum = 0;
            for (int i = 0; i < Size; i++)
            {
                sum += Data[i] * Data[i];
            }
            return Math.Sqrt(sum);
        }

        /// <summary>
        /// Максимальная норма (L-infinity)
        /// </summary>
        public double MaxNorm()
        {
            return Data.Max(Math.Abs);
        }

        /// <summary>
        /// Создать нулевой вектор
        /// </summary>
        public static Vector Zero(int size)
        {
            return new Vector(size);
        }
        public static Vector Ones(int size)
        {
            var vector = new Vector(size);
            for (int i = 0; i < size; i++)
            {
                vector.Data[i] = 1.0;
            }
            return vector;
        }
        /// <summary>
        /// Создать случайный вектор
        /// </summary>
        public static Vector Random(int size, Random random = null)
        {
            random ??= new Random();
            var vector = new Vector(size);

            for (int i = 0; i < size; i++)
            {
                vector.Data[i] = random.NextDouble() * 100 - 50;
            }

            return vector;
        }

        /// <summary>
        /// Загрузить вектор из строки
        /// </summary>
        public static Vector FromString(string data)
        {
            var values = data.Split(new[] { ' ', '\t', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length == 0)
                throw new ArgumentException("Пустые данные вектора");

            var vector = new Vector(values.Length);

            for (int i = 0; i < values.Length; i++)
            {
                if (!double.TryParse(values[i], out double value))
                    throw new ArgumentException($"Некорректное значение в позиции {i}");

                vector.Data[i] = value;
            }

            return vector;
        }

        /// <summary>
        /// Сериализация в строку
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();

            for (int i = 0; i < Size; i++)
            {
                sb.Append(Data[i].ToString("F6"));
                if (i < Size - 1)
                    sb.Append(" ");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Клонирование вектора
        /// </summary>
        public Vector Clone()
        {
            return new Vector((double[])Data.Clone());
        }

        /// <summary>
        /// Оператор сложения
        /// </summary>
        public static Vector operator +(Vector a, Vector b)
        {
            return a.Add(b);
        }

        /// <summary>
        /// Оператор вычитания
        /// </summary>
        public static Vector operator -(Vector a, Vector b)
        {
            return a.Subtract(b);
        }

        /// <summary>
        /// Оператор умножения на скаляр
        /// </summary>
        public static Vector operator *(Vector v, double scalar)
        {
            return v.Multiply(scalar);
        }

        /// <summary>
        /// Оператор умножения на скаляр (обратный порядок)
        /// </summary>
        public static Vector operator *(double scalar, Vector v)
        {
            return v.Multiply(scalar);
        }
    }
}