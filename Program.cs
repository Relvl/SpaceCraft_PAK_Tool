using PakTool.Command;
using PakTool.Command.Impl;
using PalTools;

return CommandLine.Run(args);

namespace PalTools
{
    internal static class CommandLine
    {
        private static readonly IReadOnlyDictionary<string, IPakCommand> Commands = CreateCommandMap(
            new EmptyTreeCommand(),
            new ListCommand(),
            new PackCommand(),
            new UnpackCommand(),
            new ValidateCommand(),
            new VerifyCommand(),
            new VersionCommand()
        );

        public static int Run(string[] args)
        {
            try
            {
                if (args.Length == 0 || args[0] is "-h" or "--help")
                {
                    PrintUsage();
                    return 0;
                }

                if (args[0] == "help")
                {
                    PrintHelp(args[1..]);
                    return 0;
                }

                var commandName = args[0];
                if (!Commands.TryGetValue(commandName, out var command))
                    throw new UsageException($"Unknown command: {commandName}");

                if (args.Length > 1 && args[1] is ("-h" or "--help"))
                {
                    PrintCommandUsage(command);
                    return 0;
                }

                var parsedArgs = new CommandLineParser(command).Parse(args[1..]);
                return command.Execute(parsedArgs);
            }
            catch (UsageException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine();
                PrintUsage();
                return 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static IReadOnlyDictionary<string, IPakCommand> CreateCommandMap(params IPakCommand[] commands)
        {
            var result = new Dictionary<string, IPakCommand>(StringComparer.OrdinalIgnoreCase);
            foreach (var command in commands)
            {
                if (!result.TryAdd(command.Name, command))
                    throw new InvalidOperationException($"Duplicate command: {command.Name}");
            }

            return result;
        }

        private static void PrintUsage()
        {
            Console.Error.WriteLine($"Usage: {ToolName} <command> [args]");
            Console.Error.WriteLine($"       {ToolName} help <command>");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Commands:");

            var commandWidth = Commands.Values.Max(command => command.Name.Length);
            foreach (var command in Commands.Values.OrderBy(command => command.Name))
                Console.Error.WriteLine($"  {command.Name.PadRight(commandWidth)}  {command.Description}");
        }

        private static void PrintHelp(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            if (args.Length != 1)
                throw new UsageException("help expects a single command name.");

            if (!Commands.TryGetValue(args[0], out var command))
                throw new UsageException($"Unknown command: {args[0]}");

            PrintCommandUsage(command);
        }

        private static void PrintCommandUsage(IPakCommand command)
        {
            var arguments = string.Join(" ", command.Arguments.Select(argument => argument.Usage));
            Console.Error.WriteLine($"Usage: {ToolName} {command.Name}{(arguments.Length == 0 ? string.Empty : " " + arguments)}");
            Console.Error.WriteLine();
            Console.Error.WriteLine(command.Description);

            if (command.Arguments.Count == 0) return;

            Console.Error.WriteLine();
            Console.Error.WriteLine("Arguments:");

            var nameWidth = command.Arguments.Max(argument => FormatArgumentNames(argument).Length);
            foreach (var argument in command.Arguments)
                Console.Error.WriteLine($"  {FormatArgumentNames(argument).PadRight(nameWidth)}  {argument.Description}");
        }

        private static string FormatArgumentNames(PakArgument argument)
        {
            var names = argument.IsOption
                ? string.Join(", ", argument.Names.Select(name => argument.RequiresValue ? $"{name} {argument.Operand}" : name))
                : argument.Name;
            return argument.Optional ? $"[{names}]" : names;
        }

        private static string ToolName =>
            Path.GetFileName(Environment.ProcessPath) ?? "PakTool";
    }
}