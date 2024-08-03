using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using static Mapping_Tools.MainWindow;

namespace Mapping_Tools {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
         // Log the exception, display it, etc
            var exception = e.Exception;
            var lines = new List<string> { exception.Message, exception.StackTrace, exception.Source };

            while (exception.InnerException != null) {
                exception = exception.InnerException;
                lines.Add("\n内部异常：");
                lines.Add(exception.Message);
                lines.Add(exception.StackTrace);
                lines.Add(exception.Source);
            }

            const string filename = "crash-log.txt";
            var path = AppDataPath != null ? Path.Combine(AppDataPath, filename) : filename;
            File.WriteAllLines(path, lines);
            MessageBox.Show($"程序遇到未处理异常。查阅 {filename} 获得更多信息：\n{path}", "错误");

            // Prevent default unhandled exception processing
            e.Handled = AppDataPath != null;
        }
    }
}
