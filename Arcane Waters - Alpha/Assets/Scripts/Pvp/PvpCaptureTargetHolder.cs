using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class PvpCaptureTargetHolder : SeaStructure, IMapEditorDataReceiver {
   #region Public Variables

   // A reference to the prefab used to spawn the PvpCaptureTarget
   public GameObject pvpCaptureTargetPrefab;

   // A reference to the transform determining where the target will spawn
   public Transform targetHolderTransform;

   // A reference to the transform that the target will be a child of, when dropped.
   public Transform targetHolderDroppedTransform;

   #endregion

   public void toggleActivation () {
      setIsActivated(!_isActivated);
   }

   protected override void onActivated () {
      base.onActivated();
      
      // Spawn target here
      if (isServer) {   
         Instance instance = InstanceManager.self.getInstance(instanceId);

         _captureTarget = Instantiate(pvpCaptureTargetPrefab, targetHolderTransform.position, Quaternion.identity, targetHolderTransform).GetComponent<PvpCaptureTarget>();
         _captureTarget.areaKey = areaKey;
         _captureTarget.pvpTeam = pvpTeam;
         _captureTarget.targetHolder = this;
         _captureTarget.assignHoldingEntity(this);

         InstanceManager.self.addSeaEntityToInstance(_captureTarget, instance);
         NetworkServer.Spawn(_captureTarget.gameObject);
      }
   }

   protected override void onDeactivated () {
      base.onDeactivated();

      // Despawn target here
      if (_captureTarget && _captureTarget.gameObject) {
         NetworkServer.Destroy(_captureTarget.gameObject);
         _captureTarget = null;
      }
   }

   public override bool isPvpCaptureTargetHolder () {
      return true;
   }

   public bool tryCaptureTarget (PvpCaptureTarget target, PlayerShipEntity playerShip) {
      // Check if our own target is at base before allowing capture of the enemy target
      if (_captureTarget.getHoldingEntity() == this) {
         PvpGame activeGame = PvpManager.self.getGameWithPlayer(playerShip.userId);
         activeGame.addScoreForTeam(1, playerShip.pvpTeam);
         activeGame.sendGameMessage(playerShip.entityName + " captured the " + PvpGame.getTeamName(target.pvpTeam) + "' flag.");
         playerShip.holdingPvpCaptureTarget = false;

         // Return the captured flag to its holder
         target.returnFlag();
         return true;
      }

      return false;
   }

   public void returnTarget (PvpCaptureTarget target) {
      target.assignHoldingEntity(this);
      
      target.transform.SetParent(targetHolderTransform);
      Util.setLocalXY(target.transform, Vector3.zero);
   }

   #region Private Variables

   // A reference to the pvp capture target for this holder
   private PvpCaptureTarget _captureTarget;

   #endregion
}
