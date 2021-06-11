using UnityEngine;
using MapCreationTool.Serialization;
using UnityEngine.UI;
using System;

namespace MapCreationTool {
   public class PvpTowerMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable {
      #region Public Variables

      // The pvp related variables being set in the map editor
      public Text pvpLaneText;
      public Text pvpLaneNumberText;
      public Text pvpTeamTypeText;

      #endregion

      private void Awake () {
         outline = GetComponentInChildren<SpriteOutline>();
      }

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(DataField.PVP_LANE) == 0) {
            try {
               string pvpLane = Enum.Parse(typeof(PvpLane), field.v).ToString();
               pvpLaneText.text = pvpLane;
            } catch {

            }
         }
         if (field.k.CompareTo(DataField.PVP_LANE_NUMBER) == 0) {
            try {
               string pvpLaneNumber = field.v.ToString();
               pvpLaneNumberText.text = pvpLaneNumber;
            } catch {

            }
         }
         if (field.k.CompareTo(DataField.PVP_TEAM_TYPE) == 0) {
            try {
               string pvpTeam = Enum.Parse(typeof(PvpTeamType), field.v).ToString();
               pvpTeamTypeText.text = pvpTeam;
            } catch {

            }
         }
      }

      public void setHighlight (bool hovered, bool selected, bool deleting) {
         setOutlineHighlight(outline, hovered, selected, deleting);
      }

      #region Private Variables

      // The outline
      private SpriteOutline outline;

      #endregion
   }
}