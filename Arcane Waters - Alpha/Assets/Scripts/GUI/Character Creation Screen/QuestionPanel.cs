using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class QuestionPanel : MonoBehaviour
{
   #region Public Variables

   // The index of the option currently selected
   public int currentSelectedOptionIndex = -1;

   #endregion

   public void setUpQuestion (CharacterCreationQuestion question) {
      // Set the question
      _questionText.SetText(question.question);

      // Set the options
      setOptions(question);

      currentSelectedOptionIndex = -1;
   }

   private void setOptions (CharacterCreationQuestion question) {
      for (int i = 0; i < 4; i++) {
         CharacterCreationQuestionOption option = question.options[i];
         QuestionOptionTemplate template = _optionButtons[i];
         template.setQuestionOption(option, i);

         template.button.onClick.AddListener(() => {
            // Unselect all the options
            foreach (QuestionOptionTemplate o in _optionButtons) {
               o.setUnselected();
            }

            currentSelectedOptionIndex = template.optionIndex;
            CharacterCreationQuestionsScreen.self.confirmAnswerClicked(template.optionIndex);
         });
      }
   }

   #region Private Variables

   // The question text
   [SerializeField]
   private TextMeshProUGUI _questionText;

   // The current option templates
   [SerializeField]
   private List<QuestionOptionTemplate> _optionButtons;

   #endregion
}
