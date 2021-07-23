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

   #endregion

   protected override void Update () {
      base.Update();

      // Temp for testing
      if (KeyUtils.GetKeyDown(UnityEngine.InputSystem.Key.P)) {
         setIsActivated(!_isActivated);
      }
   }

   protected override void onActivated () {
      base.onActivated();

      // Spawn target here?
      Instance instance = InstanceManager.self.getInstance(instanceId);

      _captureTarget = Instantiate(pvpCaptureTargetPrefab, targetHolderTransform.position, Quaternion.identity, transform).GetComponent<PvpCaptureTarget>();
      _captureTarget.pvpTeam = pvpTeam;
      _captureTarget.targetHolder = this;

      InstanceManager.self.addSeaEntityToInstance(_captureTarget, instance);
      NetworkServer.Spawn(_captureTarget.gameObject);
   }

   protected override void onDeactivated () {
      base.onDeactivated();

      // Despawn target here?
      NetworkServer.Destroy(_captureTarget.gameObject);
      _captureTarget = null;
   }

   public override bool isPvpCaptureTargetHolder () {
      return true;
   }

   public void tryCaptureTarget (PvpCaptureTarget target, PlayerShipEntity playerShip) {
      // Check if our own target is at base before allowing capture of the enemy target
      if (_captureTarget.holdingEntity == this) {
         D.log(playerShip.nameText.text + " has captured the " + PvpGame.getTeamName(target.pvpTeam) + " team's flag.");
         // TODO: Add logic here for incrementing score

         // Return the captured flag to its holder
         returnTarget();
      }
   }

   public void returnTarget () {
      _captureTarget.holdingEntity = this;
      _captureTarget.transform.SetParent(targetHolderTransform);
      Util.setLocalXY(_captureTarget.transform, Vector3.zero);
   }

   #region Private Variables

   // A reference to the pvp capture target for this holder
   private PvpCaptureTarget _captureTarget;

   #endregion
}
