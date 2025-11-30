using System.CommandLine;
using MiliastraUtility.CLI.Commands;

namespace MiliastraUtility.CLI;

internal static class Program
{
    static readonly RootCommand RootCommand = new ("Miliastra Utility CLI");

    static int Main(string[] args)
    {
        RootCommand.Subcommands.Add(ConvertCommand.Create());
        return RootCommand.Parse(args).Invoke();
    }
}
