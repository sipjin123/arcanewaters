using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using DG.Tweening;

public class StepCompletedNotificationPanel : Panel {
   #region Public Variables

   #endregion

   public override void Awake () {
      base.Awake();

      _exitButton.onClick.AddListener(() => {
         hide();
      });

      _tweenSequence = DOTween.Sequence();
   }

   public void showStepCompletedNotification (string stepTitle, string stepImagePath) {
      _tweenSequence?.Kill();
      _stepTitleText.SetText(stepTitle);

      try {
         _stepImage.sprite = ImageManager.getSprite(stepImagePath);
      } catch {
         _stepImage.sprite = ImageManager.self.blankSprite;
      }

      _notificationCanvas.gameObject.SetActive(true);

      _tweenSequence = DOTween.Sequence();

      _tweenSequence.Append(_notificationCanvas.DOFade(1, _fadeDuration))
         .AppendInterval(_animationDelay)
         .Append(_notificationCanvas.DOFade(0, _fadeDuration))
         .OnComplete(() => {
            _notificationCanvas.gameObject.SetActive(false);
         });
   }

   #region Private Variables

   [SerializeField]
   private CanvasGroup _notificationCanvas;

   // The step title text
   [SerializeField]
   private TextMeshProUGUI _stepTitleText;

   // The step image
   [SerializeField]
   private Image _stepImage;

   // The exit button
   [SerializeField]
   private Button _exitButton;

   // The animation speed
   [SerializeField]
   private float _fadeDuration = 0.25f;

   // Wait time after the notification is hidden
   [SerializeField]
   private float _animationDelay = 2.0f;

   // The animation sequence
   private Sequence _tweenSequence;

   // The initial animation position
   private Vector3 _initialPosition;

   #endregion
}
