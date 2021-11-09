﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class OrePickup : MonoBehaviour {
   #region Public Variables

   // The max distance at which this pickup will gravitate towards the player
   public const float MAX_GRAVITATE_DISTANCE = 1f;

   // The sprite renderer reference
   public SpriteRenderer spriteRender;

   // The ore node reference
   public OreNode oreNode;

   // The id of the mine effect
   public int oreEffectId;

   // The user who interacted this ore
   public int ownerId;

   // The voyage group that owns this ore
   public int voyageGroupId;

   #endregion

   public void initData (int ownerId, int voyageGroupId, int oreEffectId, OreNode oreNode, Sprite sprite, Quaternion spawnRotation) {
      this.ownerId = ownerId;
      this.voyageGroupId = voyageGroupId;
      this.oreEffectId = oreEffectId;
      this.oreNode = oreNode;
      spriteRender.sprite = sprite;
      spriteRender.transform.rotation = spawnRotation;
   }

   private void Start () {
      _creationTime = NetworkTime.time;
   }

   private void Update () {
      // Slowly move towards the player after a little while
      if (Global.player != null && NetworkTime.time - _creationTime > 1.5f) {
         float distance = Vector2.Distance(this.transform.position, Global.player.transform.position);

         // Make sure we're not too far away
         if (distance < MAX_GRAVITATE_DISTANCE) {
            Vector2 direction = Global.player.transform.position - this.transform.position;
            float speed = MAX_GRAVITATE_DISTANCE - distance;
            Vector3 offset = (Time.deltaTime * speed * direction.normalized);
            Vector2 newPosition = this.transform.position + offset;
            Util.setXY(this.transform, newPosition);
         }
      }
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      if (collision.GetComponent<PlayerBodyEntity>() != null && collision.GetComponent<PlayerBodyEntity>() == Global.player) {
         if (voyageGroupId < 1 && oreNode.mapSpecialType == Area.SpecialType.TreasureSite) {
            triggerWarningMessage("Invalid Voyage Group!");
            return;
         }
         if (collision.GetComponent<PlayerBodyEntity>().voyageGroupId != voyageGroupId && oreNode.mapSpecialType == Area.SpecialType.TreasureSite) {
            triggerWarningMessage("This does not belong to you!");
            return;
         }

         // TODO: Create sprite effects for ore collect
         EffectManager.self.create(Effect.Type.Crop_Harvest, transform.position);
         EffectManager.self.create(Effect.Type.Crop_Shine, transform.position);
         EffectManager.self.create(Effect.Type.Crop_Dirt_Large, transform.position);

         // Sound effect triggered for collecting the ores
         SoundEffectManager.self.playSoundEffect(SoundEffectManager.ORE_PICKUP, transform);

         Global.player.rpc.Cmd_PickupOre(oreNode.id, oreEffectId, ownerId, voyageGroupId);
         GetComponent<BoxCollider2D>().enabled = false;
         gameObject.SetActive(false);
      }
   }

   private void triggerWarningMessage (string msg) {
      Vector3 pos = this.transform.position + new Vector3(0f, .32f);
      GameObject messageCanvas = Instantiate(PrefabsManager.self.warningTextPrefab);
      messageCanvas.transform.position = pos;
      messageCanvas.GetComponentInChildren<TextMeshProUGUI>().text = msg;
   }

   #region Private Variables

   // The time at which we were created
   private double _creationTime;

   #endregion
}
