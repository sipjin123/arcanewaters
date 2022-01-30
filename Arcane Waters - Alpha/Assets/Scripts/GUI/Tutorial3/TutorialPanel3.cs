using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using TMPro;
using UnityEngine.InputSystem;

public class TutorialPanel3 : MonoBehaviour
{
   #region Public Variables

   // The panel mode
   public enum Mode
   {
      TutorialList = 0,
      NPCSpeech = 1,
      Closed = 3
   }

   // The time we wait after the player warped to another area to display the tutorial panel again
   public static float DELAY_AFTER_WARP = 1f;

   // The speed at which the hidden title row fades in and out
   public static float FADE_SPEED = 8f;

   // The section containing the tutorial list
   public CanvasGroup tutorialListSection;

   // The prefab we use for creating tutorial rows
   public TutorialRow3 rowPrefab;

   // The container for the tutorial rows
   public GameObject rowContainer;

   // The text component displaying the npc text
   public TextMeshProUGUI npcSpeechText;

   // The button container in 'NPC Speech' mode
   public CanvasGroup navigationRowButtonCanvasGroup;

   // A reference to the canvas group
   public CanvasGroup canvasGroup;

   // The blinking animation of the expand button
   public GameObject expandButtonAnim;

   // The blinking animation of the right button
   public GameObject rightButtonAnim;

   // The button allowing to return to the previous step
   public Button leftButton;

   // The button allowing to go to the next step
   public Button rightButton;

   // The section showing the step index and navigation buttons
   public CanvasGroup navigationRowCanvasGroup;

   // When the mouse is over this defined zone, we consider that it hovers the panel
   public RectTransform panelHoveringZone;

   // The text displaying the current step index
   public Text stepText;

   #endregion

   public void initialize (Mode mode, List<Tutorial3> tutorials) {
      gameObject.SetActive(true);
      canvasGroup.Hide();
      npcSpeechText.SetText("");
      expandButtonAnim.SetActive(false);
      rightButtonAnim.SetActive(false);
      tutorialListSection.Hide();
      navigationRowCanvasGroup.Hide();

      _mode = mode;
      refreshPanelConfig();

      rowContainer.DestroyChildren();
      _rows.Clear();
      foreach (Tutorial3 tutorial in tutorials) {
         TutorialRow3 row = Instantiate(rowPrefab, rowContainer.transform, false);
         row.setRowForTutorial(tutorial);
         _rows.Add(row);
      }
   }

   public void refreshTutorialStep (string selectedTutorialKey, string npcSpeech, int currentStepIndex,
      int maxStepIndex, bool isNextStepManual, bool isTutorialCompleted, bool isNextStepAlreadyReached, bool canNextStepBeSkipped) {
      _npcSpeech = npcSpeech;
      _isNavigationRowVisible = false;

      // The navigation buttons are only visible if the tutorial has been completed before
      if (isTutorialCompleted || isNextStepAlreadyReached || canNextStepBeSkipped) {
         rightButton.gameObject.SetActive(true);
      } else {
         rightButton.gameObject.SetActive(false);
      }

      // Select the correct row
      foreach (TutorialRow3 row in _rows) {
         row.refresh(selectedTutorialKey);
      }

      // Type the npc text
      if (canvasGroup.IsShowing()) {
         AutoTyper.SlowlyRevealText(npcSpeechText, _npcSpeech);
      }

      // Set the step index
      stepText.text = "Step " + currentStepIndex + " of " + maxStepIndex;

      // Enable or disable navigation buttons
      if (currentStepIndex == 1) {
         leftButton.interactable = false;
      } else {
         leftButton.interactable = true;
      }

      // In case the last step is manual, we allow clicking on the right button
      if (isNextStepManual) {
         rightButton.gameObject.SetActive(true);
         rightButton.interactable = true;
         rightButtonAnim.SetActive(true);
         _isNavigationRowVisible = true;
      } else {
         rightButtonAnim.SetActive(false);

         // The right button is disabled on the last step
         if (currentStepIndex == maxStepIndex) {
            rightButton.interactable = false;
         } else {
            rightButton.interactable = true;
         }
      }

      // Special case: when the user must click on the 'expand' button, enable an highlight animation
      if (TutorialManager3.self.getCurrentTrigger() == TutorialTrigger.ExpandTutorialPanel) {
         expandButtonAnim.SetActive(true);
         _isNavigationRowVisible = true;
      } else {
         expandButtonAnim.SetActive(false);
      }
   }

   public void Update () {
      bool isCameraFading = CameraManager.defaultCamera != null && CameraManager.defaultCamera.isFading();

      if (Global.player == null || !AreaManager.self.hasArea(Global.player.areaKey) || isCameraFading) {
         if (canvasGroup.IsShowing()) {
            canvasGroup.Hide();
            AutoTyper.FinishText(npcSpeechText);
            _timeSinceWarp = 0;
         }
      } else if (!canvasGroup.IsShowing()){
         // After a warp, we wait a little before showing the tutorial panel again
         _timeSinceWarp += Time.deltaTime;
         if (_timeSinceWarp > DELAY_AFTER_WARP) {
            canvasGroup.Show();
            AutoTyper.SlowlyRevealText(npcSpeechText, _npcSpeech);
            AutoTyper.FinishText(npcSpeechText);
         }
      } else {
         // Fade in or out some sections
         switch (_mode) {
            case Mode.TutorialList:
               if (tutorialListSection.alpha < 1) {
                  tutorialListSection.alpha += FADE_SPEED * Time.deltaTime;
                  tutorialListSection.alpha = Mathf.Clamp(tutorialListSection.alpha, 0, 1);
               }

               if (navigationRowCanvasGroup.alpha < 1) {
                  navigationRowCanvasGroup.alpha = 1;
               }
               break;
            case Mode.NPCSpeech:
               if (tutorialListSection.alpha > 0) {
                  tutorialListSection.alpha -= FADE_SPEED * Time.deltaTime;
                  tutorialListSection.alpha = Mathf.Clamp(tutorialListSection.alpha, 0, 1);
               }

               // In NPC Speech mode, display the navigation row only if the mouse is over the panel
               if (_isNavigationRowVisible
                  || RectTransformUtility.RectangleContainsScreenPoint(panelHoveringZone, MouseUtils.mousePosition)) {
                  navigationRowCanvasGroup.interactable = true;
                  navigationRowCanvasGroup.blocksRaycasts = true;
                  navigationRowCanvasGroup.alpha += FADE_SPEED * Time.deltaTime;
                  navigationRowCanvasGroup.alpha = Mathf.Clamp(navigationRowCanvasGroup.alpha, 0, 1);
               } else {
                  navigationRowCanvasGroup.alpha -= FADE_SPEED * Time.deltaTime;
                  navigationRowCanvasGroup.alpha = Mathf.Clamp(navigationRowCanvasGroup.alpha, 0, 1);
               }
               break;
            default:
               break;
         }

         // If the user clicks on the NPC face or text, fully write the text right away
         if (KeyUtils.GetButtonDown(MouseButton.Left)
            && RectTransformUtility.RectangleContainsScreenPoint(panelHoveringZone, MouseUtils.mousePosition)) {
            AutoTyper.FinishText(npcSpeechText);
         }
      }
   }

   public void onTutorialRowPressed (TutorialRow3 selectedRow) {
      TutorialManager3.self.selectTutorial(selectedRow.tutorial);

      // Automatically hide the list section
      onShrinkButtonPressed();
   }

   public void onExpandButtonPressed () {
      switch (_mode) {
         case Mode.NPCSpeech:
            _mode = Mode.TutorialList;
            refreshPanelConfig();
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.ExpandTutorialPanel);
            break;
         default:
            break;
      }
   }

   public void onShrinkButtonPressed () {
      switch (_mode) {
         case Mode.TutorialList:
            _mode = Mode.NPCSpeech;
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

      // Automatically hide the list section
      onShrinkButtonPressed();
   }

   public void onPreviousStepButtonPressed () {
      TutorialManager3.self.previousStep();
   }

   public void onEnterBattle () {
      if (canvasGroup.IsShowing()) {
         canvasGroup.Hide();
         AutoTyper.FinishText(npcSpeechText);
         _timeSinceWarp = 0;
      }
   }

   public void confirmClosePanel () {
      _mode = Mode.Closed;
      refreshPanelConfig();
   }

   public void openPanel () {
      //SoundManager.play2DClip(SoundManager.Type.Tutorial_Pop_Up);

      _mode = Mode.NPCSpeech;
      refreshPanelConfig();
      TutorialManager3.self.updateArrow();
      AutoTyper.SlowlyRevealText(npcSpeechText, _npcSpeech);
   }

   public Mode getMode () {
      return _mode;
   }

   private void refreshPanelConfig () {
      if (!gameObject.activeSelf) {
         gameObject.SetActive(true);
      }

      // Some sections are shown or hidden in the update
      switch (_mode) {
         case Mode.TutorialList:
            tutorialListSection.interactable = true;
            tutorialListSection.blocksRaycasts = true;
            navigationRowButtonCanvasGroup.Hide();
            navigationRowCanvasGroup.interactable = true;
            navigationRowCanvasGroup.blocksRaycasts = true;
            break;
         case Mode.Closed:
            gameObject.SetActive(false);
            break;
         case Mode.NPCSpeech:
         default:
            tutorialListSection.interactable = false;
            tutorialListSection.blocksRaycasts = false;
            navigationRowButtonCanvasGroup.Show();
            navigationRowCanvasGroup.interactable = false;
            navigationRowCanvasGroup.blocksRaycasts = false;
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

   // The time passed since the player warped to another area
   private float _timeSinceWarp = 0;

   // Gets set to true when the row showing the step index and navigation buttons must always be visible
   private bool _isNavigationRowVisible = false;

   #endregion
}