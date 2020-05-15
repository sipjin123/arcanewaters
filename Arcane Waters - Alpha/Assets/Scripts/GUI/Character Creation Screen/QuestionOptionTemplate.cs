using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class QuestionOptionTemplate : MonoBehaviour {
   #region Public Variables
   
   // The button component
   public Button button;

   // The toggle
   public Toggle toggle;

   // The index of this option
   public int optionIndex;

   #endregion

   public void setQuestionOption (CharacterCreationQuestionOption option, int index) {
      string text = $"{option.option} <color=\"yellow\">(+{option.perkPoints} {option.perkType.ToString()})";
      _questionText.SetText(text);
      optionIndex = index;
   }

   public void setSelected () {
      _questionText.color = Color.green;
      toggle.isOn = true;
   }

   public void setUnselected () {
      _questionText.color = Color.white;
      toggle.isOn = false;
   }
   
   #region Private Variables

   // The question text
   [SerializeField] 
   private TextMeshProUGUI _questionText;

   // The question option
   private CharacterCreationQuestionOption _questionOption;

   #endregion
}
