using System;
using System.Text;
using System.IO;

namespace AsyncDataAggregator__Backend_practice_1.Helpers
{
    public static class FileLogger
    {
        private static readonly string LogFilePath = "error.log"; // this is a field declaration,
        // private ==> only code inside this class can access this field.
        // static ==> this field belongs to the class itself and not any instance, there is only one copy.
        //readonly==> after the field is set , it cannot be changed.
        //all members of a static class must be static

        public static async Task LogErrorAsync(string message)// again, static means its called by the clsss itself, not on an instance
        {
            try
            {
                string LogLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ERROR: {message}";
                await File.AppendAllTextAsync(LogFilePath, LogLine + Environment.NewLine);// this is a file I/O operation , but i will ive a brief explanation, the rest can be found in the docs
                //File.AppendAllTextAsyc==> built in .NET method that opens a file, appens toext to ehte end, and closes the file, async does this without blocking the thread
                //Logfilepath==> file to write to ("error.log")
                // LogLine+EnvironmentNewLine==>text to append. Environment,NewLine is the linebreak chatacter for your OS, \r\n for windows. Without it all log entries would be oone massive line.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging failed : {ex.Message}");
            }

        }
    }

}