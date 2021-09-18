using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipyardRow : MonoBehaviour {
   #region Public Variables

   // The icon
   public Image iconImage;

   // The ripples image
   public Image ripplesImage;

   // The text
   public Text itemName;

   // The gold amount
   public Text goldAmount;

   // The Button
   public Button buyButton;

   // The Ship associated with this row
   public ShipInfo shipInfo;

   // The stat texts
   public Text damageText;
   public Text healthText;
   public Text suppliesText;
   public Text cargoText;
   public Text speedText;
   public Text attackRangeText;
   public Text sailorsText;

   // Skill prefabs setup
   public GameObject skillPrefabHolder;
   public GameObject skillPrefab;

   #endregion

   public void setRowForItem (ShipInfo shipInfo) {
      this.shipInfo = shipInfo;
      
      Sprite[] shipSprites = ImageManager.getSprites(Ship.getSkinPath(shipInfo.shipType, shipInfo.skinType));
      if (shipSprites.Length > 0) {
         if (shipSprites.Length >= 9) { 
            iconImage.sprite = shipSprites[9];
         } else {
            iconImage.sprite = shipSprites[0];
         }
      }

      Sprite[] rippleSprites = ImageManager.getSprites(Ship.getRipplesPath(shipInfo.shipType));
      if (rippleSprites.Length >= 10) {
         ripplesImage.sprite = rippleSprites[10];
      }

      itemName.text = shipInfo.shipName;
      float perkMultiplier = 1.0f - PerkManager.self.getPerkMultiplierAdditive(Perk.Category.ShopPriceReduction);
      goldAmount.text = ((int)(shipInfo.price * perkMultiplier)) + "";

      // Fill in the stats
      damageText.text = (shipInfo.damage * 100).ToString("f1") + "%";
      healthText.text = "" + shipInfo.maxHealth;
      suppliesText.text = "" + shipInfo.suppliesMax;
      cargoText.text = "" + shipInfo.cargoMax;
      speedText.text = "" + shipInfo.speed;
      attackRangeText.text = "" + shipInfo.attackRange;
      sailorsText.text = "" + shipInfo.sailors;

      // Associate a new function with the confirmation button
      buyButton.onClick.RemoveAllListeners();
      buyButton.onClick.AddListener(() => ShipyardScreen.self.buyButtonPressed(shipInfo.shipId));
   }

   #region Private Variables

   #endregion
}
