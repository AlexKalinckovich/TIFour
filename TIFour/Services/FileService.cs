using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Unicode;
using Microsoft.Win32;
using TIFour.Utils;

namespace TIFour.Services;

public static class FileService
{
    private const string SignatureDelimiter = "--SIGNATURE--";
    private const int TailSearchSize        = 8192;
    private const int ReadBufferSize        = 4096;
    private const int IoBufferSize          = 8192;
    
    /// <summary>
    /// Открывает диалог выбора файла для подписания и выполняет валидацию.
    /// </summary>
    /// <exception cref="FileNotFoundException">Выбранный файл не существует</exception>
    /// <exception cref="OperationCanceledException">Пользователь не выбрал файл</exception>
    /// <exception cref="IOException"> файл занят другим процессом</exception>
    public static string OpenMessageFile()
    {
        FileDialog fileDialog = new OpenFileDialog();
        bool? result = fileDialog.ShowDialog();
        if (result != true)
        {
            throw new OperationCanceledException();
        }
        
        string path = fileDialog.FileName;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            throw new FileNotFoundException();
        }
        
        using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
        return path;
    }

     /// <summary>
    /// Сохраняет подписанное сообщение, сохраняя исходную кодировку (BOM) и добавляя разделитель+подпись.
    /// </summary>
    public static void SaveSignedMessage(in string originalPath, in BigInteger signature)
    {
        FileDialog dlg = new SaveFileDialog
        {
            Title = "Сохранить подписанное сообщение",
            FileName = Path.GetFileNameWithoutExtension(originalPath) + ".txt",
            OverwritePrompt = true
        };
        
        if (dlg.ShowDialog() != true)
            throw new OperationCanceledException();
        string savePath = dlg.FileName;

        using FileStream fsIn = new FileStream(originalPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        byte[] bomBuffer = new byte[3];
        int bytesRead = fsIn.Read(bomBuffer, 0, 3);
        bool hasUtf8Bom = (bytesRead >= 3) && (bomBuffer[0] == 0xEF && bomBuffer[1] == 0xBB && bomBuffer[2] == 0xBF);
            
        fsIn.Position = 0;

        using StreamReader reader = new StreamReader(fsIn, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: IoBufferSize, leaveOpen: true);
        
        reader.Peek(); // Для определенияя кодировки через reader. CurrentEncoding
        Encoding enc = reader.CurrentEncoding;

        if (enc is UTF8Encoding)
        {
            enc = new UTF8Encoding(hasUtf8Bom);
        }

        using FileStream fsOut = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using StreamWriter writer = new StreamWriter(fsOut, enc, bufferSize: IoBufferSize, leaveOpen: false);
        
        char[] buf = new char[IoBufferSize];
        int read;
        while ((read = reader.Read(buf, 0, buf.Length)) > 0)
            writer.Write(buf, 0, read);

        writer.Write(SignatureDelimiter);
        writer.Write(signature);
    }

    /// <summary>
    /// Загружает подписанное сообщение: определяет кодировку, ищет делимитер, извлекает подпись и хеширует тело.
    /// <exception cref="FormatException">Разделитель не найден</exception>
    /// </summary>
    public static (BigInteger ContentHash, BigInteger Signature) LoadSignedMessage(
        in string signedFilePath, in BigInteger p, in BigInteger q)
    {
        using FileStream fs = new FileStream(signedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        using StreamReader bomReader = new StreamReader(
            fs, 
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true, 
            IoBufferSize,
            leaveOpen: true);
        
        bomReader.Peek();
        Encoding enc = bomReader.CurrentEncoding;
        int preambleLen = enc.GetPreamble().Length;

        byte[] sepBytes = enc.GetBytes(SignatureDelimiter);

        long sepPos = FindSeparatorOffset(fs, sepBytes, preambleLen);

        BigInteger signature = ReadSignature(fs, sepPos + sepBytes.Length, enc);
        
        BigInteger hash = HashService.ComputeHashWithOffset(fs, 0, sepPos, p, q);

        return (hash, signature);
    }

    private static long FindSeparatorOffset(in FileStream fs, in byte[] separator, int startOffset)
    {
        long fileLen = fs.Length;
        int tailSize = (int)Math.Min(TailSearchSize, fileLen - startOffset);
        fs.Seek(fileLen - tailSize, SeekOrigin.Begin);

        byte[] tail = new byte[tailSize];
        fs.ReadExactly(tail, 0, tailSize);

        int idx = IndexOfSequence(tail, separator);
        if (idx < 0)
            throw new FormatException($"Разделитель '{SignatureDelimiter}' не найден.");

        return (fileLen - tailSize) + idx;
    }

    private static BigInteger ReadSignature(in FileStream fs, long signatureStart, in Encoding enc)
    {
        fs.Seek(signatureStart, SeekOrigin.Begin);
        using StreamReader reader = new StreamReader(
            fs, 
            enc, 
            detectEncodingFromByteOrderMarks:false,
            ReadBufferSize, 
            leaveOpen: true);
        
        string sigText = reader.ReadToEnd().Trim();
        if (!BigInteger.TryParse(sigText, out BigInteger sig))
            throw new FormatException("Неверный формат подписи.");
        return sig;
    }

    private static int IndexOfSequence(in byte[] buffer, in byte[] pattern)
    {
        int limit = buffer.Length - pattern.Length;
        for (int i = 0; i <= limit; i++)
        {
            bool ok = true;
            for (int j = 0; j < pattern.Length; j++)
                if (buffer[i + j] != pattern[j])
                { ok = false; break; }
            if (ok) return i;
        }
        return -1;
    }
}
    