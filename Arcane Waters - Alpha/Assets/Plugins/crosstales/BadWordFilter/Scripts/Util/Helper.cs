using UnityEngine;

namespace Crosstales.BWF.Util
{
    /// <summary>Various helper functions.</summary>
    public abstract class Helper : Common.Util.BaseHelper
    {

        #region Static properties

        /// <summary>Checks if the current platform is supported.</summary>
        /// <returns>True if the current platform is supported.</returns>
        public static bool isSupportedPlatform
        {
            get
            {
                //return isWindowsPlatform || isMacOSPlatform || isLinuxPlatform;
                return true;
            }
        }

        #endregion


        #region Static methods

        public static void CreateSource()
        {
#if UNITY_EDITOR
            string guid;
            int index = 0;

            do
            {
                guid = UnityEditor.AssetDatabase.AssetPathToGUID("Assets/New Source" + (index == 0 ? "" : " " + index) + ".asset");
                index++;
            } while (!string.IsNullOrEmpty(guid));

            index--;

            Data.Source asset = ScriptableObject.CreateInstance<Data.Source>();

            UnityEditor.AssetDatabase.CreateAsset(asset, "Assets/New Source" + (index == 0 ? "" : " " + index) + ".asset");
            UnityEditor.AssetDatabase.SaveAssets();

            UnityEditor.EditorUtility.FocusProjectWindow();

            UnityEditor.Selection.activeObject = asset;
#endif
        }

        #endregion
    }
}
// © 2015-2019 crosstales LLC (https://www.crosstales.com)