namespace MiliastraUtility.CLI;

internal partial class Program
{
    static void Main(string[] args)
    {
        PrintUsage();
    }

    static void PrintUsage()
    {
        string exeName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
        Console.WriteLine($"Usage:");
        Console.WriteLine($"  {exeName} --help");
        Console.WriteLine($"  {exeName} <command> [options]");
    }
}
