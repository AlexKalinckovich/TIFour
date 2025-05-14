using System.IO;
using System.Numerics;
using TIFour.Utils;

namespace TIFour.Services;

public static class HashService
{
    private const int DefaultBufferSize = 4096;
    private static readonly BigInteger InitialHash = 100;

    /// <summary>
    /// Вычисляет хеш-функцию по формуле H_i = (H_{i-1} + M_i)^2 mod n для всего файла.
    /// </summary>
    /// <param name="filePath">Путь к файлу любых типов.</param>
    /// <param name="n">Результат умножения p * q</param>
    /// <exception cref="FileNotFoundException">Файла по данному пути не существует</exception>
    /// <returns>Хеш (BigInteger).</returns>
    public static BigInteger ComputeHash(in string filePath, in BigInteger n)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Файл не найден.", filePath);
        

        using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        return ComputeByteStreamHash(stream, n);
    }

    public static BigInteger ComputeByteStreamHash(in Stream stream, in BigInteger modulus)
    {
        BigInteger currentHash = InitialHash;
        
        byte[] buffer = new byte[DefaultBufferSize];
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < bytesRead; i++)
            {
                BigInteger blockValue = buffer[i]; 
                currentHash = MathEngine.SumSquareMod(currentHash, blockValue, modulus);
            }
        }

        return currentHash;
    }
    
    /// <summary>
    /// Вычисляет хеш первых <paramref name="length"/> байт потока по блочному алгоритму.
    /// </summary>
    public static BigInteger ComputeHashWithOffset(
        in FileStream fs, long offset, long length, in BigInteger p, in BigInteger q) 
    {
        fs.Seek(offset, SeekOrigin.Begin);
        BigInteger mod = p * q;
        BigInteger currentHash = InitialHash;

        byte[] buffer = new byte[DefaultBufferSize];
        long remaining = length;
        while (remaining > 0)
        {
            int toRead = (int)Math.Min(buffer.Length, remaining);
            int read = fs.Read(buffer, 0, toRead);
            if (read <= 0) break;

            for (int i = 0; i < read; i++)
            {
                BigInteger blockValue = buffer[i];
                currentHash = MathEngine.SumSquareMod(currentHash, blockValue, mod);
            }

            remaining -= read;
        }

        return currentHash;
    }
}