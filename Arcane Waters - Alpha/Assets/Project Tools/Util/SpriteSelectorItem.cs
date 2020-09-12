using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class SpriteSelectorItem : MonoBehaviour, IPointerClickHandler
{
   #region Public Variables

   // The kind of entity this item represents
   public enum Type { Back = 1, Folder = 2, Texture = 3 }

   // The type of this item
   public Type type;

   // The icon of the item
   public Image icon;

   // The label of the item
   public Text label;

   // The background image of the item
   public Image background;

   // The button of the item
   public Button button;

   // If this is a texture, the texture path
   public string texturePath;

   // If this is a folder, which folder is it targeting
   public SpriteSelector.TextureFolder folder;

   // The sprite selector this item belongs to
   public SpriteSelector spriteSelector;

   #endregion

   public void setSelected (bool selected) {
      Util.setAlpha(background, selected ? 1f : 0f);
   }

   public void OnPointerClick (PointerEventData eventData) {
      spriteSelector.selectionItemClick(this, eventData);
   }

   #region Private Variables

   #endregion
}
