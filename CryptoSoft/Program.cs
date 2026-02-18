namespace CryptoSoft;

public static class Program
{
    private static Mutex? _mutex;
    public static void Main(string[] args)
    {
        const string mutexName = @"Global\CryptoSoft_ProSoft_V3";

        _mutex = new Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {

            Environment.Exit(-2);
            return;
        }

        try
        {
            if (args.Length < 2)
            {
                Environment.Exit(-1);
                return;
            }

            var fileManager = new FileManager(args[0], args[1]);
            int elapsedTime = fileManager.TransformFile();

            Environment.Exit(elapsedTime);
        }
        catch (Exception)
        {
            Environment.Exit(-99);
        }
        finally
        {
            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }
        }
    }
}