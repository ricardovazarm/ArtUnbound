using UnityEditor.Android;
using UnityEngine;

namespace ArtUnbound.Editor
{
    /// <summary>
    /// Adds required permissions for Meta Spatial Anchors to the Android Manifest.
    /// </summary>
    public class SpatialAnchorManifestModifier : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 1;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifestPath = path + "/src/main/AndroidManifest.xml";
            
            if (!System.IO.File.Exists(manifestPath))
            {
                Debug.LogWarning($"[SpatialAnchorManifest] AndroidManifest.xml not found at {manifestPath}");
                return;
            }

            string manifestContent = System.IO.File.ReadAllText(manifestPath);

            // Add Spatial Anchor permission if not already present
            if (!manifestContent.Contains("com.oculus.permission.USE_ANCHOR_API"))
            {
                manifestContent = manifestContent.Replace(
                    "</manifest>",
                    "    <uses-permission android:name=\"com.oculus.permission.USE_ANCHOR_API\" />\n</manifest>"
                );
                
                Debug.Log("[SpatialAnchorManifest] Added USE_ANCHOR_API permission to AndroidManifest.xml");
            }

            // Add Spatial Anchor feature requirement if not already present
            if (!manifestContent.Contains("com.oculus.feature.SPATIAL_ANCHOR_SUPPORT"))
            {
                manifestContent = manifestContent.Replace(
                    "</application>",
                    "        <meta-data android:name=\"com.oculus.feature.SPATIAL_ANCHOR_SUPPORT\" android:value=\"required\" />\n    </application>"
                );
                
                Debug.Log("[SpatialAnchorManifest] Added SPATIAL_ANCHOR_SUPPORT feature to AndroidManifest.xml");
            }

            System.IO.File.WriteAllText(manifestPath, manifestContent);
        }
    }
}
