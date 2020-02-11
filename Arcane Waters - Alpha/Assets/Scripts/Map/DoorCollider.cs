using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DoorCollider : MonoBehaviour {
   #region Public Variables

   // The icon we use for showing that the door is locked
   public GameObject doorLockIcon;

   #endregion

   protected void Awake () {
      _boxCollider = GetComponent<BoxCollider2D>();
   }

   void OnTriggerEnter2D (Collider2D other) {
      NetEntity player = other.transform.GetComponent<NetEntity>();

      // If our player comes near the door, show some GUI
      if (player != null && player == Global.player) {
         doorLockIcon.SetActive(true);
      }
   }

   void OnTriggerExit2D (Collider2D other) {
      NetEntity player = other.transform.GetComponent<NetEntity>();

      // If our player leaves the door, remove the GUI
      if (player != null && player == Global.player) {
         doorLockIcon.SetActive(false);
      }
   }

   #region Private Variables

   // Our Box Collider
   protected BoxCollider2D _boxCollider;

   #endregion
}
