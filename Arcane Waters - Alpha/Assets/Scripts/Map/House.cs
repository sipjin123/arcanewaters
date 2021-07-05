using UnityEngine;
using MapCreationTool.Serialization;
using TMPro;

public class House : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // The tooltip game object
   public GameObject toolTipGameObj;

   // The text label of the house
   public TextMeshProUGUI textLabel;

   // If this has shop data
   public bool hasShopData = false;

   #endregion

   private void Awake () {
      warp = GetComponentInChildren<Warp>(true);
      door = GetComponentInChildren<Door>(true);
   }

   public void receiveData (DataField[] dataFields) {
      DataField targetMapField = null;

      foreach (DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case DataField.HOUSE_TARGET_MAP_KEY:
               targetMapField = field;
               warp.gameObject.SetActive(true);
               break;
            case DataField.NPC_SHOP_NAME_KEY:
               if (textLabel) {
                  hasShopData = true;
                  textLabel.text = field.v;
               }
               break;
            case DataField.HOUSE_TARGET_SPAWN_KEY:
               if (string.IsNullOrWhiteSpace(field.v)) {
                  continue;
               }
               warp.spawnTarget = field.v.Trim(' ');
               warp.gameObject.SetActive(true);
               break;
            case DataField.TARGET_MAP_INFO_KEY:
               warp.targetInfo = field.objectValue<Map>();
               break;
         }
      }

      if (targetMapField != null) {
         warp.setAreaTarget(targetMapField);
      }

      warp.updateArrow();
   }

   public void onHoverEnter () {
      if (toolTipGameObj && hasShopData) { 
         toolTipGameObj.SetActive(true);
      }
   }

   public void onHoverExit () {
      if (toolTipGameObj && hasShopData) {
         toolTipGameObj.SetActive(false);
      }
   }

   #region Private Variables

   // The warp that is associated with this house and should teleport the player inside
   private Warp warp;

   // The door of this house
   private Door door;

   #endregion

}
