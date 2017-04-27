using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Couchbase.Lite;
using Couchbase.Lite.Listener.Tcp;
using Couchbase.Lite.Util;
using Newtonsoft.Json;

namespace CouchbaseLite1
{
    class Program
    {
        private const string DbName = "app1";
        private const ushort DbPort = 59840;

        private static Manager manager;
        private static Database db;

        private static void Main()
        {
            SetupLogger();

            var directoryPath = $"D:\\{Process.GetCurrentProcess().ProcessName}";
            manager = new Manager(
                Directory.CreateDirectory(directoryPath),
                ManagerOptions.Default);

            db = manager.GetDatabase(DbName);

            var listener = new CouchbaseLiteTcpListener(manager, DbPort);
            listener.Start();

            Console.WriteLine("Press ESC to stop");

            var shutdownTokenSource = new CancellationTokenSource();
            HandleCommands(shutdownTokenSource);
        }

        private static string Tag => "MAIN (Main)";

        private static void SetupLogger()
        {
            log4net.Config.XmlConfigurator.Configure();
            Log.SetLogger(new Log4NetLogger());

            //Log.Level = Log.LogLevel.Debug;
            //Log.Domains.All.Level = Log.LogLevel.Debug;
            Log.Domains.ChangeTracker.Level = Log.LogLevel.Debug;
        }

        private static void HandleCommands(CancellationTokenSource shutdownTokenSource)
        {
            while (!shutdownTokenSource.IsCancellationRequested)
            {
                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.Spacebar:
                        PrintDocuments();
                        break;

                    case ConsoleKey.Enter:
                        CreateDocument();
                        break;

                    case ConsoleKey.NumPad0:
                        DeleteDocument();
                        break;

                    case ConsoleKey.Escape:
                        shutdownTokenSource.Cancel();
                        return;
                }
            }
        }

        private static void PrintDocuments()
        {
            var sb = new StringBuilder("Documents:" + Environment.NewLine);
            var allDocumentsQuery = db.CreateAllDocumentsQuery();
            var rows = allDocumentsQuery.Run();

            var count = 0;
            foreach (var row in rows)
            {
                sb.AppendLine($" {++count}) {JsonConvert.SerializeObject(row.Document.Properties)}");
            }

            Log.I(Tag, sb.ToString());
        }

        private static void CreateDocument()
        {
            var doc = db.CreateDocument();
            doc.PutProperties(
                new Dictionary<string, object>
                {
                    {"time", DateTime.Now.ToString("G")}
                });

            Log.I(Tag, $"Created {JsonConvert.SerializeObject(doc.Properties)}");
        }

        private static void DeleteDocument()
        {
            var allDocumentsQuery = db.CreateAllDocumentsQuery();
            var startKeyDocId = allDocumentsQuery.Run().FirstOrDefault()?.DocumentId;

            if (startKeyDocId == null)
                return;

            var document = db.GetDocument(startKeyDocId);
            Log.I(Tag, $"Deleted {JsonConvert.SerializeObject(document.Properties)}");
            document.Delete();
        }
    }
}
