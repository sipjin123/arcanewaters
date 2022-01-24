using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System;

public class DiscoveryPanel : Panel {
   #region Public Variables

   // The text containing the name of the discovery
   public TextMeshProUGUI discoveryNameText;

   // The text containing the description of the discovery
   public TextMeshProUGUI discoveryDescriptionText;

   // The text containing the rarity of the discovery
   public List<Image> discoveryRarityStars;

   // The image that belongs to the discovery
   public Image discoveryImage;

   // The sprite for enabled stars used for rarity
   public Sprite disabledStarSprite;

   // The sprites of the stars based on the rarity of the discovery. Must be in order.
   public List<Sprite> starSprites;

   #endregion

   public void showDiscovery (DiscoveryData data) {
      discoveryNameText.SetText(data.name);
      discoveryNameText.color = Rarity.getColor(data.rarity);
      discoveryDescriptionText.SetText(data.description);
      discoveryImage.sprite = ImageManager.getSprite(data.spriteUrl);
      setupRarityStars(data.rarity);
      PanelManager.self.linkIfNotShowing(Type.Discovery);
   }

   private void setupRarityStars (Rarity.Type rarity) {
      int rarityStars = (int) rarity;

      for (int i = 0; i < rarityStars; i++) {
         discoveryRarityStars[i].sprite = starSprites[rarityStars - 1];
      }

      for (int i = rarityStars; i < discoveryRarityStars.Count; i++) {
         discoveryRarityStars[i].sprite = disabledStarSprite;
      }
   }

   #region Private Variables

   #endregion
}
