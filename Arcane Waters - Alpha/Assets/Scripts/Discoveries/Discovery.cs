using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using System;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class Discovery : NetworkBehaviour, IObserver
{
   #region Public Variables

   // Types of discovery categories
   public enum Category
   {
      None = 0,
      Arcane = 1,
      Architectural = 2,
      Natural = 3
   }

   // The max valid distance between the player and a discovery
   public const float MAX_VALID_DISTANCE = 5.0f;

   // The discovery data
   [SyncVar]
   public DiscoveryData data;

   // User IDS of the users that have discovered this discovery
   public readonly SyncHashSet<int> discoveredUsersIds = new SyncHashSet<int>();

   // The instance ID of the area this discovery belongs to
   [SyncVar]
   public int instanceId;

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

      if (DiscoveryManager.self != null) {
         DiscoveryManager.self.onDiscoveryDestroyed(this);
      }
   }

   public override void OnStartClient () {
      initializeDiscovery();

      if (DiscoveryManager.self.revealedDiscoveriesClient.Contains(data.discoveryId)) {
         reveal(true);
      }
   }

   private void Update () {
      // Only run on client
      if (!NetworkClient.active) {
         return;
      }

      // Animate visual alphas
      Util.setAlpha(_revealSprite, Mathf.MoveTowards(_revealSprite.color.a, _showRevealVisual ? 1f : 0, Time.deltaTime * 2f));
      foreach (SpriteRenderer ren in _mistRenderers) {
         Util.setAlpha(ren, Mathf.MoveTowards(ren.color.a, _showMist ? 1f : 0, Time.deltaTime * 2f));
      }

      // Animate Slider based on progress
      _revealSliderVisual.transform.localPosition = new Vector3(_isRevealed ? 0 : _currentProgress, 0, 0);

      if (!_isRevealed && _isLocalPlayerInside && !_isWaitingForRequestResponse) {
         if (_hovered && _pointerHeld) {
            // Check if the animation is at the right point
            if (_revealSpriteAnim.getIndex() == revealAnimPressedFrame() - 1 || _revealSpriteAnim.getIndex() == revealAnimPressedFrame()) {
               _revealSpriteAnim.updateIndexMinMax(revealAnimPressedFrame(), revealAnimPressedFrame());
               _revealSpriteAnim.setIndex(revealAnimPressedFrame());
               _revealSpriteAnim.resetAnimation();

               _currentProgress += Time.deltaTime / EXPLORE_DISCOVERY_TIME;

               if (_currentProgress >= 1f && !_isWaitingForRequestResponse) {
                  _isWaitingForRequestResponse = true;
                  Global.player.rpc.Cmd_FoundDiscovery(data.discoveryId);
                  Minimap.self.deleteDiscoveryIcon(this);
               }
            }
         } else {
            // Check if the animation is at the right point
            if (_revealSpriteAnim.getIndex() == revealAnimPressedFrame() - 1 || _revealSpriteAnim.getIndex() == revealAnimPressedFrame()) {
               _revealSpriteAnim.updateIndexMinMax(revealAnimPressedFrame() - 1, revealAnimPressedFrame() - 1);
               _revealSpriteAnim.setIndex(revealAnimPressedFrame() - 1);
               _revealSpriteAnim.resetAnimation();
            }

            _currentProgress = Mathf.Max(_currentProgress - Time.deltaTime * 2f, 0);
         }
      }
   }

   public void onPointerDown () {
      _pointerHeld = true;

      if (_isRevealed) {
         openDiscoveryPanel();
      }
   }

   public void onPointerUp () {
      _pointerHeld = false;
   }

   public void onPointerEnter () {
      _hovered = true;
      _outline.setVisibility(_isRevealed);
   }

   public void onPointerExit () {
      _hovered = false;
      _outline.setVisibility(false);
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
      _discoverySprite.sprite = ImageManager.getSprite(data.spriteUrl);

      // Enable mist
      _showMist = true;
      foreach (SpriteRenderer ren in _mistRenderers) {
         Util.setAlpha(ren, 1f);
      }

      // Disable reveal visual
      _showRevealVisual = false;
      Util.setAlpha(_revealSprite, 0f);

      // Disable reveal sprite
      _revealSprite.transform.localPosition = new Vector3(0, 0, 0);

      // Set reveal visuals based on the category of discovery
      _revealSprite.sprite = ImageManager.getSprite($"Discoveries/Reveal/{ data.category }.png");
      _revealSliderMask.sprite = ImageManager.getSprite($"Discoveries/Reveal/{ data.category }_Mask.png");

      // Set the camera for any world space canvases we have
      foreach (Canvas canvas in GetComponentsInChildren<Canvas>()) {
         if (canvas.renderMode == RenderMode.WorldSpace) {
            canvas.worldCamera = Camera.main;
         }
      }
   }

   private void OnTriggerStay2D (Collider2D collision) {
      if (Global.player != null && collision.GetComponent<NetEntity>() == Global.player) {
         _isLocalPlayerInside = true;
         _showMist = false;
         if (!_isRevealed) {
            if (_showRevealVisual == false) {
               _revealSpriteAnim.updateIndexMinMax(0, revealAnimPressedFrame() - 1);
               _revealSpriteAnim.setIndex(0);
               _revealSpriteAnim.resetAnimation();
            }

            _showRevealVisual = true;
         }
      }
   }

   private void OnTriggerExit2D (Collider2D collision) {
      if (_isLocalPlayerInside && Global.player != null && collision.GetComponent<NetEntity>() == Global.player) {
         _isLocalPlayerInside = false;
         if (!_isRevealed) {
            _showMist = true;
         }
         _showRevealVisual = false;
      }
   }

   [TargetRpc]
   public void Target_RevealDiscovery (NetworkConnection connection) {
      if (!DiscoveryManager.self.revealedDiscoveriesClient.Contains(data.discoveryId)) {
         DiscoveryManager.self.revealedDiscoveriesClient.Add(data.discoveryId);
      }

      reveal();
   }

   [Server]
   public void assignDiscoveryAndPosition (DiscoveryData fetchedData, Vector3 position) {
      data = new DiscoveryData(fetchedData.name, fetchedData.description, fetchedData.discoveryId, fetchedData.spriteUrl, fetchedData.rarity, fetchedData.category);
      _discoveryPosition = position;
   }

   private void reveal (bool instant = false) {
      _isRevealed = true;
      _showMist = false;

      if (instant) {
         _showRevealVisual = false;
      } else {
         _revealSpriteAnim.updateIndexMinMax(revealAnimPressedFrame() + 1, 1000);
         _revealSpriteAnim.setIndex(revealAnimPressedFrame() + 1);
         _revealSpriteAnim.resetAnimation();

         StartCoroutine(CO_Reveal());
      }
   }

   private IEnumerator CO_Reveal () {
      yield return new WaitForSeconds(2f);
      _showRevealVisual = false;
      openDiscoveryPanel();
   }

   public int getXPValue () {
      // Calculate an XP value based on the rarity of the discovery
      return Mathf.Clamp(BASE_EXPLORER_XP * (int) data.rarity, BASE_EXPLORER_XP, MAX_EXPLORER_XP);
   }

   public int getInstanceId () {
      return instanceId;
   }

   private int revealAnimPressedFrame () {
      // The frame of the reveal animation where the button is pressed
      switch (data.category) {
         case Category.Arcane:
            return 34;
         case Category.Architectural:
            return 29;
         case Category.Natural:
            return 28;
         default:
            return 34;
      }
   }

   #region Private Variables

   // How much progress has the player done exploring this discovery
   private float _currentProgress = default;

   // True when the local player is within the trigger
   private bool _isLocalPlayerInside = default;

   // True when the discovery is explored by the local player
   private bool _isRevealed = false;

   // True when we're waiting for the server to reply to our reveal request
   private bool _isWaitingForRequestResponse = false;

   // Is user hovering over this discovery with the mouse
   private bool _hovered = false;

   // Is user holding the mouse over currently
   private bool _pointerHeld = false;

   // Should mist clouds be shown
   private bool _showMist = false;

   // Should reveal visual be shown
   private bool _showRevealVisual = false;

   // Main sprite of the discovery
   [SerializeField] private SpriteRenderer _discoverySprite = null;

   // Main sprite of the reveal visual
   [SerializeField] private SpriteRenderer _revealSprite = null;
   [SerializeField] private SimpleAnimation _revealSpriteAnim = null;

   // Mist clouds sprites
   [SerializeField] private SpriteRenderer[] _mistRenderers = new SpriteRenderer[0];

   // Transform which controls the reveal slider
   [SerializeField] private Transform _revealSliderVisual = null;
   [SerializeField] private SpriteMask _revealSliderMask = null;

   // The outline of the main discovery sprite
   [SerializeField] private SpriteOutline _outline = null;

   // The position of this discovery
   [SyncVar]
   private Vector3 _discoveryPosition = default;

   // How much time (in seconds) it takes to explore a discovery
   private const float EXPLORE_DISCOVERY_TIME = 2.0f;

   // The base explorer XP gained for exploring any discovery
   private const int BASE_EXPLORER_XP = 5;

   // The maximum explorer experience a player can get for finding a discovery
   private const int MAX_EXPLORER_XP = 25;

   #endregion
}
