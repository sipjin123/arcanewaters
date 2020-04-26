using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class NewTutorialPanel : Panel {
   #region Public Variables

   #endregion

   public override void Awake () {
      base.Awake();

      _exitButton.onClick.AddListener(() => {
         hide();
      });
   }

   public void showNewTutorialPanel (List<TutorialViewModel> tutorials) {
      _contentParentTransform.gameObject.DestroyChildren();

      foreach (TutorialViewModel tutorial in tutorials) {
         NewTutorialRow row = Instantiate(_rowPrefab, _contentParentTransform);
         row.titleText.SetText(tutorial.tutorialName);
         row.descriptionText.SetText(tutorial.tutorialDescription);

         try {
            row.image.sprite = ImageManager.getSprite(tutorial.tutorialImgUrl);
         } catch {
            row.image.sprite = ImageManager.self.blankSprite;
         }

         row.warpButton.onClick.AddListener(() => {
            Global.player.Cmd_SpawnInNewMap(tutorial.tutorialAreaKey, string.Empty, Direction.South);
            hide();
         });

         row.gameObject.SetActive(true);
      }

      base.show();
   }

   #region Private Variables

   // The parent transform for tutorial rows
   [SerializeField]
   private Transform _contentParentTransform;

   // The exit panel button reference
   [SerializeField]
   private Button _exitButton;

   // The row prefab reference
   [SerializeField]
   private NewTutorialRow _rowPrefab;

   #endregion
}
