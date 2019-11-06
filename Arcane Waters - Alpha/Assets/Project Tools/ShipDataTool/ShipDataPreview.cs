using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class ShipDataPreview : MonoBehaviour {
   #region Public Variables

   // Holds the cached ship data
   public ShipData currentShipData;

   // Sprite renderers for the ship
   public SpriteRenderer shipSprite;
   public SpriteRenderer shipRippleSprite;

   // Button for closing the panel
   public Button closePanelButton;

   // Sprite simple animations
   public SimpleAnimation simpleAnimShip, simpleAnimRipple;

   // Warning popup
   public GameObject warningNotSupported;

   // Directional text label
   public Text directionText;

   // UI Notification and controls
   public Slider animSlider;

   // Toggle for flipping the sprite
   public Button toggleFlip;

   #endregion

   private void Awake () {
      closePanelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
      });

      toggleFlip.onClick.AddListener(() => {
         shipSprite.flipX = !shipSprite.flipX;
         shipRippleSprite.flipX = !shipRippleSprite.flipX;
      });
      animSlider.onValueChanged.RemoveAllListeners();
      animSlider.maxValue = Enum.GetValues(typeof(Anim.Type)).Length - 1;
      animSlider.onValueChanged.AddListener(_ => {
         simpleAnimShip.initialize();
         try {
            warningNotSupported.SetActive(false);
            simpleAnimShip.playAnimation((Anim.Type) animSlider.value);
            simpleAnimRipple.playAnimation((Anim.Type) animSlider.value);

            simpleAnimShip.isPaused = false;
            simpleAnimRipple.isPaused = false;
         } catch {
            warningNotSupported.SetActive(true);
         }
         directionText.text = ((Anim.Type) animSlider.value).ToString();
      });
   }

   public void setpreview (ShipData shipData) {
      currentShipData = shipData;

      shipSprite.sprite = ImageManager.getSprite(shipData.spritePath);
      shipRippleSprite.sprite = ImageManager.getSprite(shipData.rippleSpritePath);
      animSlider.value = 1;
      animSlider.onValueChanged.Invoke(1);
   }

   #region Private Variables
      
   #endregion
}
