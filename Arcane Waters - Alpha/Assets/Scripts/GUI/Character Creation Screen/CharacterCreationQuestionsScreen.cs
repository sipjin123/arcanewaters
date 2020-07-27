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

   // The questions to be asked during character creation
   public List<CharacterCreationQuestion> questions;

   // A list containing the option chosen for each question
   public int[] chosenAnswers;

   // Self
   public static CharacterCreationQuestionsScreen self;

   #endregion

   private void Awake () {
      self = this;
   }

   public void startQuestionnaire () {
      // Initialize all answers as -1, meaning an answer hasn't been chosen yet
      chosenAnswers = new int[3];
      for (int i = 0; i < chosenAnswers.Length; i++) {
         chosenAnswers[i] = -1;
      }
   }

   public List<Perk> getPerkResults (int[] answers) {
      if (answers.Length != questions.Count) {
         D.error("Number of answers doesn't match number of questions. This shouldn't happen.");
      }

      List<Perk> results = new List<Perk>();

      for (int i = 0; i < answers.Length; i++) {
         int answerIndex = answers[i];

         if (answers[i] >= 0) {
            CharacterCreationQuestion question = questions[i];
            CharacterCreationQuestionOption option = question.options[answerIndex];

            results.Add(new Perk(option.perkId, option.perkPoints));
         } else {
            results.Add(new Perk(Perk.UNASSIGNED_ID, 1));
         }
      }

      return results;
   }

   public void showQuestion (int index) {
      _questionPanel.gameObject.SetActive(true);
      _questionPanel.setUpQuestion(questions[index]);
      _currentQuestionIndex = index;
   }

   public void confirmAnswerClicked (int optionIndex) {
      chosenAnswers[_currentQuestionIndex] = optionIndex;

      updateQuestionButtons();

      _questionPanel.gameObject.SetActive(false);
   }

   private void updateQuestionButtons () {
      int i = 0;

      foreach (QuestionButton button in _questionButtons) {
         // Update the text of the button if an option has been chosen for the question
         if (chosenAnswers[i] > -1) {
            CharacterCreationQuestion question = questions[i];
            button.setChosenOption(question.options[chosenAnswers[i]]);
         }

         i++;
      }
   }

   #region Private Variables

   // The current question index
   private int _currentQuestionIndex;

   // The question panel
   [SerializeField]
   private QuestionPanel _questionPanel;

   [SerializeField]
   private List<QuestionButton> _questionButtons;

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
   public Perk.Category perkCategory;

   // The unique ID of the perk
   public int perkId;

   // How many perk points this option gives
   public int perkPoints;
}