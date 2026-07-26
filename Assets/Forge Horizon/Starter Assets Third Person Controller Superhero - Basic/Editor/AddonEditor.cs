#if UNITY_EDITOR
using StarterAssets;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgeHorizon
{
    public class AddonEditor : EditorWindow
    {
        private GameObject playerGameObject; // Player Gameobject

        [MenuItem("Window/Forge Horizon/Starter Assets Third Person Controller Superhero - Basic/Editor")]
        public static void ShowWindow()
        {
            GetWindow<AddonEditor>("Starter Assets Third Person Controller Superhero - Basic");
        }

        private void OnGUI()
        {
            // Assign a player gameobject
            playerGameObject = (GameObject)EditorGUILayout.ObjectField("Player GameObject:", playerGameObject, typeof(GameObject), true);

            if (GUILayout.Button("Add Superhero Features"))
            {
                // Check if the player gameobject is attached
                if (playerGameObject == null)
                {
                    EditorUtility.DisplayDialog("Warning", "Please assign a player gameobject.", "OK");
                    return;
                }

                // Update the ThirdPersonController script and attach full add-on scripts
                AttachAnimatorControllerToAnimator();
                AddVariablesAndMethodsToThirdPersonController();
                AttachFlyingScript();

                EditorUtility.DisplayDialog("Success", "Superhero features have been added to the attached player gameobject.", "OK");

                // Confirm saving of changes made to the scene
                if (EditorUtility.DisplayDialog("Save Scene", "Do you want to save the changes made to the scene?", "Yes", "No"))
                {
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    Close(); // Close the editor widow if yes
                }
            }
        }

        private void AttachAnimatorControllerToAnimator()
        {
            // Check if the player gameobject is attached
            if (playerGameObject == null)
            {
                EditorUtility.DisplayDialog("Warning", "Please assign a player gameobject.", "OK");
                return;
            }

            // Check if the Animator component already exists
            Animator animator = playerGameObject.GetComponent<Animator>();
            if (animator == null)
            {
                EditorUtility.DisplayDialog("Error", "The Animator component could not be found on the player gameobject.", "OK");
                return;
            }

            // Define the path to the player's animator controller
            string animatorControllerPath = "Assets/Forge Horizon/Starter Assets Third Person Controller Superhero - Basic/Animations/Animator Controllers/Player.controller";

            // Load the player's animator controller from the asset path
            AnimatorController animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(animatorControllerPath);
            if (animatorController == null)
            {
                EditorUtility.DisplayDialog("Error", "The player animator controller could not be found at the specified path.", "OK");
                return;
            }

            // Attach the player's animator controller
            animator.runtimeAnimatorController = animatorController;
        }

        private void AddVariablesAndMethodsToThirdPersonController()
        {
            // Check if the player gameobject is attached
            if (playerGameObject == null)
            {
                EditorUtility.DisplayDialog("Warning", "Please assign a player gameobject.", "OK");
                return;
            }

            // Check if the ThirdPersonController script exists
            MonoBehaviour thirdPersonController = playerGameObject.GetComponent<ThirdPersonController>();
            if (thirdPersonController == null)
            {
                EditorUtility.DisplayDialog("Error", "The ThirdPersonController script could not be found on the player gameobject.", "OK");
                return;
            }

            // Define the path to the ThirdPersonController script
            string thirdPersonControllerScriptPath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(thirdPersonController));

            // Load the ThirdPersonController script from the asset path
            string scriptContent = File.ReadAllText(thirdPersonControllerScriptPath);

            // Add the ForgeHorizon namespace into the ThirdPersonController script
            string forgeHorizon = "using ForgeHorizon;";
            if (!scriptContent.Contains(forgeHorizon))
            {
                string targetUsing = "using UnityEngine.InputSystem;";
                int insertIndex = scriptContent.IndexOf(targetUsing);

                if (insertIndex != -1)
                {
                    insertIndex = scriptContent.IndexOf('\n', insertIndex) + 1;
                    scriptContent = scriptContent.Insert(insertIndex, forgeHorizon + "\n");
                }
                else
                {
                    scriptContent = forgeHorizon + "\n" + scriptContent;
                }
            }

            // Add flying variable declaration if none
            if (!scriptContent.Contains("private Flying flying;"))
            {
                int classIndex = scriptContent.IndexOf("public class ThirdPersonController");
                if (classIndex != -1)
                {
                    int insertIndex = scriptContent.IndexOf("{", classIndex) + 1;
                    scriptContent = scriptContent.Insert(insertIndex, "\n\t\tprivate Flying flying;\n");
                }
            }

            // Add flying variable initialization in the Start method
            if (!scriptContent.Contains("flying = GetComponent<Flying>();"))
            {
                int startIndex = scriptContent.IndexOf("void Start()");
                if (startIndex != -1)
                {
                    int startMethodBodyIndex = scriptContent.IndexOf("{", startIndex) + 1;
                    scriptContent = scriptContent.Insert(startMethodBodyIndex, "\n\t\t\tflying = GetComponent<Flying>();\n");
                }
            }

            // Add flying or rescuing check in the Update method
            if (!scriptContent.Contains("if (flying.isFloating) return;"))
            {
                int updateIndex = scriptContent.IndexOf("void Update()");
                if (updateIndex != -1)
                {
                    int updateMethodBodyIndex = scriptContent.IndexOf("{", updateIndex) + 1;
                    scriptContent = scriptContent.Insert(updateMethodBodyIndex, "\n\t\t\tif (flying.isFloating) return;\n");
                }
            }

            // Update script and refresh
            File.WriteAllText(thirdPersonControllerScriptPath, scriptContent);
            AssetDatabase.Refresh();
        }

        private void AttachFlyingScript()
        {
            // Check if the player gameobject is attached
            if (playerGameObject == null)
            {
                EditorUtility.DisplayDialog("Warning", "Please assign a player gameobject.", "OK");
                return;
            }

            // Check if the Flying script is already attached to the player gameobject or add it if not
            Flying flyingScript = playerGameObject.GetComponent<Flying>();
            if (flyingScript == null)
            {
                flyingScript = playerGameObject.AddComponent<Flying>();
            }
        }
    }
}
#endif
