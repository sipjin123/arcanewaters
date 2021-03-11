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

   // The background image
   public Image backgroundImage;

   // The background sprites
   public Sprite seaBackground;
   public Sprite landBackground;
   public Sprite combatBackground;
   public Sprite unknownBackground;

   // The layout group for the portraits
   public LayoutGroup layoutGroup;

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

      // Set the default background
      updateBackground(null);

      // Get reference to the layout group
      layoutGroup = this.transform.parent.GetComponent<LayoutGroup>();
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

   public void updateLayers (NetEntity entity) {
      updateLayers(entity.gender, entity.bodyType, entity.eyesType, entity.hairType, entity.eyesPalettes, entity.hairPalettes, entity.getArmorCharacteristics(), entity.getWeaponCharacteristics(), entity.getHatCharacteristics());

      // Set the background
      updateBackground(entity);
   }

   public void updateLayers (Gender.Type gender, BodyLayer.Type bodyType, EyesLayer.Type eyesType, HairLayer.Type hairType, string eyesPalettes, string hairPalettes, Armor armor, Weapon weapon, Hat hat) {
      // Update the character stack
      characterStack.updateLayers(gender, bodyType, eyesType, hairType, eyesPalettes, hairPalettes, armor, weapon, hat);
      characterStack.setDirection(Direction.East);
   }

   public void updateBackground (NetEntity entity) {
      if (entity == null) {
         backgroundImage.sprite = unknownBackground;
      } else if (entity.hasAnyCombat()) {
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

      // Disable layout group so we can rearrange the portrait heirarchy
      layoutGroup.enabled = false;

      // Store the original position in the heirarchy
      _portraitIndex = this.transform.GetSiblingIndex();

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

      // Return portrait to original heirachy position
      this.transform.SetSiblingIndex(_portraitIndex);
   }

   #region Private Variables

   // The original scale
   private Vector3 _originalScale;

   // The original color of the overlay (used for highlighting)
   private Color _originalOverlayColor;

   // The shadow component
   private Shadow _shadow;

   // The index of the portait in the list
   private int _portraitIndex;

   #endregion
}
