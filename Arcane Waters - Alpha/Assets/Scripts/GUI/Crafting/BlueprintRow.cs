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
   public Sprite blueprintSelectedSprite;

   // The row image
   public Image rowImage;

   // The icon displayed when the blueprint can be crafted
   public Image canBeCraftedImage;

   // The button of the row
   public Button rowButton;

   #endregion Public Variables

   public void setRowForBlueprint (Item resultItem, bool isSelected, Blueprint.Status status) {
      _blueprintItemId = resultItem.id;

      // Set the 'blueprint selected' background
      if (isSelected) {
         rowImage.sprite = blueprintSelectedSprite;
      }

      // Retrieve the icon sprite and coloring depending on the type
      ColorKey colorKey = null;

      if (resultItem.category == Item.Category.Weapon) {
         WeaponStatData weaponData = WeaponStatData.getStatData(resultItem.data, resultItem.itemTypeId);
         itemNameText.text = weaponData.equipmentName;
         icon.sprite = ImageManager.getSprite(weaponData.equipmentIconPath);
         colorKey = new ColorKey(Global.player.gender, resultItem.itemTypeId, new Weapon());
      } else if (resultItem.category == Item.Category.Armor) {
         ArmorStatData armorData = ArmorStatData.getStatData(resultItem.data, resultItem.itemTypeId);
         itemNameText.text = armorData.equipmentName;
         icon.sprite = ImageManager.getSprite(armorData.equipmentIconPath);
         colorKey = new ColorKey(Global.player.gender, resultItem.itemTypeId, new Armor());
      } else {
         icon.sprite = null;
      }

      // Set the sprite of the shadow
      iconShadow.sprite = icon.sprite;

      // Recolor
      if (colorKey != null) {
         recoloredSprite.recolor(colorKey, resultItem.color1, resultItem.color2);
      }
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
      CraftingPanel.self.displayBlueprint(_blueprintItemId);
   }

   #region Private Variables

   // The ID of the blueprint being displayed
   private int _blueprintItemId;

   #endregion
}
