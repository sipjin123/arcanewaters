using UnityEngine;
using MapCreationTool;

public class House : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   #endregion

   private void Awake () {
      warp = GetComponentInChildren<Warp>(true);
      door = GetComponentInChildren<Door>(true);
   }

   public void receiveData (MapCreationTool.Serialization.DataField[] dataFields) {
      foreach (MapCreationTool.Serialization.DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case "target map":
               if (string.IsNullOrWhiteSpace(field.v)) {
                  continue;
               }
               warp.areaTarget = field.v.Trim(' ');
               if (!door.isLocked) {
                  warp.gameObject.SetActive(true);
               }
               break;
            case "target spawn":
               if (string.IsNullOrWhiteSpace(field.v)) {
                  continue;
               }
               warp.spawnTarget = field.v.Trim(' ');
               if (!door.isLocked) {
                  warp.gameObject.SetActive(true);
               }
               break;
            case "locked":
               if (bool.TryParse(field.v, out bool locked)) {
                  door.isLocked = locked;
                  if (!door.isLocked) {
                     warp.gameObject.SetActive(true);
                  }
               }
               break;
            default:
               Debug.LogWarning($"Unrecognized data field key: {field.k}");
               break;
         }
      }
   }

   #region Private Variables

   // The warp that is associated with this house and should teleport the player inside
   private Warp warp;

   // The door of this house
   private Door door;

   #endregion

}
