using System.IO;
using System.Numerics;
using System.Windows;
using TIFour.Services;
using TIFour.Utils;

namespace TIFour;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    
    private BigInteger _p = BigInteger.Zero;
    private BigInteger _q = BigInteger.Zero;
    private BigInteger _d = BigInteger.Zero;
    private BigInteger _e = BigInteger.Zero;
    private BigInteger _currentHash = BigInteger.Zero;
    
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void btnSelectMessage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string path = FileService.OpenMessageFile();
            TxtMessagePath.Text = path;
        }
        catch (OperationCanceledException)
        {
            // Пользователь просто не выбрал файл
        }
        catch (FileNotFoundException)
        {
            MessageBox.Show("Выбранного файла не существует", "Ошибка выбора файла", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (IOException)
        {
            MessageBox.Show("Файл занят другим процессом", "Ошибка выбора файла", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnComputeHash_Click(object sender, RoutedEventArgs e)
    {
        string filepath = TxtMessagePath.Text.Trim();
        if (string.IsNullOrEmpty(filepath))
        {
            MessageBox.Show("Выберите шифруемый файл.");
            return;
        }

        if (FillPqValues())
        {
            (_e, _d, BigInteger n) = RsaService.GenerateKeyPair(_p, _q);

            DLabel.Content = "d: " + _d;
            ELabel.Content = "e: " + _e;

            try
            {
                _currentHash = HashService.ComputeHash(filepath, n);
                TxtHash.Text = _currentHash.ToString();
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("Выбранного файла не существует", "Ошибка выбора файла",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException)
            {
                MessageBox.Show("Файл занят другим процессом", "Ошибка выбора файла",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private bool FillPqValues()
    {
        string pInputText = TxtP.Text.Trim();
        if (!ValidationExceptions.IsPValid(pInputText, out BigInteger p))
        {
            MessageBox.Show("P должно быть простым числом!");
            return false;
        }
        
        string qInputText = TxtQ.Text.Trim();
        if (!ValidationExceptions.IsQValid(qInputText, out BigInteger q))
        {
            MessageBox.Show("Q должно быть простым числом!");
            return false;
        }
        
        _p = p;
        _q = q;
        return true;
    }
    
    private void btnSign_Click(object sender, RoutedEventArgs e)
    {
        if (_currentHash == BigInteger.Zero)
        {
            MessageBox.Show("Вычислите хэш сообщения перед подписью.");
            return;
        }
        
        if (_p == BigInteger.Zero)
        {
            MessageBox.Show("Введите значение P.");
            return;
        }

        if (_q == BigInteger.Zero)
        {
            MessageBox.Show("Введите значение Q.");
            return;
        }

        if (_d == BigInteger.Zero)
        {
            MessageBox.Show("Введите значение D.");
            return;
        }
        
        BigInteger n = _p * _q;
        BigInteger signature = RsaService.Sign(_currentHash, _d, n);
        TxtSignature.Text = signature.ToString();
    }

    private void btnSaveSignedMessage_Click(object sender, RoutedEventArgs e)
    {
        string original = TxtMessagePath.Text.Trim();
        string signatureStringInput = TxtSignature.Text.Trim();

        if (!ValidationExceptions.IsSignatureValid(signatureStringInput, out BigInteger signature))
        {
            MessageBox.Show("Подпись не является числом! Невозможно подписать файл.");
            return; 
        }
        
        try
        {
            FileService.SaveSignedMessage(original, signature);
            MessageBox.Show("Подписанное сообщение сохранено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            // Отмена сохранения
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void btnSelectSignedMessage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string path = FileService.OpenMessageFile();
            TxtSignedMessagePath.Text = path;
        }
        catch (OperationCanceledException)
        {
            // Пользователь просто не выбрал файл
        }
        catch (FileNotFoundException)
        {
            MessageBox.Show("Выбранного файла не существует", "Ошибка выбора файла", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (IOException)
        {
            MessageBox.Show("Файл занят другим процессом", "Ошибка выбора файла", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnVerifySignature_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(TxtSignedMessagePath.Text))
        {
            MessageBox.Show("Выберите подписанный файл.");
            return;
        }


        if (FillPqValues())
        {
            string eText = ETxt.Text.Trim();
            if (!ValidationExceptions.IsEValid(eText, out BigInteger eValue))
            {
                MessageBox.Show("Введите корректное целое e.");
                return;
            }

            BigInteger contentHash;
            BigInteger signature;
            
            try
            {
                (contentHash, signature) = FileService.LoadSignedMessage(TxtSignedMessagePath.Text, _p, _q);
            }
            catch (FormatException exception)
            {
                MessageBox.Show(exception.Message);
                return;
            }

            
            BigInteger modulus = _p * _q;
            bool isValid;
            BigInteger recoveredHash;
            
            try
            {
                (isValid, recoveredHash) = RsaService.Verify(signature, eValue, modulus, contentHash);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                MessageBox.Show(exception.Message);
                return;
            }

            TxtVerifyResult.Text = isValid
                ? $"Подпись верна (h = {recoveredHash})"
                : $"Подпись неверна (получено {recoveredHash}, ожидалось {contentHash})";
        }
    }
}