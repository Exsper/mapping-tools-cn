using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using Mapping_Tools.Classes.JsonConverters;

namespace Mapping_Tools.Classes.SystemTools {
    public enum ErrorType
    {
        Success,
        Error,
        Warning
    }

    public static class ProjectManager {
        private static readonly JsonSerializer serializer = new() {
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Objects,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore, 
            Converters = { new Vector2Converter()}
        };

        public static void WriteJson(StreamWriter streamWriter, object obj) {
            using (JsonTextWriter reader = new JsonTextWriter(streamWriter)) {
                serializer.Serialize(reader, obj);
            }
        }

        public static void SaveJson(string path, object obj) {
            using (StreamWriter fs = new StreamWriter(path)) {
                WriteJson(fs, obj);
            }
        }
        
        public static T LoadJson<T>(string path) {
            using (StreamReader fs = new StreamReader(path)) {
                using (JsonReader reader = new JsonTextReader(fs)) {
                    return serializer.Deserialize<T>(reader);
                }
            }
        }

        public static T LoadJson<T>(Stream stream) {
            using (StreamReader fs = new StreamReader(stream)) {
                using (JsonReader reader = new JsonTextReader(fs)) {
                    return serializer.Deserialize<T>(reader);
                }
            }
        }

        public static void AutoSaveProject<T>(ISavable<T> view) {
            string path = view.AutoSavePath;
            SaveProject(view, path);

            if (view is IHasExtraAutoSaveTarget hasExtraAutoSaveTarget) {
                SaveProject(view, hasExtraAutoSaveTarget.ExtraAutoSavePath);
            }
        }

        public static void SaveProjectDialog<T>(ISavable<T> view) {
            Directory.CreateDirectory(view.DefaultSaveFolder);
            string path = IOHelper.SaveProjectDialog(view.DefaultSaveFolder);
            SaveProject(view, path);
        }

        public static void SaveProject<T>(ISavable<T> view, string path) {
            // If the file name is not an empty string open it for saving.
            if (string.IsNullOrEmpty(path)) return;
            try {
                SaveJson(path, view.GetSaveData());
            } catch (Exception ex) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                MessageBox.Show("无法保存项目！");
                ex.Show();
            }
        }

        public static void LoadProject<T>(ISavable<T> view, bool dialog=false, bool message=true) {
            if (dialog)
                Directory.CreateDirectory(view.DefaultSaveFolder);
            string path = dialog ? IOHelper.LoadProjectDialog(view.DefaultSaveFolder) : view.AutoSavePath;

            // If the file name is not an empty string open it for saving.  
            if (path == "") return;
            try {
                T project = LoadJson<T>(path);

                if (project == null) {
                    throw new Exception("导入项目为空。");
                }

                view.SetSaveData(project);
            } catch (Exception ex) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                if (message) {
                    MessageBox.Show("无法加载项目！");
                    ex.Show();
                }
            }
        }

        public static void NewProject<T>(ISavable<T> view, bool dialog = false, bool message = true) {
            if (dialog) {
                var messageBoxResult = MessageBox.Show("您确定要创建一个新项目吗？所有未保存的进度都将丢失。", "新建项目确认", MessageBoxButton.YesNo);
                if (messageBoxResult != MessageBoxResult.Yes) return;
            }

            try {
                T project = Activator.CreateInstance<T>();
                view.SetSaveData(project);
            } catch (Exception ex) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                if (message) {
                    MessageBox.Show("无法初始化新项目！");
                    ex.Show();
                }
            }
        }

        /// <summary>
        /// Gets the project file for a savable tool with optional dialog.
        /// Uses default save path if no dialog is used.
        /// </summary>
        /// <typeparam name="T">The type of the project data</typeparam>
        /// <param name="view">The tool to get the project from</param>
        /// <param name="dialog">Whether to use a dialog</param>
        /// <returns></returns>
        public static T GetProject<T>(ISavable<T> view, bool dialog=false) {
            if (dialog)
                Directory.CreateDirectory(view.DefaultSaveFolder);
            string path = dialog ? IOHelper.LoadProjectDialog(view.DefaultSaveFolder) : view.AutoSavePath;

            return LoadJson<T>(path);
        }

        public static void SaveToolFile<T, T2>(ISavable<T> view, T2 obj, bool dialog = false) {
            if (dialog)
                Directory.CreateDirectory(view.DefaultSaveFolder);
            string path = dialog ? IOHelper.SaveProjectDialog(view.DefaultSaveFolder) : view.AutoSavePath;

            SaveJson(path, obj);
        }

        public static T2 LoadToolFile<T, T2>(ISavable<T> view, bool dialog = false) {
            if (dialog)
                Directory.CreateDirectory(view.DefaultSaveFolder);
            string path = dialog ? IOHelper.LoadProjectDialog(view.DefaultSaveFolder) : view.AutoSavePath;

            return LoadJson<T2>(path);
        }

        public static bool IsSavable(object obj) {
            return IsSavable(obj.GetType());
        }

        public static bool IsSavable(Type type) {
            return type.GetInterfaces().Any(x =>
                x.IsGenericType &&
                x.GetGenericTypeDefinition() == typeof(ISavable<>));
        }
    }
}
