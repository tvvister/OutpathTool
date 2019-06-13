using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace OutputPathTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var filePaths = args[0].Split('\n')
                .Select(path => Path.GetFullPath(path))
                .ToArray();

            var startUpPath = Path.GetFullPath(Environment.CurrentDirectory);

            var cancellationTokenSource = new CancellationTokenSource();
            _ = HandleFilesAsync(filePaths, startUpPath, cancellationTokenSource.Token);

            

            while (true)
            {
                var readKey = Console.ReadKey(intercept: true);
                if (readKey.Key == ConsoleKey.Escape)
                {
                    cancellationTokenSource.Cancel();
                    break;
                }
            }
        }

        private static async Task HandleFilesAsync(string[] filePaths, string startUpPath, CancellationToken token)
        {
            foreach (var pathInfo in filePaths.Select((path, index) => new { Path = path, Index = index }))
            {
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine($"Operation cancelled");
                    break;
                }
                Console.WriteLine($"Modifing file: {pathInfo.Path} Index: {pathInfo.Index}");
                try
                {
                    await HandleFileAsync(pathInfo.Path, startUpPath).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(exception);
                }
            }
        }

        private static Task HandleFileAsync(string path, string startUpPath)
        {
            return Task.Run(() =>
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(path);
                var outpathElements = xmlDocument.GetElementsByTagName("OutputPath")
                    .Cast<XmlNode>();
                foreach (var xmlNode in outpathElements)
                {
                    var folders = xmlNode.InnerText.Replace("..\\", "").Split('\\');
                    xmlNode.InnerText = $"{GetRelativePath(path, startUpPath)}OutputPath\\{String.Join("\\", folders)}";
                    xmlDocument.Save(path);
                }
            });         
        }

        private static string GetRelativePath(string path, string startUpPath)
        {
            var folderCount = path.Replace(startUpPath, "")
                .Trim()
                .Split('\\')
                .Where(x => !String.IsNullOrWhiteSpace(x))
                .Count();

            return String.Join("", Enumerable
                .Range(0, folderCount - 1)
                .Select(_ => "..\\"));
        }
    }
}
