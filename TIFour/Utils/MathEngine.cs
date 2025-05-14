using System.Numerics;
using System.Security.Cryptography;

namespace TIFour.Utils;

public class MathEngine
{

    public static bool IsPrime(in BigInteger number, int iterations = 5)
    {
        // NEGATIVE NUMBERS AND ONE IS NOT PRIME
        if (number <= 1) return false;
        // 2,3 IS PRIME
        if (number <= 3) return true;
        // IF NUMBER IS EVEN => IT IS NOT PRIME
        if (number.IsEven) return false;

        // WE NEED TO DECOMPOSE VALUES IN => n - 1 = d * (2 ^ s)
        (BigInteger d, int s) = Decompose(number - 1);
    
        // IT IS CRYPTOGRAPHY SAVE RANDOM GENERATOR
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        byte[] bytes = new byte[number.GetByteCount()];
    
        for (int i = 0; i < iterations; i++)
        {
            // FILL ARRAY OF BYTES WITH STRONG RANDOM BYTES
            rng.GetBytes(bytes);
            // CREATING A RANDOM NUMBER (+ 2 IS FOR BOUND [1,N - 2])
            BigInteger a = BigInteger.Abs(new BigInteger(bytes)) % (number - 3) + 2;
        
            // NOW CALCULATING (a ^ b mod n)
            BigInteger x = FastPow(a, d, number);
            
            // JUST RULE CONDITION, IF X IS THAT => SKIP
            if (x == 1 || x == number - 1) continue;

            bool isComposite = true;
            for (int j = 0; j < s - 1; j++)
            {
                // x = x^2 mod n
                x = FastPow(x, 2, number);
                if (x == number - 1)
                {
                    isComposite = false;
                    break;
                }
            }
            if (isComposite) return false;
        }
        return true;
    }

    
    private static (BigInteger d, int s) Decompose(in BigInteger numMinusOne)
    {
        BigInteger d = numMinusOne;
        int s = 0;
        // WHILE d IS EVEN
        while (d % 2 == 0)
        {
            // DIVISION BY TWO
            d /= 2;
            // INCREMENT POWER
            s += 1;
        }
        return (d, s);
    }
    
    public static BigInteger FastPow(BigInteger baseVal, BigInteger exponent, BigInteger mod)
    {
        // IF MOD IS ONE => ALWAYS ZERO
        if (mod == BigInteger.One) return 0;
        
        BigInteger result = BigInteger.One;
        
        // NORMALIZING baseVal (mod is number from IsPrime function) 
        // TO BOUND [0, mod-1]
        // FOR RULE a^k ≡ (a mod n)^k (mod n)
        baseVal %= mod;
    
        while (exponent > 0)
        {
            if (exponent % 2 == 1)
                result = (result * baseVal) % mod;
        
            // ((baseVal % mod) * (baseVal % mod)) % mod 
            // BUT NORMALIZATION WAS IN (baseVal % mod)
            baseVal = (baseVal * baseVal) % mod;
            exponent >>= 1;
        }
        return result < 0 ? result + mod : result;
    }
    
    /// <summary>
    /// Вычисляет (value + addend)² mod modulus.
    /// </summary>
    public static BigInteger SumSquareMod(in BigInteger value, in BigInteger addend, in BigInteger modulus)
    {
        // (value + addend) модуль n
        BigInteger s = (value + addend) % modulus;
        // квадрат по модулю n
        return FastPow(s, 2, modulus);
    }
    
    /// <summary>
    /// Находит gcd(a, b) через встроенный метод.
    /// </summary>
    public static BigInteger Gcd(BigInteger a, BigInteger b) =>
        BigInteger.GreatestCommonDivisor(a, b);

    /// <summary>
    /// Расширенный алгоритм Евклида: возвращает (g, x, y) для ax + by = g = gcd(a,b).
    /// </summary>
    private static (BigInteger g, BigInteger x, BigInteger y) ExtendedGcd(BigInteger a, BigInteger b)
    {
        if (b == 0)
            return (a < 0 ? -a : a, a.Sign < 0 ? -1 : 1, 0);

        var (g, x1, y1) = ExtendedGcd(b, a % b);
        return (g, y1, x1 - (a / b) * y1);
    }

    /// <summary>
    /// Вычисляет обратное к e по модулю mod: e * inv ≡ 1 (mod mod).
    /// </summary>
    public static BigInteger ModInverse(BigInteger e, BigInteger mod)
    {
        var (g, x, _) = ExtendedGcd(e, mod);
        if (g != 1)
            throw new ArithmeticException("e и модуль не взаимно простые, обратного не существует");
        // нормализуем x в [0, mod)
        BigInteger inv = x % mod;
        return inv < 0 ? inv + mod : inv;
    }

    /// <summary>
    /// Генерирует случайное BigInteger в диапазоне [min, max) с криптостойким RNG.
    /// </summary>
    public static BigInteger RandomInRange(BigInteger min, BigInteger max)
    {
        if (min >= max) throw new ArgumentException("min должен быть меньше max");
        BigInteger diff = max - min;
        int bytes = diff.GetByteCount();
        byte[] buf = new byte[bytes];
        using var rng = RandomNumberGenerator.Create();

        BigInteger val;
        do
        {
            rng.GetBytes(buf);
            val = new BigInteger(buf) & (BigInteger.One << (bytes*8 - 1)) - 1; 
            // признак положительности
        } while (val >= diff);

        return val + min;
    }
    
}