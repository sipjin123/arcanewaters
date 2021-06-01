﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class AdminGameSettingsRow : MonoBehaviour
{
   #region Public Variables

   // The parameter value
   public InputField valueInput;

   // The spinner buttons
   public Button upButton;
   public Button downButton;

   #endregion

   public void initialize (float value) {
      setValue(value, false);
   }

   public void onUpButtonClicked () {
      setValue(_value + 0.1f);
   }

   public void onDownButtonClicked () {
      setValue(_value - 0.1f);
   }

   public void onValueInputEndEdit () {
      if (float.TryParse(valueInput.text, out float parsedAmount)) {
         setValue(parsedAmount);
      } else {
         // Restore the previous value
         setValue(_value);
      }
   }

   private void setValue (float value, bool notifyMainPanel = true) {
      // Truncate after the second decimal
      value = Mathf.Round(value * 100f) / 100f;
      _value = value;
      valueInput.SetTextWithoutNotify(_value.ToString());

      // Disable the spinner buttons when at the limits of valid amounts
      downButton.interactable = true;
      upButton.interactable = true;
      if (_value <= 0) {
         downButton.interactable = false;
      }

      // Notify the panel
      if (notifyMainPanel) {
         PanelManager.self.adminGameSettingsPanel.onParameterRowChanged();
      }
   }

   public float getValue () {
      return _value;
   }

   #region Private Variables

   // The parameter value currently set in the input field
   private float _value = 0;

   #endregion
}