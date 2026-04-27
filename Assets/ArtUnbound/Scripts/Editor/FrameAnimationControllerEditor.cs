using UnityEditor;
using UnityEngine;
using ArtUnbound.Feedback;

namespace ArtUnbound.Editor
{
    [CustomEditor(typeof(FrameAnimationController))]
    public class FrameAnimationControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Assign Frame Materials from Materials folder"))
            {
                AssignMaterials((FrameAnimationController)target);
            }
        }

        private static void AssignMaterials(FrameAnimationController controller)
        {
            var so = new SerializedObject(controller);
            var bronceProp = so.FindProperty("bronceMaterial");
            var plataProp = so.FindProperty("plataMaterial");
            var oroProp = so.FindProperty("oroMaterial");

            bronceProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/ArtUnbound/Materials/Frame_Bronce.mat");
            plataProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/ArtUnbound/Materials/Frame_Plata.mat");
            oroProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/ArtUnbound/Materials/Frame_Oro.mat");

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
        }
    }
}
