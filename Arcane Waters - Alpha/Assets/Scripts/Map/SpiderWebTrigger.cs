using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SpiderWebTrigger : MonoBehaviour {
   #region Public Variables

   // The web that this will use to move the player
   [HideInInspector]
   public SpiderWeb web;

   [HideInInspector]
   public Direction bounceDirection;

   #endregion

   public bool isFacingWeb (Direction directionPressed) {
      return (directionPressed == bounceDirection);
   }

   public void onPlayerJumped (PlayerBodyEntity player) {
      web.tryBouncePlayer(player);
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      PlayerBodyEntity player = collision.GetComponent<PlayerBodyEntity>();

      if (player) {
         player.collidingSpiderWebTrigger = this;
      }
   }

   private void OnTriggerExit2D (Collider2D collision) {
      PlayerBodyEntity player = collision.GetComponent<PlayerBodyEntity>();

      if (player) {
         player.collidingSpiderWebTrigger = null;
      }
   }

   #region Private Variables

   #endregion
}
