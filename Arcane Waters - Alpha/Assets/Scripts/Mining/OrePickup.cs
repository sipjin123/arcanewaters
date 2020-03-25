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
         Global.player.rpc.Cmd_MineNode(oreNode.id);
      }
   }

   #region Private Variables

   #endregion
}
