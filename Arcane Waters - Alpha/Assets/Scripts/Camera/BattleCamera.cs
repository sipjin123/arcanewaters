using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BattleCamera : BaseCamera {
   #region Public Variables

   // Self
   public static BattleCamera self;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   protected override void Start () {
      base.Start();
      onResolutionChanged();
   }

   public override void onResolutionChanged () {
      base.onResolutionChanged();

      if (_vcam != null) {
         _cam.orthographicSize = _vcam.m_Lens.OrthographicSize;
      }

      // Save the original values
      _originalSize = _cam.orthographicSize;
      _originalPosition = _cam.transform.position;
   }

   public void focusOnPosition (Vector3 targetPos, float time) {
      // TODO: Confirm if this function will now be deleted, only used on melee pre attack
      /*
      _focusSequence?.Kill();
      _focusSequence = DOTween.Sequence();

      // Make sure the time is valid 
      if (time < 0) {
         time = _defaultTransitionTime;
      }

      // Apply the offset
      targetPos += _positionOffsetOnFocus;

      // Keep the camera's original Z position
      targetPos.z = _cam.transform.position.z;

      _focusSequence.Join(_cam.transform.DOMove(targetPos, time));
      _focusSequence.Join(_cam.DOOrthoSize(_originalSize * _sizeScaleOnFocus, time));

      // Update the position of the bars so they don't cover the player
      _focusSequence.OnUpdate(() => {
         BattleUIManager.self.updatePlayerUIPositions();
      });

      _focusSequence.OnComplete(() => {
         BattleUIManager.self.updatePlayerUIPositions();
      });

      _focusSequence.Play();*/
   }

   public void returnToOriginalSettings (float time) {
      // TODO: Confirm if this function will now be deleted,  only used on melee after attack
      /*
      _focusSequence?.Kill();
      _focusSequence = DOTween.Sequence();

      if (time < 0) {
         time = _defaultTransitionTime;
      }

      _focusSequence.Join(_cam.transform.DOMove(_originalPosition, time));
      _focusSequence.Join(_cam.DOOrthoSize(_originalSize, time));

      // Update the position of the bars so they don't cover the player
      _focusSequence.OnUpdate(() => {
         BattleUIManager.self.updatePlayerUIPositions();
      });

      _focusSequence.OnComplete(() => {
         BattleUIManager.self.updatePlayerUIPositions();
      });

      _focusSequence.Play();*/
   }

   #region Private Variables

   // The relative size when focusing on an attack
   [SerializeField]
   private float _sizeScaleOnFocus = 0.75f;

   // The default animation time if an invalid time is provided
   [SerializeField]
   private float _defaultTransitionTime = 0.25f;

   // An offset to apply to the camera position when focusing on an attack
   [SerializeField]
   private Vector3 _positionOffsetOnFocus = new Vector3(0, 0.25f, 0);

   // The sequence moving and resizing the camera
   private Sequence _focusSequence;

   // The original size of the camera
   private float _originalSize;

   // The original position of the camera
   private Vector3 _originalPosition;

   #endregion
}
