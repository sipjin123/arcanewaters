using UnityEngine;
using UnityEngine.UI;

public class HoverableItemIcon : MonoBehaviour
{
   #region Public Variables

   #endregion

   private void Awake () {
      // Disable tooptip initially
      GetComponent<ToolTipComponent>().enabled = false;
   }

   private void OnEnable () {
      RPCManager.itemDataReceived += itemDataReceived;
   }

   private void OnDisable () {
      RPCManager.itemDataReceived -= itemDataReceived;
   }

   private void itemDataReceived (Item item) {
      if (item != null && item.id == _itemId) {
         item = item.getCastItem();
         // We received our associated item
         _item = item;

         // Some items seem to not get their names from database, but can be found in static data
         if (string.IsNullOrEmpty(item.itemName) && Item.isUsingEquipmentXML(item.category) && EquipmentXMLManager.self != null) {
            if (item.category == Item.Category.Weapon) {
               item.itemName = EquipmentXMLManager.self.getWeaponData(item.itemTypeId)?.equipmentName;
            } else if (item.category == Item.Category.Armor) {
               item.itemName = EquipmentXMLManager.self.getArmorDataBySqlId(item.itemTypeId)?.equipmentName;
            } else if (item.category == Item.Category.Hats) {
               item.itemName = EquipmentXMLManager.self.getHatData(item.itemTypeId)?.equipmentName;
            }
         }

         Sprite sprite = ImageManager.getSprite(item.getBorderlessIconPath());
         if (sprite != null && sprite != ImageManager.self.blankSprite) {
            GetComponent<Image>().sprite = sprite;
            GetComponent<Image>().color = Color.white;
         }

         ToolTipComponent tooltip = GetComponent<ToolTipComponent>();

         if (_interactable) {
            tooltip.enabled = true;
         }

         tooltip.message = item.category == Item.Category.Blueprint ? EquipmentXMLManager.self.getItemName(item) : item.getTooltip();
         tooltip.message += Item.isUsingEquipmentXML(item.category) ? "\nDurability = " + item.durability : "";
      }
   }

   public void setItemId (int id, bool interactable = true) {
      _itemId = id;
      _interactable = interactable;

      GetComponent<ToolTipComponent>().enabled = interactable;

      // Lets fetch the corresponding item from the server
      if (Global.player != null && Global.player.rpc != null) {
         Global.player.rpc.Cmd_RequestItemData(id);
      }
   }

   public Item getItem () {
      return _item;
   }

   #region Private Variables

   // The item this icon is representing
   private Item _item = null;

   // The item id of the item this icon is representing
   private int _itemId = -1;

   // Can you interact with this icon
   private bool _interactable = true;

   #endregion
}
