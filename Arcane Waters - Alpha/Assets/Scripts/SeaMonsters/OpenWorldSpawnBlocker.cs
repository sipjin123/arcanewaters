using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class OpenWorldSpawnBlocker : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // The object that will be adjusted to determine block proximity
   public GameObject objectScaler;

   // The size of the object
   public float xSize, ySize;

   // Markers determining their world coordinates
   public Transform leftMarker, rightMarker, topMarker, bottomMarker;

   // The multiplier size from map editor into main world scale
   public const float REAL_WORLD_SCALER = 16;

   #endregion

   public bool isWithinBounds (Vector3 coordinate) {
      bool isWithinHorizontalBounds = false;
      bool isWithinVerticalBounds = false;
      if (coordinate.x < rightMarker.position.x && coordinate.x > leftMarker.position.x) {
         isWithinHorizontalBounds = true;
      }

      if (coordinate.y < topMarker.position.y && coordinate.y > bottomMarker.position.y) {
         isWithinVerticalBounds = true;
      }

      return isWithinHorizontalBounds && isWithinVerticalBounds;
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.SPAWN_BLOCK_SIZE_X_KEY) == 0) {
            try {
               xSize = float.Parse(field.v);
            } catch {
            }
         }
         if (field.k.CompareTo(DataField.SPAWN_BLOCK_SIZE_Y_KEY) == 0) {
            try {
               ySize = float.Parse(field.v);
            } catch {
            }
         }
      }

      objectScaler.transform.localScale = new Vector2(xSize * REAL_WORLD_SCALER, ySize* REAL_WORLD_SCALER);
   }

   #region Private Variables

   #endregion
}
