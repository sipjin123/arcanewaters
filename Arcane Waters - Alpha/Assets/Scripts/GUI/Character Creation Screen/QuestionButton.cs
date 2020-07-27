using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class QuestionButton : MonoBehaviour {
   #region Public Variables

   #endregion

   public void setChosenOption (CharacterCreationQuestionOption option) {
      _questionText.SetText($"+{option.perkPoints} {Perk.getCategoryDisplayName(option.perkCategory)}");
   }

   #region Private Variables

   // The original question
   private string _question;

   // The text component for the question
   [SerializeField]
   private TextMeshProUGUI _questionText;

   // The background image
   [SerializeField]
   private Image _backgroundImage;

   #endregion
}
