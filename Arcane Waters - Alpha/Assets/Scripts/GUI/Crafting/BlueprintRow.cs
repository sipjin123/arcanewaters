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

   public void setRowForBlueprint (Item resultItem, Blueprint blueprint, bool isSelected, Blueprint.Status status) {
      _blueprintItemId = blueprint.id;
      itemNameText.text = resultItem.getName();

      // Set the 'blueprint selected' background
      if (isSelected) {
         rowImage.sprite = blueprintSelectedSprite;
      }

      // Retrieve the icon sprite and coloring depending on the type
      ColorKey colorKey = null;
      switch (resultItem.category) {
         case Item.Category.Weapon:
            icon.sprite = ImageManager.getSprite(resultItem.getIconPath());
            colorKey = new ColorKey(Global.player.gender, resultItem.itemTypeId, new Weapon());
            break;
         case Item.Category.Armor:
         case Item.Category.Helm:
            icon.sprite = ImageManager.getSprite(resultItem.getIconPath());
            colorKey = new ColorKey(Global.player.gender, resultItem.itemTypeId, new Armor());
            break;
         default:
            icon.sprite = null;
            break;
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
