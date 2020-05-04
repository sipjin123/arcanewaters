using UnityEngine;
using MapCreationTool.Serialization;

public class House : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   #endregion

   private void Awake () {
      warp = GetComponentInChildren<Warp>(true);
      door = GetComponentInChildren<Door>(true);
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case DataField.HOUSE_TARGET_MAP_KEY:
               if (field.tryGetIntValue(out int id)) {
                  warp.areaTarget = AreaManager.self.getAreaName(id);
               } else {
                  warp.areaTarget = field.v;
               }
               warp.gameObject.SetActive(true);
               break;
            case DataField.HOUSE_TARGET_SPAWN_KEY:
               if (string.IsNullOrWhiteSpace(field.v)) {
                  continue;
               }
               warp.spawnTarget = field.v.Trim(' ');
               warp.gameObject.SetActive(true);
               break;
            case DataField.PLACED_PREFAB_ID:
               break;
            default:
               Debug.LogWarning($"Unrecognized data field key: {field.k}");
               break;
         }
      }

      warp.updateArrow();
   }

   #region Private Variables

   // The warp that is associated with this house and should teleport the player inside
   private Warp warp;

   // The door of this house
   private Door door;

   #endregion

}
