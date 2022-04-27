using UnityEngine;
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

   public void initData (int ownerId, int voyageGroupId, int oreEffectId, OreNode oreNode, Sprite sprite) {
      this.ownerId = ownerId;
      this.voyageGroupId = voyageGroupId;
      this.oreEffectId = oreEffectId;
      this.oreNode = oreNode;
      spriteRender.sprite = sprite;
   }

   private void Start () {
      _creationTime = NetworkTime.time;
   }

   private void Update () {
      // Slowly move towards the player after a little while
      if (Global.player != null && NetworkTime.time - _creationTime > 1f) {
         float distance = Vector2.Distance(this.transform.position, Global.player.transform.position);

         // Only move towards player when player is within the distance
         if (distance < MAX_GRAVITATE_DISTANCE) {
            float speed = MAX_GRAVITATE_DISTANCE / distance  * 1.25f;
            Vector2 newPosition = Vector2.Lerp(transform.position, Global.player.transform.position, Time.deltaTime * speed);
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
         //SoundEffectManager.self.playSoundEffect(SoundEffectManager.ORE_PICKUP, transform);

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
