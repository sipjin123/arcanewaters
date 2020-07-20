using System;
using System.Linq;
using UnityEngine;

namespace MapCreationTool
{
   [Serializable]
   public class MapListPanelState
   {
      #region Public Variables

      // Key by which this class is saved in playerprefs
      public const string SAVE_KEY = "mapeditor_maplistpanel_state";

      // Which map groups are expanded
      public int[] expandedMapIds = new int[0];

      // Which user map groups are expanded
      public int[] expandedUserIds = new int[0];

      // How is the panel currently ordered
      public MapListPanel.OrderingType orderingType = MapListPanel.OrderingType.None;

      // Should only current user's maps be shown
      public bool showOnlyMyMaps = false;

      #endregion

      public static MapListPanelState self
      {
         get
         {
            if (_self == null) {
               _self = load();
            }

            return _self;
         }
      }

      private static MapListPanelState load () {
         if (PlayerPrefs.HasKey(SAVE_KEY)) {
            return JsonUtility.FromJson<MapListPanelState>(PlayerPrefs.GetString(SAVE_KEY));
         } else {
            return new MapListPanelState();
         }
      }

      public static void toggleExpandedMapId (int id) {
         if (self.expandedMapIds.Contains(id)) {
            self.expandedMapIds = self.expandedMapIds.Where(i => i != id).ToArray();
         } else {
            self.expandedMapIds = self.expandedMapIds.Append(id).ToArray();
         }
         self.save();
      }

      public static void toggleExpandedUserId (int id) {
         if (self.expandedUserIds.Contains(id)) {
            self.expandedUserIds = self.expandedUserIds.Where(i => i != id).ToArray();
         } else {
            self.expandedUserIds = self.expandedUserIds.Append(id).ToArray();
         }
         self.save();
      }

      public static void setOrderingType (MapListPanel.OrderingType orderingType) {
         self.orderingType = orderingType;
         self.save();
      }

      public static void setShowOnlyMyMaps (bool value) {
         self.showOnlyMyMaps = value;
         self.save();
      }

      public void save () {
         PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(this));
      }

      #region Private Variables

      // Singleton instance
      private static MapListPanelState _self;

      #endregion
   }
}