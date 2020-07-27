using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using ProceduralMap;
using Mirror;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;

public class CharacterPortrait : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
   #region Public Variables

   // The character stack
   public CharacterStack characterStack;

   // The icon displayed when the portrait info is not available
   public GameObject unknownIcon;

   // The background image
   public Image backgroundImage;

   // The background sprites
   public Sprite seaBackground;
   public Sprite landBackground;
   public Sprite combatBackground;
   public Sprite unknownBackground;

   // Whether to use animations (size increase, overlay fading) on mouse enter/exit
   public bool useMouseEventAnimations = false;

   // The toggle component
   [HideInInspector]
   public Toggle toggle;

   // The overlay image
   public Image overlayImage;

   // The scale of the icon when the mouse is over the portrait
   public float scaleOnMouseHover = 1.5f;

   #endregion

   public void Awake () {
      characterStack.pauseAnimation();
      initializeComponents();

      _originalScale = transform.localScale;

      if (overlayImage != null) {
         _originalOverlayColor = overlayImage.color;
      }
   }

   public void initializeComponents () {
      toggle = GetComponentInChildren<Toggle>(true);
      _shadow = GetComponentInChildren<Shadow>(true);

      if (useMouseEventAnimations && overlayImage != null) {
         toggle.onValueChanged.AddListener(onToggleValueChanged);
      }            
   }

   private void onToggleValueChanged (bool isSelected) {
      if (overlayImage != null) {
         overlayImage.enabled = !isSelected;
      }

      if (_shadow != null) {
         _shadow.effectDistance = isSelected ? new Vector2(4, -4) : new Vector2(2, -2);
      }
   }

   public void setIsSelected (bool isSelected) {
      toggle.SetIsOnWithoutNotify(isSelected);
      onToggleValueChanged(isSelected);
   }

   public void initialize (NetEntity entity) {
      // If the entity is null, display a question mark
      if (entity == null) {
         unknownIcon.SetActive(true);
         backgroundImage.sprite = unknownBackground;
         return;
      }
      
      // Update the character stack
      characterStack.updateLayers(entity);
      characterStack.setDirection(Direction.East);

      // Set the background
      updateBackground(entity);

      // Hide the question mark icon
      unknownIcon.SetActive(false);
   }

   public void updateBackground (NetEntity entity) {
      if (entity.hasAnyCombat()) {
         backgroundImage.sprite = combatBackground;
      } else if (entity is SeaEntity) {
         backgroundImage.sprite = seaBackground;
      } else {
         backgroundImage.sprite = landBackground;
      }
   }

   public void OnPointerEnter (PointerEventData eventData) {
      if (!useMouseEventAnimations) {
         return;
      }

      // Move this icon to the bottom of the hierarchy so it's not covered by other icons
      transform.SetAsLastSibling();

      transform.localScale = _originalScale * scaleOnMouseHover;

      if (overlayImage != null) {
         overlayImage.color = new Color(0, 0, 0, 0);
      }

      if (_shadow != null) {
         _shadow.enabled = false;
      }
   }

   public void OnPointerExit (PointerEventData eventData) {
      if (!useMouseEventAnimations) {
         return;
      }

      transform.localScale = _originalScale;

      if (overlayImage != null) {
         overlayImage.color = _originalOverlayColor;
      }

      if (_shadow != null) {
         _shadow.enabled = true;
      }
   }

   #region Private Variables

   // The original scale
   private Vector3 _originalScale;

   // The original color of the overlay (used for highlighting)
   private Color _originalOverlayColor;

   // The shadow component
   private Shadow _shadow;

   #endregion
}
