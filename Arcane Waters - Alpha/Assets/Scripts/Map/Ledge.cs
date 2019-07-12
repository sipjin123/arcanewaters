using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Ledge : MonoBehaviour {
   #region Public Variables

   // The direction associated with this ledge
   public Direction direction = Direction.South;

   #endregion

   private void Start () {
      // The server doesn't need to both with this
      if (Application.isBatchMode) {
         this.gameObject.SetActive(false);
      }
   }

   void OnTriggerEnter2D (Collider2D other) {
      BodyEntity player = other.transform.GetComponent<BodyEntity>();

      if (player == null) {
         return;
      }

      player.fallDirection = (int) this.direction;
   }

   void OnTriggerExit2D (Collider2D other) {
      BodyEntity player = other.transform.GetComponent<BodyEntity>();

      if (player == null) {
         return;
      }

      player.fallDirection = 0;
   }

   #region Private Variables

   #endregion
}
