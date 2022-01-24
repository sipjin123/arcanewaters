using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class QuestionOptionTemplate : MonoBehaviour {
   #region Public Variables
   
   // The button component
   [HideInInspector]
   public Button button;

   // The index of this option
   public int optionIndex;

   #endregion

   private void Awake () {
      button = GetComponent<Button>();   
   }

   public void setQuestionOption (CharacterCreationQuestionOption option, int index) {
      string text = $"{option.option} <color=\"yellow\">(+{option.perkPoints} {Perk.getCategoryDisplayName(option.perkCategory)})";
      _questionText.SetText(text);
      optionIndex = index;
   }

   public void setSelected () {
      _questionText.color = Color.green;
   }

   public void setUnselected () {
      _questionText.color = Color.white;
   }
   
   #region Private Variables

   // The question text
   [SerializeField] 
   private TextMeshProUGUI _questionText = default;

   #endregion
}
