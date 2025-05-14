using System.Numerics;

namespace TIFour.Utils;

public static class ValidationExceptions
{
    private static bool IsPrimeAndNumeric(in string textInput, out BigInteger result)
    {
        if (!BigInteger.TryParse(textInput, out result))
            return false;
        if (!MathEngine.IsPrime(result))
            return false;
        return true;
    }
    
    /// <summary>
    /// Валидирует ввод для q: проверяет, что это простое число.
    /// </summary>
    public static bool IsQValid(in string textInput, out BigInteger q)
    {
        return IsPrimeAndNumeric(textInput, out q);
    }
    
    /// <summary>
    /// Валидирует ввод для p: проверяет, что это простое число.
    /// </summary>
    public static bool IsPValid(in string textInput, out BigInteger p)
    {
        return IsPrimeAndNumeric(textInput, out p);
    }


    /// <summary>
    /// Валидирует открытый экспонент e: целое > 1.
    /// </summary>
    public static bool IsEValid(in string textInput, out BigInteger e)
    {
        return IsBigintegerNumber(textInput, out e);
    }

    public static bool IsSignatureValid(in string textInput, out BigInteger signature)
    {
        return IsBigintegerNumber(textInput, out signature);
    }


    private static bool IsBigintegerNumber(in string textInput, out BigInteger result)
    {
        if (!BigInteger.TryParse(textInput, out result))
            return false;
        return result > 1;
    }
}