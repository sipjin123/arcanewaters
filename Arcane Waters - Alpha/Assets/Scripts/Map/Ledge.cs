using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using MapCreationTool;

public class Ledge : MonoBehaviour, IMapEditorDataReceiver {
   #region Public Variables

   // The direction associated with this ledge
   public Direction direction = Direction.South;

   #endregion

   private void Start () {
      // The server doesn't need to both with this
      if (Util.isBatch()) {
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

   public void receiveData (DataField[] dataFields) {
      int w = 1;
      int h = 1;

      foreach (DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case DataField.LEDGE_WIDTH_KEY:
               if (int.TryParse(field.v, out int width)) {
                  w = width;
               }
               break;
            case DataField.LEDGE_HEIGHT_KEY:
               if (int.TryParse(field.v, out int height)) {
                  h = height;
               }
               break;
            default:
               Debug.LogWarning($"Unrecognized data field key: {field.k}");
               break;
         }
      }

      foreach (BoxCollider2D col in GetComponentsInChildren<BoxCollider2D>()) {
         col.size = new Vector2(w * 0.16f, h * 0.16f);
         col.offset = new Vector2(0, -h * 0.08f + 0.08f);
      }
   }

   #region Private Variables

   #endregion
}
