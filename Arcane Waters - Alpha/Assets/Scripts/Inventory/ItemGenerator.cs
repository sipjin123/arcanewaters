public class ItemGenerator
{
   public static Item generate (Item.Category category, int itemTypeId, int count = 1, int newItemId = -1, string palettes = "", string data = "", int durability = Item.MAX_DURABILITY) {
      string itemTemplatePalette = "";
      
      if (Item.isRecolorable(category)) {
         if (EquipmentXMLManager.self != null) {
            if (category == Item.Category.Weapon) {
               if (EquipmentXMLManager.self.getWeaponData(itemTypeId) != null) {
                  itemTemplatePalette = EquipmentXMLManager.self.getWeaponData(itemTypeId).palettes;
               }
            }
            if (category == Item.Category.Armor) {
               if (EquipmentXMLManager.self.getArmorDataBySqlId(itemTypeId) != null) {
                  itemTemplatePalette = EquipmentXMLManager.self.getArmorDataBySqlId(itemTypeId).palettes;
               }
            }
            if (category == Item.Category.Hats) {
               if (EquipmentXMLManager.self.getHatData(itemTypeId) != null) {
                  itemTemplatePalette = EquipmentXMLManager.self.getHatData(itemTypeId).palettes;
               }
            }
         }
      }

      string mergedPalette = Item.parseItmPalette(Item.overridePalette(palettes, itemTemplatePalette));
      return new Item(newItemId, category, itemTypeId, count, mergedPalette, data, durability);
   }
}
