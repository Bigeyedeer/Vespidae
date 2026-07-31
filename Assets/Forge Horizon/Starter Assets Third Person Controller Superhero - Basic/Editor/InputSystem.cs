#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgeHorizon
{
    public class InputSystem : EditorWindow 
    {
        [MenuItem("Window/Forge Horizon/Starter Assets Third Person Controller Superhero - Basic/Input System")]
        public static void ShowWindow()
        {
            GetWindow<InputSystem>("Starter Assets Third Person Controller Superhero - Basic");
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Add Superhero Inputs"))
            {
                // Confirm backup of the existing asset input file
                bool isBackedUp = EditorUtility.DisplayDialog("Confirm", "Do you want to proceed without backing up your existing asset input file?", "Yes", "No");

                if (!isBackedUp)
                {
                    EditorUtility.DisplayDialog("Backup", "Please back up your existing asset input file before proceeding.", "OK");
                    return;
                }

                // Modify StarterAssetsInputs by adding new input actions
                AddVariablesAndMethodsToStarterAssetsInputs();
                AddInputActionAsset();

                EditorUtility.DisplayDialog("Success", "Superhero Add-On inputs have been added.", "OK");
                Close();
            }
        }

        private void AddInputActionAsset()
        {
            // Define the path to the InputAction asset
            string inputActionAssetPath = "Assets/StarterAssets/InputSystem/StarterAssets.inputactions";

            // Ensure the path is valid
            if (string.IsNullOrEmpty(inputActionAssetPath))
            {
                EditorUtility.DisplayDialog("Error", "The InputAction asset file could not be found at the specified path.", "OK");
                return;
            }

            // Load the InputAction asset from the asset path
            InputActionAsset inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputActionAssetPath);

            // Find existing or create a new action map for the player if not found
            InputActionMap actionMap = inputActionAsset.FindActionMap("Player") ?? inputActionAsset.AddActionMap("Player");

            // Add or update input actions
            AddInputAction(actionMap, "Fly", InputActionType.Button, "<Keyboard>/f");
            AddInputAction(actionMap, "Ascend", InputActionType.Button, "<Keyboard>/q", "press(behavior=2)");
            AddInputAction(actionMap, "Descend", InputActionType.Button, "<Keyboard>/e", "press(behavior=2)");

            // Enable the action map
            actionMap.Enable();
        }

        private void AddInputAction(InputActionMap actionMap, string actionName, InputActionType type, string bindingPath, string interaction = null)
        {
            // Find existing or create a new input action if not found
            InputAction action = actionMap.FindAction(actionName) ?? actionMap.AddAction(actionName, type);

            // Check if the action already has the required binding or add it if none
            if (!action.bindings.Any(b => b.path == bindingPath))
            {
                var binding = action.AddBinding(bindingPath);
                if (!string.IsNullOrEmpty(interaction))
                {
                    binding.WithInteraction(interaction);
                }
            }
        }

        private void AddVariablesAndMethodsToStarterAssetsInputs()
        {
            string scriptPath = "Assets/StarterAssets/InputSystem/StarterAssetsInputs.cs";

            // Check if the StarterAssetsInputs script exists
            if (!File.Exists(scriptPath))
            {
                EditorUtility.DisplayDialog("Error", "The StarterAssetsInputs script could not be found.", "OK");
                return;
            }

            string scriptContent = File.ReadAllText(scriptPath);
            bool updated = false;

            // Locate class definition to insert new variables
            int classIndex = scriptContent.IndexOf("public class StarterAssetsInputs");
            if (classIndex != -1)
            {
                int insertIndex = scriptContent.IndexOf("{", classIndex) + 1;

                // Add missing variables
                updated |= InsertVariableIfMissing(ref scriptContent, insertIndex, "public bool fly;");
                updated |= InsertVariableIfMissing(ref scriptContent, insertIndex, "public bool ascend;");
                updated |= InsertVariableIfMissing(ref scriptContent, insertIndex, "public bool descend;");
            }

            // Methods to check and add
            string[] methodSignatures =
            {
            "public void OnFly(InputValue value)",
            "public void OnAscend(InputValue value)",
            "public void OnDescend(InputValue value)",
            "public void FlyInput(bool newFlyState)",
            "public void AscendInput(bool newAscendState)",
            "public void DescendInput(bool newDescendState)"
        };

            string[] methodBodies =
            {
            "public void OnFly(InputValue value)\n\t\t{\n\t\t\tFlyInput(value.isPressed);\n\t\t}",
            "public void OnAscend(InputValue value)\n\t\t{\n\t\t\tAscendInput(value.isPressed);\n\t\t}",
            "public void OnDescend(InputValue value)\n\t\t{\n\t\t\tDescendInput(value.isPressed);\n\t\t}",
            "public void FlyInput(bool newFlyState)\n\t\t{\n\t\t\tfly = newFlyState;\n\t\t}",
            "public void AscendInput(bool newAscendState)\n\t\t{\n\t\t\tascend = newAscendState;\n\t\t}",
            "public void DescendInput(bool newDescendState)\n\t\t{\n\t\t\tdescend = newDescendState;\n\t\t}"
        };

            foreach (var method in methodSignatures)
            {
                if (!scriptContent.Contains(method))
                {
                    // Find a proper location to insert methods
                    int insertIndex = scriptContent.LastIndexOf("public bool fly;") + "public bool fly;".Length;
                    scriptContent = scriptContent.Insert(insertIndex, "\n\n\t\t" + methodBodies[Array.IndexOf(methodSignatures, method)]);
                    updated = true;
                }
            }        

            // Update script only if changes were made
            if (updated)
            {
                File.WriteAllText(scriptPath, scriptContent);
                AssetDatabase.Refresh();
            }
        }

        private bool InsertVariableIfMissing(ref string scriptContent, int insertIndex, string variableDeclaration)
        {
            if (!scriptContent.Contains(variableDeclaration))
            {
                scriptContent = scriptContent.Insert(insertIndex, "\n\t\t" + variableDeclaration);
                return true;
            }
            return false;
        }
    }
}
#endif
