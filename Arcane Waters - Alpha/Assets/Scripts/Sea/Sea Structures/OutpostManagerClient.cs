using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class OutpostManagerClient : ClientMonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
   #region Public Variables

   // The build button
   public Button buildButton = null;

   // Tooltip we show when we can't build
   public ToolTipComponent buildTooltip = null;

   #endregion

   private void Update () {
      // Try to find where we could build an outpost
      bool canBuild = OutpostUtil.canBuildOutpostAnywhere(Global.player, out Vector3 buildPosition, out OutpostUtil.CantBuildReason reason, out bool foundPosition, out Direction direction);
      updateBuildTooltip(reason, canBuild);

      // If we didn't receive a valid reason why can't we build, lets assume we aren't in a map where we can ever build
      bool shouldBeActive = canBuild || reason != OutpostUtil.CantBuildReason.None;

      // Enable disable us when needed
      if (shouldBeActive != buildButton.gameObject.activeSelf) {
         buildButton.gameObject.SetActive(shouldBeActive);
      }

      _buildHovered &= shouldBeActive;

      if (_buildHovered) {
         buildTooltip.showTooltip();
      }

      buildButton.interactable = canBuild;

      // Enable/disable the toggle
      if (foundPosition && shouldBeActive && _buildHovered) {
         if (_outpostHighlight == null) {
            _outpostHighlight = Instantiate(PrefabsManager.self.outpostPrefab);
         }

         _outpostHighlight.transform.position = buildPosition;
         _outpostHighlight.setAsBuildHighlight(canBuild, direction);
         if (_outpostHighlight.TryGetComponent<ZSnap>(out ZSnap snap)) {
            snap.snapZ();
         }
      } else {
         if (_outpostHighlight != null) {
            Destroy(_outpostHighlight.gameObject);
         }
      }
   }

   public void buildButtonClick () {
      if (Global.player != null) {
         if (OutpostUtil.canBuildOutpostAnywhere(Global.player, out Vector3 buildPosition, out _, out _, out Direction direction)) {
            if (AreaManager.self.tryGetArea(Global.player.areaKey, out Area area)) {
               Global.player.rpc.Cmd_BuildOutpost(area.transform.InverseTransformPoint(buildPosition), direction);
            }
         }
      }
   }

   private void updateBuildTooltip (OutpostUtil.CantBuildReason cantBuildReason, bool canBuild) {
      if (canBuild) {
         buildTooltip.message = "Build a new outpost";
         return;
      }

      switch (cantBuildReason) {
         case OutpostUtil.CantBuildReason.Obstructed:
            buildTooltip.message = "Outpost is blocked";
            break;
         case OutpostUtil.CantBuildReason.NotEnoughResources:
            buildTooltip.message = "Not enough resources";
            break;
         case OutpostUtil.CantBuildReason.TooCloseToOutpost:
            buildTooltip.message = "There's already an outpost nearby";
            break;
         case OutpostUtil.CantBuildReason.NotShore:
            buildTooltip.message = "Can't find shore nearby";
            break;
         case OutpostUtil.CantBuildReason.NotPrimaryInstance:
            buildTooltip.message = "Can't build on this server";
            break;
         case OutpostUtil.CantBuildReason.IncorrectTilesBlocking:
            buildTooltip.message = "Uneven terrain";
            break;
         default:
            buildTooltip.message = "";
            break;
      }
   }

   public void OnPointerEnter (PointerEventData eventData) {
      _buildHovered = true;
   }

   public void OnPointerExit (PointerEventData eventData) {
      _buildHovered = false;
   }

   #region Private Variables

   // Is the build button hovered over
   private bool _buildHovered = false;

   // The highlight we show before placing an outpost
   private Outpost _outpostHighlight = null;

   #endregion
}
