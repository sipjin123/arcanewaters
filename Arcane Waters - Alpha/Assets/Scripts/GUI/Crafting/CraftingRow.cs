using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CraftingRow : MonoBehaviour
{
   #region Public Variables
   public Item item;

   public Button button;

   public Image icon, selectionIcon;

   public Text nameText;

   public bool hasData;

   public Sprite emptySprite;
   #endregion

   public void InjectItem (Item itemvar) {
      hasData = true;
      item = itemvar;
      nameText.text = item.getName();
      icon.sprite = ImageManager.getSprite(item.getIconPath());
   }
   public void PurgeData () {
      hasData = false;
      item = null;
      nameText.text = "";
      icon.sprite = emptySprite;
   }

   public void SelectItem () {
      selectionIcon.enabled = true;
   }

   public void UnselectItem () {
      selectionIcon.enabled = false;
   }

}
