using System;

namespace SubmissionUtility
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using Chilkat;

    class Program
    {
        static void Main(string[] args)
        {
            var outputFile = @".\ATLS_Server_Build_1.0.15.1-380 System Files.csv";
            var inputPath = @"C:\Users\dwestbro\Downloads\Aristocrat\ATLS\Builds\1.0.15.1-380-deploy";
            var descXmlPath = @".\Descriptions.xml";

            var descriptions =
            (from desc in XDocument.Load(descXmlPath).Element("descriptions")?.Descendants("add") ??
                          throw new InvalidOperationException("Invalid assembly descriptions xml file")
                select new
                {
                    Pattern = desc.Attribute("pattern")?.Value ??
                              throw new InvalidOperationException("pattern attribute not found"),
                    desc.Value
                }).ToDictionary(x => x.Pattern, x => x.Value);

            var systemFiles = new List<(string Name, string Hash, string Description)>();

            foreach (var file in new DirectoryInfo(inputPath).GetFiles("manifest.txt", SearchOption.AllDirectories))
            {
                using (var csv = new Csv {HasColumnNames = false, Delimiter = ","})
                {
                    csv.LoadFile(file.FullName);

                    for (var i = 0; i < csv.NumRows; i++)
                    {
                        var name = csv.GetCell(i, 0);
                        var hash = csv.GetCell(i, 3);

                        if (Regex.IsMatch(name, "^(?:appsettings)(?:\\.\\w+)*\\.json$") ||
                            Regex.IsMatch(name, "^.*\\.config$"))
                        {
                            continue;
                        }

                        if (systemFiles.Any(x => x.Name == name && x.Hash == hash))
                        {
                            continue;
                        }

                        var desc = descriptions
                            .Where(x => Regex.IsMatch(name, $"^{x.Key}$"))
                            .Select(x => x.Value)
                            .FirstOrDefault() ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(desc))
                        {
                            Console.WriteLine($"Desciption not found for {name}");
                        }

                        systemFiles.Add((name, hash, desc));
                    }
                }
            }

            var builder = new System.Text.StringBuilder();
            builder.Append("System File,Description,Manifest SHA1\n");

            foreach (var entry in systemFiles)
            {
                builder.Append($"{entry.Name},{entry.Description},{entry.Hash}\n");
            }

            var controlFile = new Csv {HasColumnNames = true, Delimiter = "," };
            controlFile.LoadFromString(builder.ToString());

            if (File.Exists(outputFile))
            {
                try
                {
                    File.Delete(outputFile);
                }
                catch (IOException ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    return;
                }
            }

            controlFile.SaveFile(outputFile);

            Console.WriteLine("Press any key to exit");

            Console.ReadKey();
        }
    }
}
