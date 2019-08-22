using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Christopher Palacios

namespace ItemEditor.Layout.Style
{
   public static class ItemEditorStyle
   {
      private static GUISkin _style;

      private static readonly Dictionary<ItemEditorLayoutStyles, string> StylesMap =
          new Dictionary<ItemEditorLayoutStyles, string>();

      private const string BUTTON = "button";
      private const string LABEL = "label";
      private const string BOLD_LABEL = "bold-label";
      private const string BOX_SUB = "box-sub";
      private const string WHITE_BOX = "white-box";
      private const string LABEL_ALT = "label-alt";
      private const string LABEL_BLACK = "label-black";
      private const string NEGATIVE_BUTTON = "negative-button";
      private const string SEARCH_BUTTON = "search-button";
      private const string TRASH_BUTTON = "trash-button";
      private const string REFERENCE_BUTTON = "reference-button";
      private const string PREFAB_ICON = "prefab-icon";
      private const string SCENE_ICON = "scene-icon";
      private const string ASSET_ICON = "asset-icon";
      private const string SCRIPT_ICON = "script-icon";
      private const string IMAGE_ICON = "image-icon";
      private const string MATERIAL_ICON = "material-icon";
      private const string ANIMATOR_ICON = "animator-icon";
      private const string WARNING_ICON = "warning-icon";
      private const string H4 = "h4";
      private const string H3 = "h3";
      private const string H2 = "h2";
      private const string CENTERED_LABEL = "centered-label";
      private const string HOME_BUTTON = "home-button";
      private const string BACK_BUTTON = "back-button";

      static ItemEditorStyle () {
         StylesMap = new Dictionary<ItemEditorLayoutStyles, string> {
                {ItemEditorLayoutStyles.Button, BUTTON},
                {ItemEditorLayoutStyles.BoxSub, BOX_SUB},
                {ItemEditorLayoutStyles.WhiteBox, WHITE_BOX},
                {ItemEditorLayoutStyles.LabelAlt, LABEL_ALT},
                {ItemEditorLayoutStyles.LabelBlack, LABEL_BLACK},
                {ItemEditorLayoutStyles.NegativeButton, NEGATIVE_BUTTON},
                {ItemEditorLayoutStyles.Label, LABEL},
                {ItemEditorLayoutStyles.BoldLabel, BOLD_LABEL},
                {ItemEditorLayoutStyles.SearchButton, SEARCH_BUTTON},
                {ItemEditorLayoutStyles.TrashButton, TRASH_BUTTON},
                {ItemEditorLayoutStyles.PrefabIcon, PREFAB_ICON},
                {ItemEditorLayoutStyles.SceneIcon, SCENE_ICON},
                {ItemEditorLayoutStyles.MaterialIcon, MATERIAL_ICON},
                {ItemEditorLayoutStyles.AnimatorIcon, ANIMATOR_ICON},
                {ItemEditorLayoutStyles.ReferenceButton, REFERENCE_BUTTON},
                {ItemEditorLayoutStyles.AssetIcon, ASSET_ICON},
                {ItemEditorLayoutStyles.ScriptIcon, SCRIPT_ICON},
                {ItemEditorLayoutStyles.ImageIcon, IMAGE_ICON},
                {ItemEditorLayoutStyles.WarningIcon, WARNING_ICON},
                {ItemEditorLayoutStyles.H4, H4},
                {ItemEditorLayoutStyles.H2, H2},
                {ItemEditorLayoutStyles.H3, H3},
                {ItemEditorLayoutStyles.HomeButton, HOME_BUTTON},
                {ItemEditorLayoutStyles.BackButton, BACK_BUTTON},
                {ItemEditorLayoutStyles.CenteredLabel, CENTERED_LABEL }
            };
      }

      public static GUISkin skin
      {
         get
         {
            if (_style != null) return _style;
            string referenceExplorerStyleRelativePath = 
                ItemEditorUtilities.absolutePathToAssetsRelative(Application.dataPath + "/Scripts/Utilities/Editor/GUIStyle/AsterEditorSkin.guiskin");
            _style = AssetDatabase.LoadAssetAtPath<GUISkin>(referenceExplorerStyleRelativePath);
            Debug.Log(referenceExplorerStyleRelativePath);

            return _style;
         }
      }

      public static string getStyleFromEnum (ItemEditorLayoutStyles referenceExplorerStyle) {
         return StylesMap[referenceExplorerStyle];
      }

      public static GUIStyle getCustomStyle (ItemEditorLayoutStyles referenceExplorerStyle) {
         return skin.customStyles.First(guiStyle => guiStyle.name == getStyleFromEnum(referenceExplorerStyle));
      }
   }

   public enum ItemEditorLayoutStyles
   {
      Button = 0,
      Label = 1,
      BoldLabel = 2,
      BoxSub = 3,
      WhiteBox = 4,
      LabelAlt = 5,
      LabelBlack = 6,
      NegativeButton = 7,
      SearchButton = 8,
      TrashButton = 9,
      PrefabIcon = 10,
      SceneIcon = 11,
      MaterialIcon = 12,
      AnimatorIcon = 13,
      ReferenceButton = 14,
      AssetIcon = 15,
      ScriptIcon = 16,
      ImageIcon = 17,
      WarningIcon = 18,
      H4 = 19,
      H3 = 20,
      H2 = 21,
      HomeButton = 22,
      BackButton = 23,
      CenteredLabel = 24
   }
}
