using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FlagshipRow : MonoBehaviour {
   #region Public Variables

   // The icon
   public Image iconImage;

   // The text
   public Text itemName;

   // The Ship associated with this row
   public ShipInfo shipInfo;

   // The button for assigning the flagship
   public Button flagshipButton;

   // The Image that shows up for the Flagship row
   public Image flagshipIcon; 

   // The stat texts
   public Text damageText;
   public Text healthText;
   public Text suppliesText;
   public Text cargoText;
   public Text speedText;
   public Text attackRangeText;
   public Text sailorsText;

   #endregion

   public void setRowForItem (ShipInfo shipInfo) {
      this.shipInfo = shipInfo;

      // Update the icon name and text
      Sprite[] sprites = ImageManager.getSprites(Ship.getSkinPath(shipInfo.shipType, shipInfo.skinType));
      if (sprites.Length >= 9) {
         iconImage.sprite = sprites[9];
      } else {
         iconImage.sprite = sprites[0];
      }
      itemName.text = "" + shipInfo.shipName;
      itemName.color = Rarity.getColor(shipInfo.rarity);

      // Fill in the stats
      damageText.text = "" + shipInfo.damage;
      healthText.text = "" + shipInfo.maxHealth;
      suppliesText.text = "" + shipInfo.suppliesMax;
      cargoText.text = "" + shipInfo.cargoMax;
      speedText.text = "" + shipInfo.speed;
      attackRangeText.text = "" + shipInfo.attackRange;
      sailorsText.text = "" + shipInfo.sailors;

      // Disable the flagship button at sea
      flagshipButton.interactable = (Global.player is BodyEntity);

      // Toggle the flagship icon appropriately
      flagshipIcon.enabled = shipInfo.shipId == FlagshipPanel.playerFlagshipId;
   }

   public void chooseThisAsFlagship () {
      Global.player.rpc.Cmd_RequestNewFlagship(shipInfo.shipId);
   }

   #region Private Variables

   #endregion
}
