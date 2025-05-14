using System.Numerics;
using TIFour.Utils;

namespace TIFour.Services;

public static class RsaService
{
    
    /// <summary>
    /// По заданным простым p и q генерирует (e, d, n).
    /// </summary>
    public static (BigInteger e, BigInteger d, BigInteger n) GenerateKeyPair(in BigInteger p, in BigInteger q)
    {
        BigInteger n = p * q;
        BigInteger phi = (p - 1) * (q - 1);

        // 3. Выбираем e: 1<e<phi, gcd(e,phi)=1
        BigInteger e;
        do
        {
            e = MathEngine.RandomInRange(2, phi);
        } while (MathEngine.Gcd(e, phi) != BigInteger.One);

        // 4. Вычисляем d = e^{-1} mod phi
        BigInteger d = MathEngine.ModInverse(e, phi);

        return (e, d, n);
    }
    
    /// <summary>
    /// Генерирует ЭЦП для данного хеша: S = hash^d mod n.
    /// </summary>
    /// <param name="hash">Хеш сообщения (m).</param>
    /// <param name="d">Секретная экспонента.</param>
    /// <param name="modulus">Модуль n = p * q.</param>
    /// <exception cref="ArgumentOutOfRangeException">Если хэш не в диапазоне [0,n)</exception>
    /// <returns>Цифровая подпись S.</returns>
    public static BigInteger Sign(in BigInteger hash, in BigInteger d, in BigInteger modulus)
    {
        if (hash < 0 || hash >= modulus)
            throw new ArgumentOutOfRangeException(nameof(hash), "Hash must be in [0, n).");
        return MathEngine.FastPow(hash, d, modulus);
    }

    /// <summary>
    /// Проверяет цифровую подпись:
    /// 1) Восстанавливает хеш m_rec = S^e mod n.
    /// 2) Сравнивает с ожидаемым m_expected.
    /// </summary>
    /// <param name="signature">Подпись S.</param>
    /// <param name="e">Открытая экспонента.</param>
    /// <param name="modulus">Модуль n.</param>
    /// <param name="expectedHash">Ожидаемый хеш m.</param>
    /// <returns>
    /// Кортеж: (IsValid — true, если совпало; RecoveredHash — восстановленный из S хеш).
    /// </returns>
    public static (bool IsValid, BigInteger RecoveredHash) Verify(
        in BigInteger signature,
        in BigInteger e,
        in BigInteger modulus,
        in BigInteger expectedHash)
    {
        if (signature < 0 || signature >= modulus)
            throw new ArgumentOutOfRangeException(nameof(signature), "Signature must be in [0, n).");

        BigInteger recovered = MathEngine.FastPow(signature, e, modulus);
        bool ok = (recovered == expectedHash);
        return (ok, recovered);
    }
}