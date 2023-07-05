using UnityEngine;
using UnityEditor;

namespace Evereal.VRVideoPlayer.Editor
{
    public class VRVideoPlayerAssetPostprocessor : AssetPostprocessor
    {
        const int LEFT_EYE_LAYER_NUM = 30;
        const int RIGHT_EYE_LAYER_NUM = 31;

        const string LEFT_EYE_LAYER = "Left Eye";
        const string RIGHT_EYE_LAYER = "Right Eye";

        public static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            // Add left, right eye later.
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            SerializedProperty layersProp = tagManager.FindProperty("layers");

            SerializedProperty leftEyeLayer =
                layersProp.GetArrayElementAtIndex(LEFT_EYE_LAYER_NUM);
            if (leftEyeLayer.stringValue.Equals(""))
            {
                leftEyeLayer.stringValue = LEFT_EYE_LAYER;
            }
            else if (!leftEyeLayer.stringValue.Equals(LEFT_EYE_LAYER))
            {
                Debug.LogError("User Layer " + LEFT_EYE_LAYER_NUM + " already " +
                               "taken, stereo video play may not work.");
            }

            SerializedProperty rightEyeLayer =
                layersProp.GetArrayElementAtIndex(RIGHT_EYE_LAYER_NUM);
            if (rightEyeLayer.stringValue.Equals(""))
            {
                rightEyeLayer.stringValue = RIGHT_EYE_LAYER;
            }
            else if (!rightEyeLayer.stringValue.Equals(RIGHT_EYE_LAYER))
            {
                Debug.LogError("User Layer " + RIGHT_EYE_LAYER_NUM + " already " +
                               "taken, stereo video play may not work.");
            }

            tagManager.ApplyModifiedProperties();
        }
    }
}