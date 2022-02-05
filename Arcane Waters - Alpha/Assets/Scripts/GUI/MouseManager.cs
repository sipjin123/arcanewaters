using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using System.Linq;

public class MouseManager : ClientMonoBehaviour
{
   #region Public Variables

   // Arrow
   public Texture2D defaultCursorTexture;
   public Texture2D defaultCursorLeftSlowTexture;
   public Texture2D defaultCursorRightSlowTexture;
   public Texture2D defaultCursorLeftFastTexture;
   public Texture2D defaultCursorRightFastTexture;
   public Texture2D pressedCursorTexture;

   // Hand
   public Texture2D defaultHandTexture;
   public Texture2D pressedHandTexture;

   // Magnifying glass
   public Texture2D defaultMagnifyingGlassTexture;
   public Texture2D pressedMagnifyingGlassTexture;

   // Bubble
   public Texture2D defaultBubbleTexture;
   public Texture2D pressedBubbleTexture;

   // Caret
   public Texture2D defaultCaretTexture;
   public Texture2D pressedCaretTexture;

   // Forbidden
   public Texture2D defaultForbiddenTexture;
   public Texture2D pressedForbiddenTexture;

   public CursorMode cursorMode = CursorMode.Auto;
   public Vector2 normalHotSpot = Vector2.zero;
   public Vector2 handHotSpot = Vector2.zero;
   public Vector2 caretHotSpot = Vector2.zero;

   // The mouse is considered to be moving only if the mouse movement goes over the threshold
   public float movementThreshold = 1.0f;

   // If the cursor has been moving for more than these amount of frames, accelerate
   public int accelThresholdFramesCount = 3;

   [Header("Shimmer")]
   // Is the shimmer enabled?
   public bool isShimmerEnabled = true;

   // Shimmer textures
   public Texture2D[] shimmerFrames;

   // Shimmer delay
   public float shimmerDelaySeconds = 10.0f;

   // Shimmer frame duration
   public float shimmerFrameDurationSeconds = 0.16f;

   [Header("Trail")]
   // Is the trail enabled?
   public bool isTrailEnabled = true;

   // Trail particle speed
   public float trailParticleSpeed = 5.0f;

   // Trail offset
   public Vector2 trailOffset = new Vector2(.0f, .0f);

   // Reference to the canvas to draw the particles to
   public Canvas trailCanvas;

   // The prefab used to instantiate particles
   public GameObject trailParticlePrefab;

   // The time between particle spawn events when the cursor is moving fast
   public float fastParticleSpawnIntervalSeconds;

   // The time between particle spawn events when the cursor is moving slow
   public float slowParticleSpawnIntervalSeconds;

   [Header("Click Effect")]
   // Is the click effect enabled?
   public bool isClickEffectEnabled = true;

   // Click effect sprites
   public Sprite[] clickEffectSprites;

   // Reference to the image control that will display the click effect
   public Image clickEffectImage;

   // Click effect delay
   public float clickEffectDelaySeconds = .01f;

   // Click effect frame duration
   public float clickEffectFrameDurationSeconds = .16f;

   [Header("Cursor Type")]
   // The current cursor type
   public CursorTypes cursorType;

   // The cursor types
   public enum CursorTypes
   {
      // None
      None = 0,

      // Arrow
      Arrow = 1,

      // Bubble
      Bubble = 2,

      // Caret
      Caret = 3,

      // Forbidden
      Forbidden = 4,

      // Hand
      Hand = 5,

      // Magnifying Glass
      MagnifyingGlass = 6
   }

   // Self
   public static MouseManager self;

   // Pointer event for mouse hover
   public PointerEventData pointerEventData;

   // Reference to the canvas controls to monitor
   public Canvas[] uiCanvases;

   #endregion

   protected override void Awake () {
      D.adminLog("MouseManager.Awake...", D.ADMIN_LOG_TYPE.Initialization);
      base.Awake();
      self = this;
      D.adminLog("MouseManager.Awake: OK", D.ADMIN_LOG_TYPE.Initialization);
   }

   public void Start () {
      InvokeRepeating(nameof(identifyHoveredBox), 0, .15f);
   }

   private void Update () {
      if (Util.isBatch()) {
         return;
      }

      updatePressedState();
      updateHoveredObject();
      tryNotifyHoveredBox();
      updateMouseMovementStatus();
      updateCursorTexture();
      updateMovementInfo();
      processShimmer();
      processMouseTrail();
      processClickEffect();
   }

   public void updatePressedState () {
      _prevIsPressing = _isPressing;
      _isPressing = KeyUtils.GetButton(MouseButton.Left) || KeyUtils.GetButton(MouseButton.Right);
   }

   public void updateHoveredObject () {
      // Check if the mouse is over interactable objects
      _isOverInteractableObject = false;
      _prevBoxBeingHovered = _boxBeingHovered;
      _boxBeingHovered = null;
      GameObject gameObjectUnderMouse = null;
      List<GameObject> gameObjectsUnderMouseList = new List<GameObject>();

      pointerEventData = new PointerEventData(EventSystem.current);
      pointerEventData.position = MouseUtils.mousePosition;

      // Create a list of Raycast Results
      List<RaycastResult> results = new List<RaycastResult>();
      EventSystem.current.RaycastAll(pointerEventData, results);

      // Search for clickable box
      foreach (RaycastResult result in results) {
         if (result.gameObject.GetComponent<ClickableBox>()) {
            gameObjectsUnderMouseList.Add(result.gameObject);
         }
      }

      foreach (GameObject gameObject in gameObjectsUnderMouseList) {
         if (gameObjectUnderMouse == null || gameObject.transform.position.z < gameObjectUnderMouse.transform.position.z) {
            gameObjectUnderMouse = gameObject;
         }
      }

      if (gameObjectUnderMouse == null) {
         return;
      }

      // Only consider clickable boxes if no context menu is opened
      if (PanelManager.self.contextMenuPanel != null && PanelManager.self.contextMenuPanel.isShowing()) {
         return;
      }

      // Only consider clickable boxes if no panel is opened
      if (!PanelManager.self.hasPanelInLinkedList()) {
         // Check if we're hovering over a clickable box
         _boxBeingHovered = gameObjectUnderMouse.GetComponent<ClickableBox>();

         if (_boxBeingHovered != null) {
            _isOverInteractableObject = true;
            return;
         }
      }

      // Otherwise, check if we're  hoving over a selectable, toggle or slider
      Selectable selectable = gameObjectUnderMouse.GetComponent<Selectable>();
      Toggle toggle = gameObjectUnderMouse.GetComponentInParent<Toggle>();
      Slider slider = gameObjectUnderMouse.GetComponentInParent<Slider>();

      if (selectable != null || toggle != null || slider != null) {
         _isOverInteractableObject = true;
         return;
      }

      return;
   }

   private void tryNotifyHoveredBox () {
      // Let the box know if it's been clicked
      if (_boxBeingHovered != null) {
         if (KeyUtils.GetButtonDown(MouseButton.Left)) {
            _boxBeingHovered.onMouseButtonDown(MouseButton.Left);
         } else if (KeyUtils.GetButtonUp(MouseButton.Left)) {
            _boxBeingHovered.onMouseButtonUp(MouseButton.Left);
         }

         if (KeyUtils.GetButtonDown(MouseButton.Right)) {
            _boxBeingHovered.onMouseButtonDown(MouseButton.Right);
         } else if (KeyUtils.GetButtonUp(MouseButton.Right)) {
            _boxBeingHovered.onMouseButtonUp(MouseButton.Right);
         }
      }
   }

   private void updateMouseMovementStatus () {
      _isMouseMovingThisFrame = isMouseMoving();
   }

   private void updateCursorTexture () {
      Direction? mouseDirection = getMouseMajorMovementDirection();
      if (_isOverForbidden) {
         setForbiddenCursor(_isPressing);
         return;
      }

      if (_isOverTextInput) {
         setCaretCursor(_isPressing);
         return;
      }

      if (_isOverTouchable) {
         setHandCursor(_isPressing);
         return;
      }

      if (_isOverInteractableObject) {
         if (_isOverBookcase) {
            setMagnifyingGlassCursor(_isPressing);
         } else if (_isOverNPC) {
            setBubbleCursor(_isPressing);
         } else {
            setHandCursor(_isPressing);
         }
         return;
      }

      if (_isMouseMovingThisFrame) {
         if (mouseDirection.HasValue && (mouseDirection.Value == Direction.NorthEast)) {
            setArrowRightCursor(isFast: isMovingFast());
         }

         if (mouseDirection.HasValue && (mouseDirection.Value == Direction.SouthWest)) {
            setArrowLeftCursor(isFast: isMovingFast());
         }
      } else {
         if (_idleFrames <= accelThresholdFramesCount) {
            if (mouseDirection.HasValue && (mouseDirection.Value == Direction.NorthEast)) {
               setArrowRightCursor(isFast: false);
            }

            if (mouseDirection.HasValue && (mouseDirection.Value == Direction.SouthWest)) {
               setArrowLeftCursor(isFast: false);
            }
         } else {
            setArrowCursor(_isPressing);
         }
      }
   }

   private bool isMovingFast () {
      return _movingFrames >= accelThresholdFramesCount;
   }

   private void updateMovementInfo () {
      if (_isMouseMovingThisFrame) {
         _idleFrames = 0;
         _movingFrames++;
      } else {
         if (_idleFrames == 0) {
            // Mouse is idle this frame
            _lastIdleTimeSeconds = Time.realtimeSinceStartup;
         }

         _idleFrames++;
         _movingFrames = 0;
      }
   }

   public bool isHoveringOver (ClickableBox box) {
      return _boxBeingHovered == box;
   }

   public Direction? getMouseMajorMovementDirection () {
      Vector2 mouseDelta = MouseUtils.mouseDelta;

      if (Util.areVectorsAlmostTheSame(Vector2.zero, mouseDelta)) {
         return null;
      }

      bool isMovingMostlyHorizontally = Mathf.Abs(mouseDelta.x) > Mathf.Abs(mouseDelta.y);

      if (isMovingMostlyHorizontally) {
         return (MouseUtils.mouseDelta.x > 0) ? Direction.NorthEast : Direction.SouthWest;
      } else {
         return (MouseUtils.mouseDelta.y > 0) ? Direction.NorthEast : Direction.SouthWest;
      }
   }

   public bool isMouseMoving () {
      Vector2 mouseDelta = MouseUtils.mouseDelta;

      if (mouseDelta.sqrMagnitude >= (movementThreshold * movementThreshold)) {
         return true;
      }

      return false;
   }

   public void processShimmer () {
      if (_isMouseMovingThisFrame || shimmerFrames == null || shimmerFrames.Length == 0 || (!_isShimmering && !isShimmerEnabled) || _isPressing) {
         _isShimmering = false;
         return;
      }

      _shimmerCurrentTime += Time.deltaTime;

      if (!_isShimmering) {
         _isShimmering = true;
         _shimmerCurrentTime = 0;
      }

      if (_shimmerCurrentTime >= shimmerDelaySeconds) {
         float time = _shimmerCurrentTime - shimmerDelaySeconds;
         int frameIndex = computeAnimationFrame(shimmerFrames, time, shimmerFrameDurationSeconds);
         Cursor.SetCursor(shimmerFrames[frameIndex], normalHotSpot, CursorMode.Auto);
         _isShimmering = time < computeTotalAnimationDurationSeconds(shimmerFrames.Length, shimmerFrameDurationSeconds);
      }
   }

   public void processMouseTrail () {
      if (!isTrailEnabled || cursorType != CursorTypes.Arrow || _isPressing || !isMouseMoving()) {
         return;
      }

      _trailCurrentTime += Time.deltaTime;
      float particleInterval = isMovingFast() ? fastParticleSpawnIntervalSeconds : slowParticleSpawnIntervalSeconds;

      if (_trailCurrentTime > particleInterval) {
         Vector2 spawnPosition = (MouseUtils.mousePosition + trailOffset) / OptionsManager.self.mainGameCanvas.scaleFactor;
         spawnTrailParticle(spawnPosition, -trailParticleSpeed);
         _lastParticleSpawnTime = Time.realtimeSinceStartup;
         _trailCurrentTime = 0;
      }
   }

   public void processClickEffect () {
      if (clickEffectSprites == null || clickEffectSprites.Length == 0 || (!_isClickEffectVisible && !isClickEffectEnabled)) {
         _isClickEffectVisible = false;
         return;
      }

      _clickEffectCurrentTime += Time.deltaTime;

      if (_isPressing == true && _isPressing != _prevIsPressing) {
         _isClickEffectVisible = true;
         _clickEffectCurrentTime = 0;
      }

      if (!_isClickEffectVisible) {
         return;
      }

      if (_clickEffectCurrentTime >= clickEffectDelaySeconds) {
         float time = _clickEffectCurrentTime - clickEffectDelaySeconds;
         int frameIndex = computeAnimationFrame(clickEffectSprites, time, clickEffectFrameDurationSeconds);
         clickEffectImage.rectTransform.anchoredPosition = (frameIndex == 0 ? MouseUtils.mousePosition / OptionsManager.self.mainGameCanvas.scaleFactor : clickEffectImage.rectTransform.anchoredPosition);
         clickEffectImage.sprite = clickEffectSprites[frameIndex];
         _isClickEffectVisible = time < computeTotalAnimationDurationSeconds(clickEffectSprites.Length, clickEffectFrameDurationSeconds);
      }
   }

   public bool isHoveredObjectOfType<T> () {
      if (_boxBeingHovered == null) {
         return false;
      }

      return (_boxBeingHovered.GetComponentInParent<T>() != null || _boxBeingHovered.GetComponent<T>() != null);
   }

   private float computeTotalAnimationDurationSeconds (int framesCount, float frameDuration) {
      return framesCount * frameDuration;
   }

   private int computeAnimationFrame<T> (T[] textures, float time, float frameDuration) {
      float frameIndex = time / frameDuration;
      return Mathf.FloorToInt(Mathf.Max(0, Mathf.Min(frameIndex, textures.Length - 1)));
   }

   private void identifyHoveredBox () {
      _isOverForbidden = false;
      _isOverNPC = false;
      _isOverBookcase = isHoveredObjectOfType<Bookshelf>();
      _isOverTextInput = false;
      _isOverTouchable = false;

      if (isHoveredObjectOfType<ChairClickable>() && Global.player != null) {
         PlayerBodyEntity body = Global.player.getPlayerBodyEntity();

         if (body != null && (body.isJumping() || body.isEmoting() || body.isSitting())) {
            _isOverForbidden = true;
         }
      }

      if (isHoveredObjectOfType<NPC>() && Global.player != null) {
         _isOverNPC = !Global.player.isInBattle();
      }

      if (Battler.getHoveredBattlers().Count > 0 && Global.player.isInBattle()) {
         _isOverTouchable = true;
      }

      if (EventSystem.current.IsPointerOverGameObject()) {
         foreach (Canvas canvas in uiCanvases) {
            // If the user is browsing a dropdown skip everything, and apply the mouse cursor
            if (EventSystem.current.currentSelectedGameObject != null) {
               Dropdown dropdown = EventSystem.current.currentSelectedGameObject.GetComponentInParent<Dropdown>();
               if (dropdown != null && dropdown.transform.Find("Dropdown List") != null) {
                  _isOverTouchable = true;
                  break;
               }
            }

            _raycastResults = _raycastResults == null ? new List<RaycastResult>() : _raycastResults;
            _raycastResults.Clear();

            var pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = MouseUtils.mousePosition;
            getGraphicRaycaster(canvas).Raycast(pointerData, _raycastResults);

            if (_raycastResults.Count == 0) {
               continue;
            }

            foreach (RaycastResult result in _raycastResults) {
               if (result.gameObject.TryGetComponent(out Selectable selectable)) {
                  if (!_isOverForbidden) {
                     _isOverForbidden = !selectable.interactable;

                     if (_isOverForbidden) {
                        Debug.Log("Is over forbidden");
                        break;
                     }
                  }

                  _isOverTouchable = true;
               }

               if (result.gameObject.TryGetComponent(out TMP_InputField tmpInputField)) {
                  _isOverTextInput = !tmpInputField.readOnly;
               }

               if (result.gameObject.TryGetComponent(out InputField inputField)) {
                  _isOverTextInput = !inputField.readOnly;
               }
            }
         }
      }
   }

   private GraphicRaycaster getGraphicRaycaster (Canvas canvas) {
      _graphicsRaycasters = _graphicsRaycasters == null ? new Dictionary<Canvas, GraphicRaycaster>() : _graphicsRaycasters;

      if (!_graphicsRaycasters.ContainsKey(canvas)) {
         _graphicsRaycasters[canvas] = canvas.GetComponent<GraphicRaycaster>();
      }

      return _graphicsRaycasters[canvas];
   }

   public MouseTrailParticle spawnTrailParticle (Vector2 position, float speed) {
      // Reuse particles
      foreach (MouseTrailParticle p in _particlePool) {
         if (p.hasReachedEnd()) {
            p.setPosition(position);
            p.setSpeed(speed);
            p.restart();
            return p;
         }
      }

      GameObject particle = Instantiate(trailParticlePrefab);
      particle.transform.SetParent(trailCanvas.transform);
      MouseTrailParticle particleComponent = particle.GetComponent<MouseTrailParticle>();
      particleComponent.setPosition(position);
      particleComponent.setSpeed(speed);
      _particlePool.Add(particleComponent);

      return particleComponent;
   }

   #region Cursor setters

   public void setHandCursor (bool pressed = false, CursorMode cursorMode = CursorMode.Auto) {
      Cursor.SetCursor(pressed ? pressedHandTexture : defaultHandTexture, handHotSpot, cursorMode);
      cursorType = CursorTypes.Hand;
   }

   public void setArrowCursor (bool pressed = false, CursorMode cursorMode = CursorMode.Auto) {
      Cursor.SetCursor(pressed ? pressedCursorTexture : defaultCursorTexture, normalHotSpot, cursorMode);
      cursorType = CursorTypes.Arrow;
   }

   public void setArrowRightCursor (bool isFast, CursorMode cursorMode = CursorMode.Auto) {
      Cursor.SetCursor(isFast ? defaultCursorRightFastTexture : defaultCursorRightSlowTexture, normalHotSpot, cursorMode);
      cursorType = CursorTypes.Arrow;
   }

   public void setArrowLeftCursor (bool isFast, CursorMode cursorMode = CursorMode.Auto) {
      Cursor.SetCursor(isFast ? defaultCursorLeftFastTexture : defaultCursorLeftSlowTexture, normalHotSpot, cursorMode);
      cursorType = CursorTypes.Arrow;
   }

   public void setMagnifyingGlassCursor (bool pressed = false, CursorMode cursorMode = CursorMode.Auto) {
      Cursor.SetCursor(pressed ? pressedMagnifyingGlassTexture : defaultMagnifyingGlassTexture, normalHotSpot, cursorMode);
      cursorType = CursorTypes.MagnifyingGlass;
   }

   public void setBubbleCursor (bool pressed = false, CursorMode cursorMode = CursorMode.Auto) {
      Cursor.SetCursor(pressed ? pressedBubbleTexture : defaultBubbleTexture, normalHotSpot, cursorMode);
      cursorType = CursorTypes.Bubble;
   }

   public void setCaretCursor (bool pressed = false, CursorMode cursorMode = CursorMode.Auto) {
      Cursor.SetCursor(pressed ? pressedCaretTexture : defaultCaretTexture, caretHotSpot, cursorMode);
      cursorType = CursorTypes.Caret;
   }

   public void setForbiddenCursor (bool pressed = false, CursorMode cursorMode = CursorMode.Auto) {
      Cursor.SetCursor(pressed ? pressedForbiddenTexture : defaultForbiddenTexture, normalHotSpot, cursorMode);
      cursorType = CursorTypes.Forbidden;
   }

   #endregion

   #region Private Variables

   // The clickable box that we're currently hovering over
   protected ClickableBox _boxBeingHovered = null;

   // The clickable box that we were previously hovering over
   protected ClickableBox _prevBoxBeingHovered = null;

   // Number of frames passed without moving
   private int _idleFrames = 0;

   // Number of frames passed while moving
   private int _movingFrames = 0;

   // Is the mouse hovering something?
   private bool _isOverInteractableObject = false;

   // Is the cursor currently moving?
   private bool _isMouseMovingThisFrame = false;

   // Is the user currently clicking/pressing?
   private bool _isPressing = false;

   // Was the user clicking/pressing in the previous frame?
   private bool _prevIsPressing = false;

   // The last time the mouse was idle
   private float _lastIdleTimeSeconds = 0;

   // Is the cursor currently shimmering?
   private bool _isShimmering = false;

   // Shimmer current time
   private float _shimmerCurrentTime = 0;

   // The spawn time of the last particle
   private float _lastParticleSpawnTime = 0;

   // The current time of the trail
   private float _trailCurrentTime = 0;

   // Is the cursor currently displaying a click effect?
   private bool _isClickEffectVisible = false;

   // The click effect time accumulator
   private float _clickEffectCurrentTime = 0;

   // Is a bookcase being hovered?
   private bool _isOverBookcase = false;

   // Is an NPC being hovered?
   private bool _isOverNPC = false;

   // Is a disabled/forbidden object being hovered?
   private bool _isOverForbidden = false;

   // Is over a touchable control?
   private bool _isOverTouchable = false;

   // Is a textual control being hovered?
   private bool _isOverTextInput = false;

   // Reference to the Raycaster
   private Dictionary<Canvas, GraphicRaycaster> _graphicsRaycasters;

   // Raycast results cache
   private List<RaycastResult> _raycastResults = new List<RaycastResult>();

   // Pool of particles
   private List<MouseTrailParticle> _particlePool = new List<MouseTrailParticle>();

   #endregion
}
