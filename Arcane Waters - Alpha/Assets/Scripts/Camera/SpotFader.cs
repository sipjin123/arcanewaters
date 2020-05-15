using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Cinemachine;
using DG.Tweening;

public class SpotFader : ClientMonoBehaviour {
   #region Public Variables

   // The background image
   public Image backgroundImage;

   // The spot mask image
   public Image spotMask;

   // Self
   public static SpotFader self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
      _canvasGroup = GetComponent<CanvasGroup>();
   }

   private void Start () {
      disableCanvasGroup();

      MAX_SPOT_SIZE = new Vector2(Screen.width * MAX_SIZE_MULTIPLIER, Screen.width * MAX_SIZE_MULTIPLIER);
      spotMask.rectTransform.sizeDelta = MAX_SPOT_SIZE;
   }

   public void fadeBackgroundColor (Color color, float time = -1) {
      if (self == null) {
         return;
      }

      _currentColorTween?.Kill();
      _currentColorTween = backgroundImage.DOColor(color, time >= 0 ? time : _totalEffectTime * 0.5f);
   }

   public void doCircleFade (Vector3 worldPosition, Color color, bool fadeColor = false, float fadeColorTime = -1, bool useGlobalPlayerPositionForFadeIn = false) {
      if (self == null) {
         return;
      }

      if (fadeColor) {
         fadeBackgroundColor(color, fadeColorTime);
      } else {
         setColor(color);
      }

      StartCoroutine(CO_doCircleFade(worldPosition, useGlobalPlayerPositionForFadeIn));
   }

   public void doCircleFade (Color color) {
      // NetEntity calls this method OnDestroy, which is fine most of the times, except when quitting the game/exiting play mode. That causes a NullReferenceException.
      if (self == null) {
         return;
      }

      setColor(color);
      StartCoroutine(CO_doCircleFade());
   }

   public void doCircleFade () {
      doCircleFade(_defaultColor);
   }

   public void openSpotToMaxSize () {
      if (self == null) {
         return;
      }

      _currentSizeTween?.Kill();
      setFadeInitialValues();
      _currentSizeTween = spotMask.rectTransform.DOSizeDelta(MAX_SPOT_SIZE, _totalEffectTime * 0.5f).OnComplete(() => {
         disableCanvasGroup();
      });
   }

   public void closeSpotTowardsPosition (Vector3 worldPosition) {
      if (self == null) {
         return;
      }

      _currentSizeTween?.Kill();
      spotMask.rectTransform.sizeDelta = MAX_SPOT_SIZE;
      spotMask.rectTransform.position = Camera.main.WorldToScreenPoint(worldPosition);
      setFadeInitialValues();
      _currentSizeTween = spotMask.rectTransform.DOSizeDelta(_highlightPlayerSpotSize, _totalEffectTime * 0.5f);
   }

   public void openSpotAtPosition (Vector3 worldPosition) {
      if (self == null) {
         return;
      }

      _currentSizeTween?.Kill(); 
      setFadeInitialValues();
      spotMask.rectTransform.sizeDelta = Vector2.zero;
      spotMask.transform.position = Camera.main.WorldToScreenPoint(worldPosition);
      _currentSizeTween = spotMask.rectTransform.DOSizeDelta(_highlightPlayerSpotSize, _totalEffectTime * 0.5f);
   }

   public void setColor (Color color) {
      if (self == null) {
         return;
      }

      _currentColorTween?.Kill();
      backgroundImage.color = color;
   }

   private void setFadeInitialValues () {
      MAX_SPOT_SIZE = new Vector2(Screen.width * MAX_SIZE_MULTIPLIER, Screen.width * MAX_SIZE_MULTIPLIER);

      _startTime = Time.time;
      _startingCamera = getActiveCamera();
      _canvasGroup.blocksRaycasts = true;
      _canvasGroup.alpha = 1;
   }

   private void disableCanvasGroup () {
      _canvasGroup.blocksRaycasts = false;
      _canvasGroup.interactable = false;
      _canvasGroup.alpha = 0;
   }

   private IEnumerator CO_doCircleFade () {
      _currentSizeTween?.Kill();

      float effectTime = _totalEffectTime * 0.5f;

      setFadeInitialValues();

      spotMask.transform.position = Camera.main.WorldToScreenPoint(Global.player.transform.position);
      _currentSizeTween = spotMask.rectTransform.DOSizeDelta(Vector2.zero, effectTime);

      while ((_currentSizeTween != null && _currentSizeTween.active && !hasCameraChanged()) || Time.time - _startTime < TIMEOUT_DURATION) {
         yield return null;
      }

      while(Global.player == null) {
         yield return null;
      }

      spotMask.transform.position = Camera.main.WorldToScreenPoint(Global.player.transform.position);

      _currentSizeTween = spotMask.rectTransform.DOSizeDelta(MAX_SPOT_SIZE, effectTime).OnComplete(() => {
         disableCanvasGroup();
      });
   }

   private IEnumerator CO_doCircleFade (Vector3 worldPosition, bool useGlobalPlayerPositionForFadeIn = false) {
      _currentSizeTween?.Kill();

      float effectTime = _totalEffectTime * 0.5f;

      setFadeInitialValues();

      spotMask.transform.position = Camera.main.WorldToScreenPoint(worldPosition);
      _currentSizeTween = spotMask.rectTransform.DOSizeDelta(Vector2.zero, effectTime);

      while ((_currentSizeTween != null && _currentSizeTween.active && !hasCameraChanged()) || Time.time - _startTime < TIMEOUT_DURATION) {
         yield return null;
      }

      if (useGlobalPlayerPositionForFadeIn) {
         while (Global.player == null) {
            yield return null;
         }

         // We need to wait two frames until the camera is moved to the player's position so WorldToScreenPoint returns an accurate position
         yield return null;
         yield return null;

         spotMask.transform.position = Camera.main.WorldToScreenPoint(Global.player.transform.position);
      }

      _currentSizeTween = spotMask.rectTransform.DOSizeDelta(MAX_SPOT_SIZE, effectTime).OnComplete(() => {
         disableCanvasGroup();
      });
   }

   protected bool hasCameraChanged () {
      if (Global.player == null || !AreaManager.self.hasArea(Global.player.areaKey)) {
         return false;
      }

      return _startingCamera != getActiveCamera();
   }

   protected ICinemachineCamera getActiveCamera () {
      if (Camera.main == null) {
         return null;
      }

      return Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera;
   }

   private void OnDestroy () {
      _currentColorTween?.Kill();
      _currentSizeTween?.Kill();
      StopAllCoroutines();
   }

   #region Private Variables

   // The default spot size, enough to highlight a player
   [SerializeField]
   private Vector2 _highlightPlayerSpotSize = new Vector2(400, 400);

   // The default color of the background image
   [SerializeField]
   private Color _defaultColor = Color.black;

   // The total time to complete the effect
   [SerializeField]
   private float _totalEffectTime = 0.5f;

   // The tween that's changing the spot size
   private Tween _currentSizeTween;

   // The tween that's changing the color
   private Tween _currentColorTween;

   // The canvas group
   private CanvasGroup _canvasGroup;

   // The time at which the effect start
   protected float _startTime;

   // The camera that was active initially
   protected ICinemachineCamera _startingCamera;

   // The amount of time we wait for an area change, before giving up
   protected static float TIMEOUT_DURATION = 5f;

   // The max spot size, enough to go become invisible 
   private static Vector2 MAX_SPOT_SIZE;

   // The max size multiplier 
   private const float MAX_SIZE_MULTIPLIER = 3.0f;

   #endregion
}
