using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class QuestionTemplate : MonoBehaviour
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
      // Destroy the old questions
      _questionsContainer.gameObject.DestroyChildren();
      _currentOptions = new List<QuestionOptionTemplate>();

      int optionIndex = 0;

      // Create a QuestionOptionTemplate for each option
      foreach (CharacterCreationQuestionOption option in question.options) {
         QuestionOptionTemplate template = Instantiate(_questionOptionTemplate);
         template.transform.SetParent(_questionsContainer, false);

         // Set the text of the question
         template.setQuestionOption(option, optionIndex);
         template.setUnselected();

         template.button.onClick.AddListener(() => {
            // Unselect all the options
            foreach (QuestionOptionTemplate o in _currentOptions) {
               o.setUnselected();
            }

            // Select this option
            template.setSelected();

            // Let the questions screen know an option has been selected
            CharacterCreationQuestionsScreen.self.onOptionSelected();

            // Set the index of the current option as the selected option index
            this.currentSelectedOptionIndex = template.optionIndex;
         });

         // Enable the template
         template.gameObject.SetActive(true);

         // Add it to our list of currently shown options
         _currentOptions.Add(template);
         optionIndex++;
      }
   }

   #region Private Variables

   // The question text
   [SerializeField]
   private TextMeshProUGUI _questionText;

   // The UI questions container
   [SerializeField]
   private Transform _questionsContainer;

   // The current option templates
   private List<QuestionOptionTemplate> _currentOptions;

   // The question option template
   [SerializeField]
   private QuestionOptionTemplate _questionOptionTemplate;

   #endregion
}
