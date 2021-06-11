using UnityEngine;
using MapCreationTool.Serialization;
using UnityEngine.UI;
using System;

namespace MapCreationTool {
   public class PvpBaseEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable {
      #region Public Variables

      // The pvp related variables being set in the map editor
      public Text pvpLaneText;
      public Text pvpLaneNumberText;
      public Text pvpTeamTypeText;

      // Scale of this object when displayed in the palette panel
      public const float PALETTE_SCALE = .4f;

      #endregion

      private void Awake () {
         outline = GetComponentInChildren<SpriteOutline>();
      }

      public void dataFieldChanged (DataField field) {
         // Adjust the scale of this building when spawned in the drawing board
         if (transform.parent.GetComponent<Palette>() != null) {
            transform.localScale = new Vector3(PALETTE_SCALE, PALETTE_SCALE, 1);
         } else {
            transform.localScale = new Vector3(1, 1, 1);
         }

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