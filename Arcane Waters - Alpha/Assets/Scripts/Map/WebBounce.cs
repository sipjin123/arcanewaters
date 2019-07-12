﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class WebBounce : MonoBehaviour {
   #region Public Variables

   // The prefab we use for the bouncing spider web effect
   public GameObject webBouncePrefab;

   #endregion

   void OnTriggerEnter2D (Collider2D other) {
      BodyEntity player = other.transform.GetComponent<BodyEntity>();

      if (player == null) {
         return;
      }

      Instantiate(webBouncePrefab, this.transform.position, Quaternion.identity);
   }

   #region Private Variables

   #endregion
}
