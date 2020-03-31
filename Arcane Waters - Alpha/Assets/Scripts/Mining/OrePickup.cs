using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

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

   public void initData (int ownerId, int voyageGroupId, int oreEffectId, OreNode oreNode, Sprite sprite) {
      this.ownerId = ownerId;
      this.voyageGroupId = voyageGroupId;
      this.oreEffectId = oreEffectId;
      this.oreNode = oreNode;
      spriteRender.sprite = sprite;
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      if (collision.GetComponent<PlayerBodyEntity>() != null && collision.GetComponent<PlayerBodyEntity>() == Global.player) {
         Global.player.rpc.Cmd_MineNode(oreNode.id, oreEffectId, ownerId, voyageGroupId);
      }
   }

   #region Private Variables

   #endregion
}
