using UnityEditor;
using UnityEngine;

namespace CreativeHub
{
    /// <summary>Window > CreativeHub > Portfolio Capture</summary>
    public class CreativeHubWindow : EditorWindow
    {
        private string _note = "";
        private string _status = "";
        private bool _statusIsError;
        private double _statusUntil;

        [MenuItem("Window/CreativeHub/Portfolio Capture")]
        public static void Open()
        {
            var window = GetWindow<CreativeHubWindow>(false, "CreativeHub");
            window.minSize = new Vector2(300, 260);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Portfolio Capture", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Send progress captures to CreativeHub.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(8);

            // ── Project ──
            var project = EditorPrefs.GetString(CreativeHubPrefs.ProjectKey, "");
            var newProject = EditorGUILayout.TextField(
                new GUIContent("Project", "Leave blank to use the Unity project name"), project);
            if (newProject != project) EditorPrefs.SetString(CreativeHubPrefs.ProjectKey, newProject);
            EditorGUILayout.LabelField(" ", "→ " + CreativeHubBridge.ProjectName, EditorStyles.miniLabel);

            EditorGUILayout.Space(6);

            // ── What to capture ──
            var source = (CaptureSource)EditorPrefs.GetInt(CreativeHubPrefs.SourceKey, 1);
            var newSource = (CaptureSource)EditorGUILayout.EnumPopup(
                new GUIContent("Capture", "Game view captures exactly what plays, including UI and menus. Scene view captures the editor viewport (3D only - overlay UI will not appear)."),
                source);
            if (newSource != source) EditorPrefs.SetInt(CreativeHubPrefs.SourceKey, (int)newSource);

            EditorGUILayout.Space(8);

            // ── Manual capture with a description ──
            EditorGUILayout.LabelField("Description (optional)");
            _note = EditorGUILayout.TextArea(_note, GUILayout.Height(46));

            if (GUILayout.Button("Capture Progress Now", GUILayout.Height(30)))
            {
                var error = CreativeHubCapture.Capture(newSource, _note);
                if (error == null)
                {
                    SetStatus("Captured and sent to CreativeHub.", false);
                    _note = "";
                    GUI.FocusControl(null);
                }
                else
                {
                    SetStatus(error, true);
                }
            }

            EditorGUILayout.Space(10);

            // ── Auto capture ──
            var auto = EditorPrefs.GetBool(CreativeHubPrefs.AutoKey, false);
            var newAuto = EditorGUILayout.ToggleLeft(
                new GUIContent("Auto-capture progress", "Capture on save and on a timer"), auto);
            if (newAuto != auto) EditorPrefs.SetBool(CreativeHubPrefs.AutoKey, newAuto);

            using (new EditorGUI.DisabledScope(!newAuto))
            {
                var minutes = EditorPrefs.GetInt(CreativeHubPrefs.IntervalKey, 15);
                var newMinutes = EditorGUILayout.IntSlider("Every (min)", minutes, 1, 120);
                if (newMinutes != minutes) EditorPrefs.SetInt(CreativeHubPrefs.IntervalKey, newMinutes);
            }
            EditorGUILayout.LabelField(
                "Also captures when you save the scene.",
                EditorStyles.wordWrappedMiniLabel);

            // ── Status ──
            if (!string.IsNullOrEmpty(_status) && EditorApplication.timeSinceStartup < _statusUntil)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox(_status, _statusIsError ? MessageType.Warning : MessageType.Info);
            }
        }

        private void SetStatus(string message, bool isError)
        {
            _status = message;
            _statusIsError = isError;
            _statusUntil = EditorApplication.timeSinceStartup + 6;
        }

        // Keep the status message ticking down without the user moving the mouse.
        private void OnInspectorUpdate() => Repaint();
    }
}
