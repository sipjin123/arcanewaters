using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GrabbedItem : MonoBehaviour
{

   #region Public Variables

   // The item icon
   public Image icon;

   // The recolored sprite component on the icon
   public RecoloredSprite recoloredSprite;

   #endregion

   public void activate (Item item, Sprite itemSprite) {
      gameObject.SetActive(true);
      icon.sprite = itemSprite;

      // Recolor
      recoloredSprite.recolor(item.paletteNames);

      // Place under the mouse
      transform.position = MouseUtils.mousePosition;
   }

   public void Update () {
      // Follow the mouse
      transform.position = MouseUtils.mousePosition;

      // Stop grabbing when the right click button is pressed
      if (KeyUtils.GetButtonDown(MouseButton.Right)) {
         InventoryPanel.self.stopGrabbingItem();
         return;
      }

      // When the left click button is released, try to drop the item
      if (!KeyUtils.GetButton(MouseButton.Left)) {
         InventoryPanel.self.tryDropGrabbedItem(MouseUtils.mousePosition);
         return;
      }
   }

   public void deactivate () {
      if (gameObject.activeSelf) {
         gameObject.SetActive(false);
      }
   }

   #region Private Variables

   #endregion
}