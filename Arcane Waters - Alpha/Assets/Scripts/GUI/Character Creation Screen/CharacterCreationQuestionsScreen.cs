using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using DG.Tweening;

public class CharacterCreationQuestionsScreen : MonoBehaviour
{
   #region Public Variables

   // The transform as a rect transform
   public RectTransform rectTransform;

   // The questions to be asked during character creation
   public List<CharacterCreationQuestion> questions;

   // A list containing the option chosen for each question
   public List<int> chosenAnswers = new List<int>();

   // Self
   public static CharacterCreationQuestionsScreen self;

   #endregion

   private void Awake () {
      self = this;
      rectTransform = transform as RectTransform;

      _horizontalScrollAmount = -_questionsContainer.rect.width;
   }

   public void startQuestionnaire () {
      chosenAnswers = new List<int>();
      _currentQuestions = new List<QuestionTemplate>();
      _currentQuestionIndex = 0;

      _questionsContainer.anchoredPosition = new Vector2(0, _questionsContainer.anchoredPosition.y);

      createQuestions();
   }

   private void createQuestions () {
      _questionsContainer.gameObject.DestroyChildren();

      foreach (CharacterCreationQuestion question in questions) {
         QuestionTemplate template = Instantiate(_questionTemplate, _questionsContainer, false);
         template.setUpQuestion(question);
         _currentQuestions.Add(template);

         template.gameObject.SetActive(true);
      }
   }

   public List<Perk> getPerkResults (List<int> answers) {
      if (answers.Count != questions.Count) {
         D.error("Number of answers doesn't match number of questions. This shouldn't happen.");
      }

      List<Perk> results = new List<Perk>();

      for (int i = 0; i < answers.Count; i++) {
         int answerIndex = answers[i];

         if (answers[i] >= 0) {
            CharacterCreationQuestion question = questions[i];
            CharacterCreationQuestionOption option = question.options[answerIndex];

            results.Add(new Perk(option.perkType, option.perkPoints));
         } else {
            results.Add(new Perk(Perk.Type.None, 1));
         }
      }

      return results;
   }

   private void focusOnQuestion (int index) {
      _moveQuestionsTween?.Kill();

      Vector3 nextPosition = _questionsContainer.anchoredPosition;
      nextPosition.x = _horizontalScrollAmount * index;
      _questionsContainer.DOAnchorPos(nextPosition, CharacterCreationPanel.WINDOW_TRANSITION_TIME);

      // Set the button text to let the player know he's about to finish the questionnaire 
      if (isLastQuestion()) {
         CharacterCreationPanel.self.setNextButtonToFinish();
      } else {
         CharacterCreationPanel.self.setNextButtonToNormal();
      }

      // The button should only be enabled if an option has already been selected for this question
      _confirmAnswerButton.interactable = _currentQuestions[_currentQuestionIndex].currentSelectedOptionIndex >= 0;
   }

   public void setNextQuestion () {
      _currentQuestionIndex++;
      focusOnQuestion(_currentQuestionIndex);
   }

   public void setPreviousQuestion () {
      _currentQuestionIndex--;
      focusOnQuestion(_currentQuestionIndex);
   }

   private bool isFirstQuestion () {
      return _currentQuestionIndex == 0;
   }

   private bool isLastQuestion () {
      return _currentQuestionIndex == questions.Count - 1;
   }

   public void confirmAnswerClicked () {
      int selectedOption = getCurrentQuestion().currentSelectedOptionIndex;

      // If we have an answer for the current question, replace it. Otherwise, add it to the list.
      if (_currentQuestionIndex < chosenAnswers.Count) {
         chosenAnswers[_currentQuestionIndex] = selectedOption;
      } else {
         chosenAnswers.Add(selectedOption);
      }

      if (!isLastQuestion()) {
         setNextQuestion();
      } else {
         // Show the confirmation panel before submitting the character creation
         PanelManager.self.showConfirmationPanel("Are you sure you want to create this character?", () => CharacterCreationPanel.self.submitCharacterCreation());
      }
   }

   public void onOptionSelected () {
      // The "confirm answer" question button will be disabled until an option is selected.
      _confirmAnswerButton.interactable = true;
   }

   public void previousQuestionClicked () {
      if (!isFirstQuestion()) {
         setPreviousQuestion();
      } else {
         CharacterCreationPanel.self.returnToAppearanceScreen();
      }
   }

   public void onShown () {
      _confirmAnswerButton.interactable = _currentQuestions[_currentQuestionIndex].currentSelectedOptionIndex >= 0;
   }

   private QuestionTemplate getCurrentQuestion () {
      return _currentQuestions[_currentQuestionIndex];
   }

   #region Private Variables

   // The question template
   [SerializeField]
   private QuestionTemplate _questionTemplate;

   // The questions container
   [SerializeField]
   private RectTransform _questionsContainer;

   // The confirm answer button
   [SerializeField]
   private Button _confirmAnswerButton;

   // The text of the confirm answer button
   [SerializeField]
   private Text _confirmAnswerButtonText;

   // The current question index
   private int _currentQuestionIndex;

   // The spawned questions
   private List<QuestionTemplate> _currentQuestions = new List<QuestionTemplate>();

   // How much should we move the panel between questions. This should be negative and equal to the width of the container.
   private float _horizontalScrollAmount = 200.0f;

   // The tween that moves the questions around
   private Tween _moveQuestionsTween;

   #endregion
}

[System.Serializable]
public struct CharacterCreationQuestion
{
   // The question itself
   public string question;

   // The question options
   public List<CharacterCreationQuestionOption> options;
}

[System.Serializable]
public struct CharacterCreationQuestionOption
{
   // The option text
   public string option;

   // The perk this option gives
   public Perk.Type perkType;

   // How many perk points this option gives
   public int perkPoints;
}