public class ItemGenerator
{
   public static Item generate (Item.Category category, int itemTypeId, int count = 1, int newItemId = -1, string palettes = "", string data = "", int durability = Item.MAX_DURABILITY) {
      string itemTemplatePalette = "";
      
      if (Item.isRecolorable(category)) {
         if (EquipmentXMLManager.self != null) {
            if (category == Item.Category.Weapon) {
               if (EquipmentXMLManager.self.getWeaponData(itemTypeId) != null) {
                  itemTemplatePalette = PaletteSwapManager.extractPalettes(EquipmentXMLManager.self.getWeaponData(itemTypeId).defaultPalettes);
               }
            }
            if (category == Item.Category.Armor) {
               if (EquipmentXMLManager.self.getArmorDataBySqlId(itemTypeId) != null) {
                  itemTemplatePalette = PaletteSwapManager.extractPalettes(EquipmentXMLManager.self.getArmorDataBySqlId(itemTypeId).defaultPalettes);
               }
            }
            if (category == Item.Category.Hats) {
               if (EquipmentXMLManager.self.getHatData(itemTypeId) != null) {
                  itemTemplatePalette = PaletteSwapManager.extractPalettes(EquipmentXMLManager.self.getHatData(itemTypeId).defaultPalettes);
               }
            }
            if (category == Item.Category.Ring) {
               if (EquipmentXMLManager.self.getRingData(itemTypeId) != null) {
                  itemTemplatePalette = PaletteSwapManager.extractPalettes(EquipmentXMLManager.self.getRingData(itemTypeId).defaultPalettes);
               }
            }
            if (category == Item.Category.Necklace) {
               if (EquipmentXMLManager.self.getNecklaceData(itemTypeId) != null) {
                  itemTemplatePalette = PaletteSwapManager.extractPalettes(EquipmentXMLManager.self.getNecklaceData(itemTypeId).defaultPalettes);
               }
            }
            if (category == Item.Category.Trinket) {
               if (EquipmentXMLManager.self.getTrinketData(itemTypeId) != null) {
                  itemTemplatePalette = PaletteSwapManager.extractPalettes(EquipmentXMLManager.self.getTrinketData(itemTypeId).defaultPalettes);
               }
            }
         }
      }

      string mergedPalette = "";
      try {
         if (itemTemplatePalette.Length > 0) {
            mergedPalette = Item.parseItmPalette(Item.overridePalette(palettes, itemTemplatePalette));
         }
      } catch {
         D.debug("Failed to MergedPalette: " + mergedPalette + ":" + palettes + ":" + itemTemplatePalette);
      }

      return new Item(newItemId, category, itemTypeId, count, mergedPalette, data, durability);
   }
}
