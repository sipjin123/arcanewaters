using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class NewTutorialDetailPanel : Panel {
   #region Public Variables

   #endregion

   public override void Awake () {
      base.Awake();

      _exitButton.onClick.AddListener(() => {
         hide();
      });

      _backButton.onClick.AddListener(() => {
         Global.player.Cmd_SpawnInNewMap(Area.STARTING_TOWN);
         hide();
      });
   }

   public void showNewTutorialDetailPanel (TutorialViewModel tutorial) {
      _tutorialName.SetText(tutorial.tutorialName);
      _tutorialDescription.SetText(tutorial.tutorialDescription);

      try {
         _tutorialImage.sprite = ImageManager.getSprite(tutorial.tutorialImgUrl);
      } catch {
         _tutorialImage.sprite = ImageManager.self.blankSprite;
      }

      _contentParentTransform.gameObject.DestroyChildren();

      foreach (TutorialStepViewModel step in tutorial.tutorialSteps) {
         NewTutorialStepRow row = Instantiate(_rowPrefab, _contentParentTransform);
         row.titleText.SetText(step.stepName);
         row.descriptionText.SetText(step.stepDescription);
         row.actionText.SetText(step.actionDescription);
         row.isCompleteToggle.SetIsOnWithoutNotify(step.isCompleted);

         if (step.isCompleted) {
            row.completedTimestampLabel.gameObject.SetActive(true);
            row.completedTimestampValue.SetText(step.completedTimestamp.ToString("MM/dd/yyyy hh:mm tt"));
            row.completedTimestampValue.gameObject.SetActive(true);
         } else {
            row.completedTimestampLabel.gameObject.SetActive(false);
            row.completedTimestampValue.gameObject.SetActive(false);
         }

         row.gameObject.SetActive(true);
      }

      base.show();
   }

   #region Private Variables

   // The parent transform for step rows
   [SerializeField]
   private Transform _contentParentTransform;

   // The exit panel button reference
   [SerializeField]
   private Button _exitButton;

   // The row prefab reference
   [SerializeField]
   private NewTutorialStepRow _rowPrefab;

   // The tutorial name text reference
   [SerializeField]
   private TextMeshProUGUI _tutorialName;

   // The tutorial description text reference
   [SerializeField]
   private TextMeshProUGUI _tutorialDescription;

   // The tutorial image reference
   [SerializeField]
   private Image _tutorialImage;

   // The back button reference
   [SerializeField]
   private Button _backButton;

   #endregion
}
