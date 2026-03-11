using UnityEditor;
using UnityEngine;
using ArtUnbound.UI;

namespace ArtUnbound.Editor
{
    [CustomEditor(typeof(UnifiedMainMenuController))]
    public class UnifiedMainMenuControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Assign Frame Materials from Materials folder"))
            {
                AssignMaterials((UnifiedMainMenuController)target);
            }
        }

        private static void AssignMaterials(UnifiedMainMenuController controller)
        {
            var so = new SerializedObject(controller);
            var bronceProp = so.FindProperty("bronceFrameMaterial");
            var plataProp = so.FindProperty("plataFrameMaterial");
            var oroProp = so.FindProperty("oroFrameMaterial");
            var platinumProp = so.FindProperty("platinumFrameMaterial");

            bronceProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/ArtUnbound/Materials/Frame_Bronce.mat");
            plataProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/ArtUnbound/Materials/Frame_Plata.mat");
            oroProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/ArtUnbound/Materials/Frame_Oro.mat");
            platinumProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/ArtUnbound/Materials/Frame_Platinum.mat");

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
        }
    }
}
