using UnityEngine;
using UnityEngine.UI;

public class BlueprintRow : MonoBehaviour
{
   #region Public Variables

   // The icon of the item
   public Image icon;

   // The shadow of the icon
   public Image iconShadow;

   // The recolored sprite component on the icon
   public RecoloredSprite recoloredSprite;

   // The name of the result item
   public Text itemNameText;

   // The row sprite to use when the blueprint is selected
   public Sprite blueprintSelectedSprite, defaultSelectionSprite;

   // The row image
   public Image rowImage;

   // The icon displayed when the blueprint can be crafted
   public Image canBeCraftedImage;

   // Enable the highlighted object
   public GameObject highlightObj;

   // The button of the row
   public Button rowButton;

   // The ID of the blueprint being displayed
   public int blueprintItemId;

   // The item type
   public int itemType;

   // The item category
   public Item.Category itemCategory;

   #endregion Public Variables

   public void highlightTemplate (bool isHighlighted) {
      if (isHighlighted) {
         rowImage.sprite = blueprintSelectedSprite;
         highlightObj.SetActive(true);
         rowButton.enabled = false;
      } else {
         rowImage.sprite = defaultSelectionSprite;
         highlightObj.SetActive(false);
         rowButton.enabled = true;
      }
   }

   public void setRowForBlueprint (Item resultItem, bool isSelected, Blueprint.Status status) {
      blueprintItemId = resultItem.id;
      itemCategory = resultItem.category;
      itemType = resultItem.itemTypeId;

      // Set the 'blueprint selected' background
      if (isSelected) {
         rowImage.sprite = blueprintSelectedSprite;
      }

      // Retrieve the icon sprite and coloring depending on the type
      if (resultItem.category == Item.Category.Weapon) {
         WeaponStatData weaponData = WeaponStatData.getStatData(resultItem.data, resultItem.itemTypeId);
         if (weaponData != null) {
            itemNameText.text = weaponData.equipmentName;
            icon.sprite = ImageManager.getSprite(weaponData.equipmentIconPath);

            resultItem.paletteNames = PaletteSwapManager.extractPalettes(weaponData.defaultPalettes);
         } else {
            D.debug("Missing: {" + resultItem.category + "} Blueprint");
         }
      } else if (resultItem.category == Item.Category.Armor) {
         ArmorStatData armorData = ArmorStatData.getStatData(resultItem.data, resultItem.itemTypeId);
         if (armorData != null) {
            itemNameText.text = armorData.equipmentName;
            icon.sprite = ImageManager.getSprite(armorData.equipmentIconPath);

            resultItem.paletteNames = PaletteSwapManager.extractPalettes(armorData.defaultPalettes);
         } else {
            D.debug("Missing: {" + resultItem.category + "} Blueprint");
         }
      } else if (resultItem.category == Item.Category.Hats) {
         HatStatData hatData = HatStatData.getStatData(resultItem.data, resultItem.itemTypeId);
         if (hatData != null) {
            itemNameText.text = hatData.equipmentName;
            icon.sprite = ImageManager.getSprite(hatData.equipmentIconPath);

            resultItem.paletteNames = PaletteSwapManager.extractPalettes(hatData.defaultPalettes);
         } else {
            D.debug("Missing: {" + resultItem.category + "} Blueprint");
         }
      } else if (resultItem.category == Item.Category.Ring) {
         RingStatData ringData = RingStatData.getStatData(resultItem.data, resultItem.itemTypeId);
         if (ringData != null) {
            itemNameText.text = ringData.equipmentName;
            icon.sprite = ImageManager.getSprite(ringData.equipmentIconPath);

            resultItem.paletteNames = PaletteSwapManager.extractPalettes(ringData.defaultPalettes);
         } else {
            D.debug("Missing: {" + resultItem.category + "} Blueprint");
         }
      } else if (resultItem.category == Item.Category.Necklace) {
         NecklaceStatData necklaceData = NecklaceStatData.getStatData(resultItem.data, resultItem.itemTypeId);
         if (necklaceData != null) {
            itemNameText.text = necklaceData.equipmentName;
            icon.sprite = ImageManager.getSprite(necklaceData.equipmentIconPath);

            resultItem.paletteNames = PaletteSwapManager.extractPalettes(necklaceData.defaultPalettes);
         } else {
            D.debug("Missing: {" + resultItem.category + "} Blueprint");
         }
      } else if (resultItem.category == Item.Category.Trinket) {
         TrinketStatData trinketData = TrinketStatData.getStatData(resultItem.data, resultItem.itemTypeId);
         if (trinketData != null) {
            itemNameText.text = trinketData.equipmentName;
            icon.sprite = ImageManager.getSprite(trinketData.equipmentIconPath);

            resultItem.paletteNames = PaletteSwapManager.extractPalettes(trinketData.defaultPalettes);
         } else{
            D.debug("Missing: {" + resultItem.category + "} Blueprint");
         }
      } else if (resultItem.category == Item.Category.CraftingIngredients) {
         CraftingIngredients ingredientReference = new CraftingIngredients(resultItem);
         itemNameText.text = ingredientReference.getName();
         icon.sprite = ImageManager.getSprite(ingredientReference.getBorderlessIconPath());
      } else if (resultItem.category == Item.Category.Crop) {
         CropItem cropItemReference = new CropItem(resultItem);
         itemNameText.text = cropItemReference.getName();
         icon.sprite = ImageManager.getSprite(cropItemReference.getBorderlessIconPath());
      } else {
         icon.sprite = null;
      }

      // Set the sprite of the shadow
      iconShadow.sprite = icon.sprite;

      // Recolor
      recoloredSprite.recolor(resultItem.paletteNames);
      switch (status) {
         case Blueprint.Status.Craftable:
            canBeCraftedImage.enabled = true;
            break;
         case Blueprint.Status.NotCraftable:
            canBeCraftedImage.enabled = false;
            break;
         case Blueprint.Status.MissingRecipe:
            canBeCraftedImage.enabled = false;
            rowButton.interactable = false;
            break;
         default:
            break;
      }
   }

   public void onRowButtonPress () {
      CraftingPanel.self.displayBlueprint(blueprintItemId, itemCategory, itemType);
   }

   #region Private Variables

   #endregion
}
