using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TutorialPanel : MonoBehaviour {
   #region Public Variables

   // The text that shows our step
   public Text titleText;

   // The text that shows the instructions
   public Text detailsText;

   // The container for our instructions
   public GameObject instructionsContainer;

   // The button we use for toggling the instructions
   public Button toggleButton;

   // The expand button sprite
   public Sprite expandSprite;

   // The minimize button sprite
   public Sprite minimizeSprite;

   // Self
   public static TutorialPanel self;

   // The maximum step
   public static int MAX_STEP = 10;

   #endregion

   void Awake () {
      self = this;

      // Look up components
      _canvasGroup = GetComponent<CanvasGroup>();
   }

   private void Update () {
      // Hide the tutorial panel if there's nothing to show
      _canvasGroup.alpha = Util.isEmpty(titleText.text) || GrampsManager.isShowing() ? 0f : 1f;
   }

   public void updatePanel (List<TutorialInfo> tutorialInfo) {
      // Look up the Step that they're currently on and display the relevant text
      TutorialStep tutorialStep = TutorialManager.getCurrentTutorialStepData();
      titleText.text = tutorialStep.titleText;
      detailsText.text = tutorialStep.detailsText;
   }

   public void toggleInstructions () {
      instructionsContainer.SetActive(!instructionsContainer.activeSelf);

      // Update the button image based on whether the instructions are showing
      toggleButton.image.sprite = instructionsContainer.activeSelf ? minimizeSprite : expandSprite;
   }

   #region Private Variables

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   #endregion
}
