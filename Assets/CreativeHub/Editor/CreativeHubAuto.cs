using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CreativeHub
{
    /// <summary>
    /// Background auto-capture: on scene save, and on an interval while the
    /// editor is in use. Everything is guarded so a failure never interrupts work.
    /// </summary>
    [InitializeOnLoad]
    public static class CreativeHubAuto
    {
        private static double _nextCapture;
        private static bool _savePending;

        static CreativeHubAuto()
        {
            EditorApplication.update += OnUpdate;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            _nextCapture = EditorApplication.timeSinceStartup + IntervalSeconds;
        }

        private static bool Enabled => EditorPrefs.GetBool(CreativeHubPrefs.AutoKey, false);

        private static double IntervalSeconds =>
            Mathf.Clamp(EditorPrefs.GetInt(CreativeHubPrefs.IntervalKey, 15), 1, 120) * 60.0;

        private static CaptureSource Source =>
            (CaptureSource)EditorPrefs.GetInt(CreativeHubPrefs.SourceKey, 1);

        private static void OnSceneSaved(Scene scene)
        {
            if (!Enabled) return;
            // Capture on the next editor tick rather than inside the save callback,
            // so we never render in the middle of Unity's own serialisation.
            _savePending = true;
        }

        private static void OnUpdate()
        {
            if (!Enabled)
            {
                _savePending = false;
                return;
            }

            // Compiling or entering play mode: leave the editor alone.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            var now = EditorApplication.timeSinceStartup;

            if (_savePending)
            {
                _savePending = false;
                Run("saved");
                _nextCapture = now + IntervalSeconds;
                return;
            }

            if (now >= _nextCapture)
            {
                _nextCapture = now + IntervalSeconds;
                Run("periodic");
            }
        }

        private static void Run(string note)
        {
            var error = CreativeHubCapture.Capture(Source, note);
            // A missing CreativeHub is normal, not worth a console warning every
            // interval; only genuine capture problems are logged.
            if (error != null && !error.StartsWith("CreativeHub isn't running"))
            {
                Debug.LogWarning("[CreativeHub] " + error);
            }
        }
    }
}
