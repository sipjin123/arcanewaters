using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using System;
using UnityEngine.InputSystem;

public class Discovery : NetworkBehaviour, IObserver
{
   #region Public Variables

   // The max valid distance between the player and a discovery
   public const float MAX_VALID_DISTANCE = 5.0f;

   // The discovery data
   [SyncVar]
   public DiscoveryData data;

   // A unique ID assigned by map editor, unique for all prefabs placed in a single area
   [SyncVar]
   public int placedDiscoveryId;

   // User IDS of the users that have discovered this discovery
   public readonly SyncHashSet<int> discoveredUsersIds = new SyncHashSet<int>();

   // The instance ID of the area this discovery belongs to
   [SyncVar]
   public int instanceId;

   // The animator for the mist
   public Animator mistAnimator;

   // The animator for the spinning icon
   public Animator spinningIconAnimator;

   #endregion

   private void Awake () {
      if (Util.isBatchServer()) {
         enabled = false;
      }
   }

   private void Start () {
      Minimap.self.addDiscoveryIcon(this);
   }

   private void OnDestroy () {
      Minimap.self.deleteDiscoveryIcon(this);
   }

   public override void OnStartClient () {
      initializeDiscovery();

      if (DiscoveryManager.self.revealedDiscoveriesClient.Contains((instanceId, placedDiscoveryId))) {
         reveal();
      }
   }

   private void Update () {
      // Only run on client
      if (!NetworkClient.active) {
         return;
      }

      if (!_isRevealed && _isLocalPlayerInside) {
         _currentProgress += Time.deltaTime;

         // Get a normalized (0-1) value of the time the player spent in the trigger
         _progressBarSlider.value = Mathf.InverseLerp(0, EXPLORE_DISCOVERY_TIME, _currentProgress);

         if (_currentProgress >= EXPLORE_DISCOVERY_TIME && !_isWaitingForRequestResponse) {
            _isWaitingForRequestResponse = true;
            Global.player.rpc.Cmd_FoundDiscovery(placedDiscoveryId);
            Minimap.self.deleteDiscoveryIcon(this);
         }
      }

      bool isMouseOver = MouseManager.self.isHoveringOver(_clickableBox);

      // Show the outline if the player is in the trigger area or the mouse is over
      _outline.setVisibility((_isLocalPlayerInside || isMouseOver) && _isRevealed);

      if ((_isLocalPlayerInside || isMouseOver) && _isRevealed) {
         if (InputManager.self.inputMaster.UIControl.Equip.WasPressedThisFrame() || (KeyUtils.GetButtonUp(MouseButton.Left) && isMouseOver)) {
            openDiscoveryPanel();
         }
      }
   }

   private void openDiscoveryPanel () {
      // Make sure the player is close enough
      if (!_isLocalPlayerInside) {
         FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, .24f)).asTooFar();
         return;
      }

      DiscoveryPanel panel = PanelManager.self.get(Panel.Type.Discovery) as DiscoveryPanel;

      if (!panel.isShowing()) {
         panel.showDiscovery(data);
      }
   }

   private void initializeDiscovery () {
      transform.position = _discoveryPosition;

      _spriteAnimation = GetComponent<SpriteAnimation>();
      _spriteRenderer = GetComponent<SpriteRenderer>();
      _outline = GetComponent<SpriteOutline>();

      // Hide the discovery sprite and outline until the discovery is revealed
      _spriteRenderer.enabled = false;
      _outline.setVisibility(false);

      // Adjust the size of the trigger
      _triggerCollider.offset = Vector2.zero;
      _triggerCollider.radius = Mathf.Max(_spriteRenderer.size.x, _spriteRenderer.size.y) / 2 + _triggerExtraRadius;

      _progressBarSlider.value = 0;
      _canvas.worldCamera = Camera.main;
      _progressBarSlider.gameObject.SetActive(false);
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      if (Global.player != null && collision.GetComponent<NetEntity>() == Global.player) {
         _isLocalPlayerInside = true;
         if (!_isRevealed) {
            spinningIconAnimator.SetBool("PlayerInside", true);
            _progressBarSlider.value = 0;
            _currentProgress = 0;
            _progressBarSlider.gameObject.SetActive(true);
         }
      }
   }

   private void OnTriggerExit2D (Collider2D collision) {
      if (_isLocalPlayerInside && Global.player != null && collision.GetComponent<NetEntity>() == Global.player) {
         _isLocalPlayerInside = false;
         _progressBarSlider.gameObject.SetActive(false);

         if (!_isRevealed) {
            spinningIconAnimator.SetBool("PlayerInside", false);
         }
      }
   }

   [TargetRpc]
   public void Target_RevealDiscovery (NetworkConnection connection) {
      if (!DiscoveryManager.self.revealedDiscoveriesClient.Contains((instanceId, placedDiscoveryId))) {
         DiscoveryManager.self.revealedDiscoveriesClient.Add((instanceId, placedDiscoveryId));
      }

      reveal();
   }

   [Server]
   public void assignDiscoveryAndPosition (DiscoveryData fetchedData, Vector3 position) {
      data = new DiscoveryData(fetchedData.name, fetchedData.description, fetchedData.discoveryId, fetchedData.spriteUrl, fetchedData.rarity);
      _discoveryPosition = position;
   }

   private void reveal () {
      _isRevealed = true;
      _spriteRenderer.enabled = true;
      _spriteAnimation.sprites = ImageManager.getSprites(data.spriteUrl);
      _spriteAnimation.startPlaying();
      mistAnimator.SetTrigger("Revealed");
      spinningIconAnimator.gameObject.SetActive(false);
      _progressBarSlider.gameObject.SetActive(false);
   }

   public int getXPValue () {
      // Calculate an XP value based on the rarity of the discovery
      return Mathf.Clamp(BASE_EXPLORER_XP * (int) data.rarity, BASE_EXPLORER_XP, MAX_EXPLORER_XP);
   }

   public int getInstanceId () {
      return instanceId;
   }

   #region Private Variables

   // The sprite animation
   private SpriteAnimation _spriteAnimation = default;

   // The sprite renderer
   private SpriteRenderer _spriteRenderer = default;

   // The sprite outline
   private SpriteOutline _outline = default;

   // How much progress has the player done exploring this discovery
   private float _currentProgress = default;

   // True when the local player is within the trigger
   private bool _isLocalPlayerInside = default;

   // The world space canvas with the progress bar
   [SerializeField]
   private Canvas _canvas = default;

   // The progress bar
   [SerializeField]
   private Slider _progressBarSlider = default;

   // The trigger collider
   [SerializeField]
   private CircleCollider2D _triggerCollider = default;

   // The clickable box
   [SerializeField]
   private ClickableBox _clickableBox = default;

   // How bigger than the sprite is the collider
   [SerializeField]
   private float _triggerExtraRadius = 0.1f;

   // True when the discovery is explored by the local player
   private bool _isRevealed = false;

   // True when we're waiting for the server to reply to our reveal request
   private bool _isWaitingForRequestResponse = false;

   // The position of this discovery
   [SyncVar]
   private Vector3 _discoveryPosition = default;

   // How much time (in seconds) it takes to explore a discovery
   private const float EXPLORE_DISCOVERY_TIME = 3.0f;

   // The base explorer XP gained for exploring any discovery
   private const int BASE_EXPLORER_XP = 5;

   // The maximum explorer experience a player can get for finding a discovery
   private const int MAX_EXPLORER_XP = 25;

   #endregion
}
