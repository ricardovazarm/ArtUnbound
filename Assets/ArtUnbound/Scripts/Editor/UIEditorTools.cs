using UnityEngine;
using UnityEditor;
using ArtUnbound.UI;
using ArtUnbound.Core;

namespace ArtUnbound.Editor
{
    public class UIEditorTools : EditorWindow
    {
        [MenuItem("Art Unbound/UI Tools")]
        public static void ShowWindow()
        {
            GetWindow<UIEditorTools>("UI Tools");
        }

        private void OnGUI()
        {
            GUILayout.Label("Panel Visibility Manager", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Hide All Panels"))
            {
                SetAllPanelsActive(false);
            }

            GUILayout.Space(10);
            GUILayout.Label("Show Only:", EditorStyles.label);

            if (GUILayout.Button("Main Menu")) ToggleSolo<MainMenuController>();
            if (GUILayout.Button("Onboarding")) ToggleSolo<OnboardingController>();
            if (GUILayout.Button("Gallery")) ToggleSolo<GalleryPanelController>();
            if (GUILayout.Button("Puzzle HUD")) ToggleSolo<PuzzleHUDController>();
            if (GUILayout.Button("Post Game")) ToggleSolo<PostGameController>();
            if (GUILayout.Button("Settings")) ToggleSolo<SettingsController>();
        }

        private void SetAllPanelsActive(bool active)
        {
            var root = FindAnyObjectByType<GameBootstrap>();
            if (root == null)
            {
                 Debug.LogWarning("No GameBootstrap found to identify panels.");
                 return;
            }

            // Heuristic: Find all controllers referenced in Bootstrap or in Canvas
            // A safer way for Editor tool: Find all MonoBehaviour types that implement common UI interfaces or just specific types
            
            ToggleValues(active, typeof(MainMenuController));
            ToggleValues(active, typeof(OnboardingController));
            ToggleValues(active, typeof(GalleryPanelController));
            ToggleValues(active, typeof(PuzzleHUDController));
            ToggleValues(active, typeof(PostGameController));
            ToggleValues(active, typeof(SettingsController));
            ToggleValues(active, typeof(ArtworkDetailController));
            ToggleValues(active, typeof(PieceCountSelectorController));
            ToggleValues(active, typeof(PauseMenuController));
        }

        private void ToggleSolo<T>() where T : MonoBehaviour
        {
            SetAllPanelsActive(false); // Hide others
            ToggleValues(true, typeof(T)); // Show target
        }

        private void ToggleValues(bool active, System.Type type)
        {
            var objects = FindObjectsByType(type, FindObjectsSortMode.None);
            foreach (var obj in objects)
            {
                var mono = obj as MonoBehaviour;
                if (mono != null)
                {
                    // If the script is on the panel root itself, toggle the GO. 
                    // Often UI controllers are on the root or manage a child 'Panel'.
                    // Let's assume the Controller IS the Panel root for simplicity, or we check its structure.
                    
                    Undo.RecordObject(mono.gameObject, $"Toggle {mono.name}");
                    mono.gameObject.SetActive(active);
                    
                    // Also check if there's a child "Panel" convention if the root is a Manager not the UI
                    // But usually disabling the Controller GO is enough to hide it.
                }
            }
        }
    }
}
