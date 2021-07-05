﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;

public class GUI_SoundEffectsTab : GUI_SoundEffects {
   #region Public Variables

   public override void OnPointerEnter (PointerEventData eventData) {
      
   }

   public override void OnPointerClick (PointerEventData eventData) {

   }

   public override void OnPointerDown (PointerEventData eventData) {
      if (_button && _button.IsInteractable()) {
         SoundEffectManager.self.playFmod2D(SoundEffectManager.CLICK_TAB);
      }
   }

   #endregion

   #region Private Variables

   #endregion
}
