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

   protected override void Start () {
      base.Start();

      _hasTarget = hasTarget();
      updateSprites();
   }

   protected override void onActivated () {
      base.onActivated();
      
      // Spawn target here
      if (isServer) {   
         Instance instance = InstanceManager.self.getInstance(instanceId);

         _captureTarget = Instantiate(pvpCaptureTargetPrefab, targetHolderTransform.position, Quaternion.identity, targetHolderTransform).GetComponent<PvpCaptureTarget>();
         _captureTarget.areaKey = areaKey;
         _captureTarget.pvpTeam = pvpTeam;
         _captureTarget.faction = faction;
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

   protected override void Update () {
      base.Update();

      if (isServer && _captureTarget) {
         if (_hasTarget && !hasTarget()) {
            _hasTarget = false;
            updateSprites();
         } else if (!_hasTarget && hasTarget()) {
            _hasTarget = true;
            updateSprites();
         }
      } else if (isClient) {
         updateSprites();
      }
   }

   protected override void updateSprites () {
      mainRenderer.sprite = getSprite();
   }

   private bool hasTarget () {
      if (!_isActivated) {
         return true;
      }
      return (_captureTarget.getHoldingEntity() == this);
   }

   public override bool isPvpCaptureTargetHolder () {
      return true;
   }

   public bool tryCaptureTarget (PvpCaptureTarget target, PlayerShipEntity playerShip) {
      // Check if our own target is at base before allowing capture of the enemy target
      if (_captureTarget.getHoldingEntity() == this) {
         PvpGame activeGame = PvpManager.self.getGameWithPlayer(playerShip.userId);
         activeGame.addScoreForTeam(1, playerShip.pvpTeam);
         activeGame.sendGameMessage(playerShip.entityName + " captured the " + target.faction + "' treasure.", PvpAnnouncement.Priority.ScoreChange);
         playerShip.holdingPvpCaptureTarget = false;

         GameStatsManager.self.addFlagCaptureCount(playerShip.userId);

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

   protected override Sprite getSprite () {
      Sprite[] sprites = ImageManager.getSprites(TREASURE_ISLAND_SPRITES_PATH);
      int spriteIndex = (_hasTarget) ? 0 : 1;
      return sprites[spriteIndex];
   }

   #region Private Variables

   // A reference to the pvp capture target for this holder
   private PvpCaptureTarget _captureTarget;

   // Whether this target holder currently has its target
   [SyncVar]
   private bool _hasTarget = true;

   // The filepath for the sprites for the treasure island
   private const string TREASURE_ISLAND_SPRITES_PATH = "Sprites/Pvp/pvp_ctf_treasure_island";

   #endregion
}
