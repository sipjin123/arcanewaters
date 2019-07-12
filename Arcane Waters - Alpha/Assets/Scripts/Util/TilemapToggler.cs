using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;

public class TilemapToggler : MonoBehaviour {
   #region Public Variables

   #endregion

   void Start () {
      StartCoroutine("CO_toggleTilemap");
   }

   protected IEnumerator CO_toggleTilemap () {
      TilemapCollider2D collider = GetComponent<TilemapCollider2D>();

      if (collider != null) {
         collider.gameObject.SetActive(false);
      }

      foreach (TilemapCollider2D childCollider in GetComponentsInChildren<TilemapCollider2D>()) {
         if (childCollider.gameObject.name == "Land (1)") {
            childCollider.gameObject.SetActive(false);
         }
      }
      yield return new WaitForEndOfFrame();

      if (collider != null) {
         collider.gameObject.SetActive(true);
      }

      foreach (TilemapCollider2D childCollider in GetComponentsInChildren<TilemapCollider2D>(true)) {
         if (childCollider.gameObject.name == "Land (1)") {
            childCollider.gameObject.SetActive(true);
         }
      }
   }

   #region Private Variables

   #endregion
}
