using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Models
{
    public enum MessageType
    {
        Initialize,
        ComputeMatrixVector,
        ComputeDotProduct,
        UpdateVector,
        Result,
        Error,
        Shutdown
    }

    public class TcpMessage
    {
        [JsonPropertyName("type")]
        public MessageType Type { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("workerId")]
        public int WorkerId { get; set; }

        [JsonPropertyName("matrixData")]
        public double[][] MatrixData { get; set; }

        [JsonPropertyName("vectorData")]
        public double[] VectorData { get; set; }

        [JsonPropertyName("vector2Data")]
        public double[] Vector2Data { get; set; }

        [JsonPropertyName("scalar")]
        public double Scalar { get; set; }

        [JsonPropertyName("resultData")]
        public double[] ResultData { get; set; }

        [JsonPropertyName("dotProductResult")]
        public double DotProductResult { get; set; }

        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        public TcpMessage()
        {
            RequestId = Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow;
        }

        // --- БИНАРНАЯ СЕРИАЛИЗАЦИЯ (Решение проблемы больших матриц) ---

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write((int)Type);
            writer.Write(RequestId ?? string.Empty);
            writer.Write(WorkerId);
            writer.Write(Timestamp.ToBinary());

            // Матрица
            bool hasMatrix = MatrixData != null;
            writer.Write(hasMatrix);
            if (hasMatrix)
            {
                int rows = MatrixData.Length;
                int cols = rows > 0 ? MatrixData[0].Length : 0;
                writer.Write(rows);
                writer.Write(cols);

                // Пишем построчно, используя BlockCopy для скорости
                // double - это 8 байт
                byte[] rowBuffer = new byte[cols * 8];
                for (int i = 0; i < rows; i++)
                {
                    Buffer.BlockCopy(MatrixData[i], 0, rowBuffer, 0, rowBuffer.Length);
                    writer.Write(rowBuffer);
                }
            }

            // Векторы и скаляры
            WriteVector(writer, VectorData);
            WriteVector(writer, Vector2Data);
            writer.Write(Scalar);
            WriteVector(writer, ResultData);
            writer.Write(DotProductResult);

            writer.Write(ErrorMessage ?? string.Empty);
        }

        public static TcpMessage ReadFrom(BinaryReader reader)
        {
            var msg = new TcpMessage();
            msg.Type = (MessageType)reader.ReadInt32();
            msg.RequestId = reader.ReadString();
            msg.WorkerId = reader.ReadInt32();
            msg.Timestamp = DateTime.FromBinary(reader.ReadInt64());

            // Матрица
            bool hasMatrix = reader.ReadBoolean();
            if (hasMatrix)
            {
                int rows = reader.ReadInt32();
                int cols = reader.ReadInt32();

                msg.MatrixData = new double[rows][];
                int rowBytesCount = cols * 8; // 8 байт на double

                for (int i = 0; i < rows; i++)
                {
                    msg.MatrixData[i] = new double[cols];
                    byte[] rowBytes = reader.ReadBytes(rowBytesCount);
                    Buffer.BlockCopy(rowBytes, 0, msg.MatrixData[i], 0, rowBytes.Length);
                }
            }

            msg.VectorData = ReadVector(reader);
            msg.Vector2Data = ReadVector(reader);
            msg.Scalar = reader.ReadDouble();
            msg.ResultData = ReadVector(reader);
            msg.DotProductResult = reader.ReadDouble();

            msg.ErrorMessage = reader.ReadString();
            if (string.IsNullOrEmpty(msg.ErrorMessage)) msg.ErrorMessage = null;

            return msg;
        }

        // Вспомогательные методы для векторов
        private void WriteVector(BinaryWriter w, double[] v)
        {
            if (v == null)
            {
                w.Write(0);
                return;
            }
            w.Write(v.Length);
            byte[] bytes = new byte[v.Length * 8];
            Buffer.BlockCopy(v, 0, bytes, 0, bytes.Length);
            w.Write(bytes);
        }

        private static double[] ReadVector(BinaryReader r)
        {
            int len = r.ReadInt32();
            if (len == 0) return null;

            byte[] bytes = r.ReadBytes(len * 8);
            double[] v = new double[len];
            Buffer.BlockCopy(bytes, 0, v, 0, bytes.Length);
            return v;
        }

        // --- JSON методы (оставляем для совместимости, если где-то нужны логи) ---
        public string ToJson()
        {
            var options = new JsonSerializerOptions { WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            return JsonSerializer.Serialize(this, options);
        }

        public static TcpMessage FromJson(string json)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<TcpMessage>(json, options);
        }

        // --- Фабричные методы (остаются без изменений) ---

        public static TcpMessage CreateInitialize(int workerId, Matrix matrix, Vector vector)
        {
            return new TcpMessage
            {
                Type = MessageType.Initialize,
                WorkerId = workerId,
                MatrixData = matrix.ToJaggedArray(),
                VectorData = vector.Data
            };
        }

        public static TcpMessage CreateComputeMatrixVector(int workerId, Vector p)
        {
            return new TcpMessage
            {
                Type = MessageType.ComputeMatrixVector,
                WorkerId = workerId,
                VectorData = p.Data
            };
        }

        public static TcpMessage CreateComputeDotProduct(int workerId, Vector v1, Vector v2)
        {
            return new TcpMessage
            {
                Type = MessageType.ComputeDotProduct,
                WorkerId = workerId,
                VectorData = v1.Data,
                Vector2Data = v2.Data
            };
        }

        public static TcpMessage CreateUpdateVector(int workerId, Vector vector)
        {
            return new TcpMessage
            {
                Type = MessageType.UpdateVector,
                WorkerId = workerId,
                VectorData = vector.Data
            };
        }

        public static TcpMessage CreateResult(int workerId, double[] result = null, double dotProduct = 0)
        {
            return new TcpMessage
            {
                Type = MessageType.Result,
                WorkerId = workerId,
                ResultData = result,
                DotProductResult = dotProduct
            };
        }

        public static TcpMessage CreateError(int workerId, string errorMessage)
        {
            return new TcpMessage
            {
                Type = MessageType.Error,
                WorkerId = workerId,
                ErrorMessage = errorMessage
            };
        }

        public static TcpMessage CreateShutdown()
        {
            return new TcpMessage
            {
                Type = MessageType.Shutdown
            };
        }
    }
}