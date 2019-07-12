using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GenericButton : Button {
   #region Public Variables

   #endregion

   protected override void Awake () {
      base.Awake();

      // Check if we already have a sound FX component
      GUI_SoundEffects fx = GetComponent<GUI_SoundEffects>();

      // If not, go ahead and add one
      if (fx == null) {
         this.gameObject.AddComponent<GUI_SoundEffects>();
      }

      // Store references
      _image = GetComponent<Image>();
      _layoutGroup = GetComponent<HorizontalLayoutGroup>();

      // Note our current top padding
      _initialPadding = _layoutGroup.padding.top;
   }

   private void Update () {
      if (_layoutGroup == null) {
         return;
      }

      // Adjust the padding top when the button gets clicked
      updatePadding();
   }

   protected override void OnEnable () {
      base.OnEnable();

      updatePadding();
   }

   protected bool isInClickMode () {
      if (_image == null) {
         return false;
      }

      return this.currentSelectionState == SelectionState.Pressed;
   }

   protected void updatePadding () {
      // Adjust the padding top when the button gets clicked (but only when the game is running, not when we're making changes in the editor)
      if (Application.isPlaying) {
         _layoutGroup.padding.top = isInClickMode() ? _initialPadding * 2 : _initialPadding;
      }
   }

   #region Private Variables

   // The initial top padding
   protected int _initialPadding;

   // Our image
   protected Image _image;

   // Our layout
   protected HorizontalLayoutGroup _layoutGroup;

   #endregion
}
