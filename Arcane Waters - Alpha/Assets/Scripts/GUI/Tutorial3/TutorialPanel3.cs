using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using TMPro;

public class TutorialPanel3 : MonoBehaviour
{
   #region Public Variables

   // The panel mode
   public enum Mode
   {
      TutorialList = 0,
      NPCSpeech = 1,
      QuestionMark = 2,
      Closed = 3
   }

   // The section containing the tutorial list
   public GameObject tutorialListSection;

   // The box containing the tutorial list and npc speech
   public GameObject tutorialPanelBox;

   // The section containing the question mark (fully minimized panel)
   public GameObject questionMark;

   // The prefab we use for creating tutorial rows
   public TutorialRow3 rowPrefab;

   // The container for the tutorial rows
   public GameObject rowContainer;

   // The text component displaying the npc text
   public TextMeshProUGUI npcSpeechText;

   // The expand button
   public GameObject expandButton;

   // A reference to the canvas group
   public CanvasGroup canvasGroup;

   // The blinking animation of the right arrow
   public GameObject rightArrowAnim;

   #endregion

   public void initialize (Mode mode, List<Tutorial3> tutorials) {
      gameObject.SetActive(true);
      canvasGroup.Hide();
      _mode = mode;
      refreshPanelConfig();
      npcSpeechText.SetText("");
      rightArrowAnim.SetActive(false);

      rowContainer.DestroyChildren();
      _rows.Clear();
      foreach (Tutorial3 tutorial in tutorials) {
         TutorialRow3 row = Instantiate(rowPrefab, rowContainer.transform, false);
         row.setRowForTutorial(tutorial);
         _rows.Add(row);
      }
   }

   public void refreshTutorialStep (string selectedTutorialKey, string npcSpeech, bool isNextStepManual) {
      _npcSpeech = npcSpeech;

      // Select the correct row
      foreach (TutorialRow3 row in _rows) {
         row.refresh(selectedTutorialKey);
      }

      // Type the npc text
      if (canvasGroup.IsShowing()) {
         AutoTyper.SlowlyRevealText(npcSpeechText, _npcSpeech);
      }

      // If the user must manually click on the right arrow to continue, animate the button
      if (isNextStepManual) {
         rightArrowAnim.SetActive(true);
      } else {
         rightArrowAnim.SetActive(false);
      }
   }

   public void Update () {
      if (Global.player == null || !AreaManager.self.hasArea(Global.player.areaKey)) {
         if (canvasGroup.IsShowing()) {
            canvasGroup.Hide();
            AutoTyper.SlowlyRevealText(npcSpeechText, "");
         }
      } else if (!canvasGroup.IsShowing()){
         canvasGroup.Show();
         AutoTyper.SlowlyRevealText(npcSpeechText, _npcSpeech);
      }
   }

   public void onTutorialRowPressed (TutorialRow3 selectedRow) {
      TutorialManager3.self.selectTutorial(selectedRow.tutorial);
   }

   public void onExpandButtonPressed () {
      switch (_mode) {
         case Mode.NPCSpeech:
            _mode = Mode.TutorialList;
            refreshPanelConfig();
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.ExpandTutorialPanel);
            break;
         case Mode.QuestionMark:
            _mode = Mode.NPCSpeech;
            refreshPanelConfig();
            AutoTyper.SlowlyRevealText(npcSpeechText, _npcSpeech);
            break;
         default:
            break;
      }
   }

   public void onMinimizeButtonPressed () {
      switch (_mode) {
         case Mode.TutorialList:
            _mode = Mode.NPCSpeech;
            refreshPanelConfig();
            break;
         case Mode.NPCSpeech:
            _mode = Mode.QuestionMark;
            refreshPanelConfig();
            break;
         default:
            break;
      }
   }

   public void onCloseButtonPressed () {
      PanelManager.self.showConfirmationPanel("Close the tutorial? You can reopen it later from the options panel",
         () => confirmClosePanel());
   }

   public void onNextStepButtonPressed () {
      TutorialManager3.self.nextStep();
   }

   public void onPreviousStepButtonPressed () {
      TutorialManager3.self.previousStep();
   }

   public void confirmClosePanel () {
      _mode = Mode.Closed;
      refreshPanelConfig();
   }

   public void openPanel () {
      if (_mode == Mode.Closed) {
         _mode = Mode.NPCSpeech;
         refreshPanelConfig();
         AutoTyper.SlowlyRevealText(npcSpeechText, _npcSpeech);
      }
   }

   public Mode getMode () {
      return _mode;
   }

   private void refreshPanelConfig () {
      if (!gameObject.activeSelf) {
         gameObject.SetActive(true);
      }

      switch (_mode) {
         case Mode.TutorialList:
            tutorialPanelBox.SetActive(true);
            tutorialListSection.SetActive(true);
            questionMark.SetActive(false);
            expandButton.SetActive(false);
            break;
         case Mode.NPCSpeech:
            tutorialPanelBox.SetActive(true);
            tutorialListSection.SetActive(false);
            questionMark.SetActive(false);
            expandButton.SetActive(true);
            break;
         case Mode.QuestionMark:
            tutorialPanelBox.SetActive(false);
            tutorialListSection.SetActive(false);
            questionMark.SetActive(true);
            expandButton.SetActive(false);
            break;
         case Mode.Closed:
            gameObject.SetActive(false);
            break;
         default:
            break;
      }
   }

   #region Private Variables

   // The current panel mode
   private Mode _mode = Mode.NPCSpeech;

   // A reference to the tutorial rows
   private List<TutorialRow3> _rows = new List<TutorialRow3>();

   // The current text being spoken by the npc
   private string _npcSpeech = "";

   #endregion
}