using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class OutpostManagerClient : ClientMonoBehaviour
{
   #region Public Variables

   // Singleton
   public static OutpostManagerClient self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   private void Update () {
      // If we are in build mode, try to find a reason why we might want to exit
      if (_buildModeActive) {
         if (InputManager.self.inputMaster.Sea.FireCannon.WasPerformedThisFrame()) {
            _buildModeActive = false;
         }

         if (KeyUtils.GetKey(UnityEngine.InputSystem.Key.Escape)) {
            _buildModeActive = false;
         }

         if (PanelManager.self.hasPanelInLinkedList()) {
            _buildModeActive = false;
         }
      }

      if (!_buildModeActive) {
         if (_outpostHighlight != null) {
            Destroy(_outpostHighlight.gameObject);
         }
         return;
      }

      // Try to find the direction in which we could build the outpost
      Vector3 desiredPosition = Camera.main.ScreenToWorldPoint(MouseUtils.mousePosition);

      // Try to find where we could build an outpost
      bool canBuild = OutpostUtil.canBuildOutpostAnywhere(Global.player, desiredPosition, out Vector3 buildPosition, out OutpostUtil.CantBuildReason reason, out bool foundPosition, out Direction direction);

      // If we didn't receive a valid reason why can't we build, lets assume we won't be able to for now
      bool shouldBeActive = canBuild || reason != OutpostUtil.CantBuildReason.None;
      if (!shouldBeActive) {
         _buildModeActive = false;
         return;
      }

      // Enable the highlight
      if (_outpostHighlight == null) {
         _outpostHighlight = Instantiate(PrefabsManager.self.outpostPrefab);
      }
      _outpostHighlight.setAsBuildHighlight(canBuild, direction);

      if (foundPosition) {
         _outpostHighlight.transform.position = buildPosition;
      } else {
         _outpostHighlight.transform.position = desiredPosition;
      }

      if (_outpostHighlight.TryGetComponent<ZSnap>(out ZSnap snap)) {
         snap.snapZ();
      }
   }

   public void onInteractWithWorldClick () {
      if (_buildModeActive) {
         _buildModeActive = false;
         Vector3 desiredPosition = Camera.main.ScreenToWorldPoint(MouseUtils.mousePosition);
         if (OutpostUtil.canBuildOutpostAnywhere(Global.player, desiredPosition, out Vector3 buildPosition, out _, out _, out Direction direction)) {
            if (AreaManager.self.tryGetArea(Global.player.areaKey, out Area area)) {
               Global.player.rpc.Cmd_BuildOutpost(area.transform.InverseTransformPoint(buildPosition), direction);
            }
         }
      } else {
         if (!Util.IsPointerOverUIObject(MouseUtils.mousePosition)) {
            if (AreaManager.self.tryGetArea(Global.player.areaKey, out Area area)) {
               if (area.hasTileAttribute(TileAttributes.Type.LandInSea, Camera.main.ScreenToWorldPoint(MouseUtils.mousePosition))) {
                  // Add a context menu for building an outpost
                  D.adminLog("ContextMenu: Interact was performed via action key sea, no object hovered", D.ADMIN_LOG_TYPE.Player_Menu);
                  PanelManager.self.contextMenuPanel.clearButtons();
                  PanelManager.self.contextMenuPanel.addButton("Build Outpost", OutpostManagerClient.self.buildButtonClick);
                  PanelManager.self.contextMenuPanel.show("World");
               }
            }
         }
      }
   }

   public void buildButtonClick () {
      if (OutpostUtil.canBuildOutposts(Global.player)) {
         _buildModeActive = true;
      }
   }

   #region Private Variables

   // Is the build button hovered over
   private bool _buildModeActive = false;

   // The highlight we show before placing an outpost
   private Outpost _outpostHighlight = null;

   #endregion
}
