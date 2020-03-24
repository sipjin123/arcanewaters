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

   #endregion

   private void OnTriggerEnter2D (Collider2D collision) {
      if (collision.GetComponent<PlayerBodyEntity>() != null && collision.GetComponent<PlayerBodyEntity>() == Global.player) {
         oreNode.pickupPosition = transform.position;
         Global.player.rpc.Cmd_MineNode(oreNode.id);
         Destroy(gameObject);
      }
   }

   #region Private Variables

   #endregion
}
