using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CreativeHub
{
    public enum CaptureSource
    {
        SceneView = 0,
        GameView = 1,
    }

    /// <summary>
    /// Renders the editor's view to a PNG and hands it to CreativeHub.
    ///
    /// The Scene view is rendered off-screen through its own camera, so the
    /// capture is clean geometry with no editor chrome and nothing in the user's
    /// scene is modified.
    /// </summary>
    public static class CreativeHubCapture
    {
        public const int Width = 1600;
        public const int Height = 900;

        /// <summary>
        /// Capture and send. Returns null on success, or a human-readable reason.
        /// </summary>
        public static string Capture(CaptureSource source, string note)
        {
            var incoming = CreativeHubBridge.RequestIncomingFolder();
            if (incoming == null)
            {
                return "CreativeHub isn't running, or no portfolio folder is set in its Settings.";
            }

            var stamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var imagePath = Path.Combine(incoming, "unity_" + stamp + ".png");

            if (source == CaptureSource.GameView)
            {
                // Screen Space - Overlay canvases (menus, HUD) never render into a
                // camera's targetTexture, so a manual render would silently drop all
                // UI. ScreenCapture grabs the real Game view instead - but it writes
                // the file a frame later, so the sidecar is written once it lands.
                try
                {
                    ScreenCapture.CaptureScreenshot(imagePath);
                }
                catch (Exception e)
                {
                    return "Capture failed: " + e.Message;
                }
                FinishWhenWritten(imagePath, stamp, source, note);
                return null;
            }

            byte[] png;
            try
            {
                png = RenderSceneView();
            }
            catch (Exception e)
            {
                return "Capture failed: " + e.Message;
            }
            if (png == null) return "No Scene view is open to capture.";

            try
            {
                File.WriteAllBytes(imagePath, png);
                File.WriteAllText(
                    Path.ChangeExtension(imagePath, ".json"),
                    BuildSidecar(stamp, source, note));
            }
            catch (Exception e)
            {
                return "Could not write the capture: " + e.Message;
            }

            CreativeHubBridge.NotifyIngest();
            return null;
        }

        /// <summary>
        /// ScreenCapture writes asynchronously. Poll for the file, then write the
        /// sidecar and notify CreativeHub. Gives up quietly after a few seconds.
        /// </summary>
        private static void FinishWhenWritten(string imagePath, long stamp, CaptureSource source, string note)
        {
            var deadline = EditorApplication.timeSinceStartup + 8;
            EditorApplication.CallbackFunction poll = null;
            poll = () =>
            {
                var done = false;
                try
                {
                    // A non-zero length means Unity has finished flushing it.
                    done = File.Exists(imagePath) && new FileInfo(imagePath).Length > 0;
                }
                catch (Exception) { /* file still locked; try again next tick */ }

                if (done)
                {
                    EditorApplication.update -= poll;
                    try
                    {
                        File.WriteAllText(
                            Path.ChangeExtension(imagePath, ".json"),
                            BuildSidecar(stamp, source, note));
                    }
                    catch (Exception) { /* sidecar is optional metadata */ }
                    CreativeHubBridge.NotifyIngest();
                }
                else if (EditorApplication.timeSinceStartup > deadline)
                {
                    EditorApplication.update -= poll;
                    Debug.LogWarning("[CreativeHub] Game view capture timed out. Is the Game view open and rendering?");
                }
            };
            EditorApplication.update += poll;
        }

        private static string BuildSidecar(long stamp, CaptureSource source, string note)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            var objects = scene.IsValid() ? scene.rootCount : 0;

            return "{"
                 + "\"ts\":" + stamp + ","
                 + "\"kind\":\"" + (source == CaptureSource.GameView ? "game" : "viewport") + "\","
                 + "\"source\":\"unity\","
                 + "\"note\":" + CreativeHubBridge.JsonEscape(string.IsNullOrEmpty(note) ? "capture" : note) + ","
                 + "\"stats\":{"
                 + "\"scene\":" + CreativeHubBridge.JsonEscape(scene.IsValid() ? scene.name : "")
                 + ",\"root_objects\":" + objects
                 + ",\"unity\":" + CreativeHubBridge.JsonEscape(Application.unityVersion)
                 + ",\"playing\":" + (EditorApplication.isPlaying ? "true" : "false")
                 + "}}";
        }

        private static byte[] RenderSceneView()
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null || view.camera == null) return null;
            return RenderCamera(view.camera);
        }

        /// <summary>
        /// Render a camera to an off-screen target. The camera's original target
        /// is restored even if rendering throws, so the editor is left untouched.
        /// </summary>
        private static byte[] RenderCamera(Camera camera)
        {
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 2;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            Texture2D texture = null;

            try
            {
                camera.targetTexture = rt;
                camera.Render();

                RenderTexture.active = rt;
                texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply();
                return texture.EncodeToPNG();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
