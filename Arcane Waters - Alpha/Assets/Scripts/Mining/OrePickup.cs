using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class OrePickup : MonoBehaviour {
   #region Public Variables

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

   private void OnTriggerEnter2D (Collider2D collision) {
      if (collision.GetComponent<PlayerBodyEntity>() != null && collision.GetComponent<PlayerBodyEntity>() == Global.player) {
         if (voyageGroupId < 1) {
            triggerWarningMessage("Invalid Voyage Group!");
            return;
         }
         if (collision.GetComponent<PlayerBodyEntity>().voyageGroupId != voyageGroupId) {
            triggerWarningMessage("This does not belong to you!");
            return;
         }

         // TODO: Create sprite effects for ore collect
         EffectManager.self.create(Effect.Type.Crop_Harvest, transform.position);
         EffectManager.self.create(Effect.Type.Crop_Shine, transform.position);
         EffectManager.self.create(Effect.Type.Crop_Dirt_Large, transform.position);

         Global.player.rpc.Cmd_MineNode(oreNode.id, oreEffectId, ownerId, voyageGroupId);
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

   #endregion
}
