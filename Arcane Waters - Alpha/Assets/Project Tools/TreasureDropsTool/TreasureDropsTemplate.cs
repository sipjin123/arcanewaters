using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TreasureDropsTemplate : MonoBehaviour {
   #region Public Variables

   // The name of the group
   public Text lootGroupName;

   // Name of the biome
   public Text biomeTypeText;

   // The select button
   public Button selectButton;

   // The image type of the loot group
   public Image lootGroupImage;

   // The sprites indicating the type of loot drop
   public Sprite monsterDrop;
   public Sprite biomeDrop;

   #endregion

   public void setImage (bool isBiomeLoot) {
      lootGroupImage.sprite = isBiomeLoot ? biomeDrop : monsterDrop;
   }

   #region Private Variables
      
   #endregion
}
