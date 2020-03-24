using System;
using System.Configuration;
using System.IO;
using DbUp;
using DbUp.ScriptProviders;

namespace DeployDbChanges
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["Database"].ConnectionString;

                // Get the current directory and make it a DirectoryInfo object.
                // Do not use Environment.CurrentDirectory, vistual studio 
                // and visual studio code will return different result:
                // Visual studio will return @"projectDir\bin\Release\netcoreapp2.0\", yet 
                // vs code will return @"projectDir\"
                var currentDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

                // On windows, the current directory is the compiled binary sits,
                // so string like @"bin\Release\netcoreapp2.0\" will follow the project directory. 
                // Hense, the project directory is the great grand-father of the current directory.
                string projectDirectory = currentDirectory.Parent.Parent.Parent.FullName;

                var scriptsPath = Path.Combine(projectDirectory, @"Scripts");

                string schemaScriptsPath = Path.Combine(scriptsPath, "Schema");

                if (Directory.GetFiles(schemaScriptsPath, "*.sql", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    Console.WriteLine("Start executing schema scripts...");

                    var schemaScriptsExecutor =
                    DeployChanges.To
                        .SqlDatabase(connectionString)
                        .WithScriptsFromFileSystem
                        (
                            schemaScriptsPath,
                            new FileSystemScriptOptions { IncludeSubDirectories = false }
                        )
                        .WithTransaction() // apply all changes in a single transaction
                        .LogToConsole();

                    var schemaUpgrader = schemaScriptsExecutor.Build();

                    if (schemaUpgrader.IsUpgradeRequired())
                    {
                        Console.WriteLine("Schema upgrade is required.");

                        var schemaUpgradeResult = schemaUpgrader.PerformUpgrade();

                        if (!schemaUpgradeResult.Successful)
                            throw new Exception("Failed!", schemaUpgradeResult.Error);

                        ShowSuccess();
                    }
                    else
                    {
                        Console.WriteLine("Schema upgrade is not required.");
                    }
                }

                string dataScriptsPath = Path.Combine(scriptsPath, "Data");

                if (Directory.GetFiles(dataScriptsPath, "*.sql", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    Console.WriteLine("Start executing data scripts...");

                    var dataScriptsExecutor =
                        DeployChanges.To
                            .SqlDatabase(connectionString)
                            .WithScriptsFromFileSystem
                            (
                                dataScriptsPath,
                                new FileSystemScriptOptions { IncludeSubDirectories = false }
                            )
                            .WithTransaction() // apply all changes in a single transaction
                            .LogToConsole();

                    var dataUpgrader = dataScriptsExecutor.Build();

                    if (dataUpgrader.IsUpgradeRequired())
                    {
                        Console.WriteLine("Data upgrade is required.");

                        var dataUpgradeResult = dataUpgrader.PerformUpgrade();

                        if (!dataUpgradeResult.Successful)
                            throw new Exception("Failed!", dataUpgradeResult.Error);

                        ShowSuccess();
                    }
                    else
                    {
                        Console.WriteLine("Data upgrade is not required.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static void ShowSuccess()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success!");
            Console.ResetColor();
        }
    }
}