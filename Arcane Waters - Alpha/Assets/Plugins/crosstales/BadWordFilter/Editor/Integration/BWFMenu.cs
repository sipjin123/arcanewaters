using UnityEngine;
using UnityEditor;
using Crosstales.BWF.EditorUtil;

namespace Crosstales.BWF.EditorIntegration
{
    /// <summary>Editor component for the "Tools"-menu.</summary>
    public class BWFMenu
    {
        [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Prefabs/" + Util.Constants.MANAGER_SCENE_OBJECT_NAME, false, EditorHelper.MENU_ID + 20)]
        private static void AddBWF()
        {
            EditorHelper.InstantiatePrefab(Util.Constants.MANAGER_SCENE_OBJECT_NAME);
        }

        [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Prefabs/" + Util.Constants.MANAGER_SCENE_OBJECT_NAME, true)]
        private static bool AddBWFValidator()
        {
            return !EditorHelper.isBWFInScene;
        }

        [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Create/Source", false, EditorHelper.MENU_ID + 300)]
        public static void CreateSource()
        {
            Util.Helper.CreateSource();
        }

        [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Help/Manual", false, EditorHelper.MENU_ID + 600)]
        private static void ShowManual()
        {
            Application.OpenURL(Util.Constants.ASSET_MANUAL_URL);
        }

        [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Help/API", false, EditorHelper.MENU_ID + 610)]
        private static void ShowAPI()
        {
            Application.OpenURL(Util.Constants.ASSET_API_URL);
        }

        [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Help/Forum", false, EditorHelper.MENU_ID + 620)]
        private static void ShowForum()
        {
            Application.OpenURL(Util.Constants.ASSET_FORUM_URL);
        }

        [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Help/Product", false, EditorHelper.MENU_ID + 630)]
        private static void ShowProduct()
        {
            Application.OpenURL(Util.Constants.ASSET_WEB_URL);
        }

        [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Help/Videos/Promo", false, EditorHelper.MENU_ID + 650)]
        private static void ShowVideoPromo()
        {
            Application.OpenURL(Util.Constants.ASSET_VIDEO_PROMO);
        }

        [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Help/Videos/Tutorial", false, EditorHelper.MENU_ID + 660)]
        private static void ShowVideoTutorial()
        {
            Application.OpenURL(Util.Constants.ASSET_VIDEO_TUTORIAL);
        }

        [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Help/Videos/All Videos", false, EditorHelper.MENU_ID + 680)]
        private static void ShowAllVideos()
        {
            Application.OpenURL(Util.Constants.ASSET_SOCIAL_YOUTUBE);
        }

        [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Help/3rd Party Assets", false, EditorHelper.MENU_ID + 700)]
        private static void Show3rdPartyAV()
        {
            Application.OpenURL(Util.Constants.ASSET_3P_PLAYMAKER);
        }

        [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/About/Unity AssetStore", false, EditorHelper.MENU_ID + 800)]
        private static void ShowUAS()
        {
            Application.OpenURL(Util.Constants.ASSET_CT_URL);
        }

        [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/About/" + Util.Constants.ASSET_AUTHOR, false, EditorHelper.MENU_ID + 820)]
        private static void ShowCT()
        {
            Application.OpenURL(Util.Constants.ASSET_AUTHOR_URL);
        }

        [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/About/Info", false, EditorHelper.MENU_ID + 840)]
        private static void ShowInfo()
        {
            EditorUtility.DisplayDialog(Util.Constants.ASSET_NAME + " - About",
                "Version: " + Util.Constants.ASSET_VERSION +
               System.Environment.NewLine +
               System.Environment.NewLine +
                "© 2015-2019 by " + Util.Constants.ASSET_AUTHOR +
               System.Environment.NewLine +
               System.Environment.NewLine +
                Util.Constants.ASSET_AUTHOR_URL +
               System.Environment.NewLine, "Ok");
        }
    }
}
// © 2015-2019 crosstales LLC (https://www.crosstales.com)