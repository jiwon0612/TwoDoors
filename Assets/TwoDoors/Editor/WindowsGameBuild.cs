using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TwoDoors.Editor
{
    public static class WindowsGameBuild
    {
        public const string Output = @"C:\Users\USER\Desktop\BuildGame\TwoDoors\TwoDoor.exe";
        public const string Report = "Temp/TwoDoorWindowsBuild.json";
        [Serializable] class Result
        {
            public string status, output, errors;
            public ulong bytes;
            public double seconds;
        }
        public static void Queue()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Editor must be stopped and compiled before building.");
            File.WriteAllText(Report, JsonUtility.ToJson(new Result { status = "Building", output = Output }));
            EditorApplication.delayCall += Build;
        }
        static void Build()
        {
            try
            {
                string[] scenes = { "Assets/CJW/Scenes/Title.unity", "Assets/CJW/Scenes/Game.unity" };
                EditorBuildSettings.scenes = scenes.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
                Directory.CreateDirectory(Path.GetDirectoryName(Output));
                AssetDatabase.SaveAssets();
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes, locationPathName = Output,
                    target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
                });
                File.WriteAllText(Report, JsonUtility.ToJson(new Result
                {
                    status = report.summary.result.ToString(), output = Output,
                    bytes = report.summary.totalSize, seconds = report.summary.totalTime.TotalSeconds,
                    errors = string.Join("\n", report.steps.SelectMany(s => s.messages)
                        .Where(m => m.type == LogType.Error || m.type == LogType.Exception).Select(m => m.content))
                }, true));
            }
            catch (Exception e)
            {
                File.WriteAllText(Report, JsonUtility.ToJson(new Result { status = "Failed", output = Output, errors = e.ToString() }, true));
                Debug.LogException(e);
            }
        }
    }
}
