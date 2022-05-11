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

      if (!_isRevealed && !_isWaitingForRequestResponse) {
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

                  _canCancelInvestigateHoldSound = false;
               }

               // Play investigate hold sound loop
               if (_canPlayInvestigateHoldSound) {
                  playSoundEffect(SoundEvent.Investigate_Hold);
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

         if (_hovered && _revealSpriteAnim.getIndex() == revealAnimPressedFrame() - 1 && _canPlayInvestigateAppearSound) {
            playSoundEffect(SoundEvent.Investigate_Appear);
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

      if (!_isRevealed && _canCancelInvestigateHoldSound) {
         playSoundEffect(SoundEvent.Stop_Investigate_Hold);
      }
   }

   public void onPointerEnter () {
      _hovered = true;
      _outline.setVisibility(_isRevealed);

      _showMist = false;
      if (!_isRevealed) {
         if (_showRevealVisual == false) {
            _revealSpriteAnim.updateIndexMinMax(0, revealAnimPressedFrame() - 1);
            _revealSpriteAnim.setIndex(0);
            _revealSpriteAnim.resetAnimation();
         }

         _showRevealVisual = true;

         playSoundEffect(SoundEvent.Appear);
      }
   }

   public void onPointerExit () {
      _hovered = false;
      _outline.setVisibility(false);

      if (!_isRevealed) {
         _showMist = true;

         playSoundEffect(SoundEvent.None);
      }
      _showRevealVisual = false;
   }

   private void openDiscoveryPanel () {
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

         playSoundEffect(SoundEvent.Glow);
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

   private void playSoundEffect (SoundEvent soundEvent) {
      if (!_fmodEvent.isValid()) {
         _fmodEvent = SoundEffectManager.self.createEventInstance(SoundEffectManager.DISCOVERY_RUINS);
         _fmodEvent.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(this.transform.position));
      }

      _fmodEvent.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE state);
      _fmodEvent.getParameterByName(SoundEffectManager.AUDIO_SW, out float value);

      int paramValue = (int) value;

      if (((state == FMOD.Studio.PLAYBACK_STATE.PLAYING || !_canPlayAppearSound) && soundEvent == SoundEvent.Appear) ||
         (soundEvent == SoundEvent.Investigate_Appear && !_canPlayInvestigateAppearSound) ||
         (soundEvent == SoundEvent.Investigate_Hold && !_canPlayInvestigateHoldSound) ||
         (soundEvent == SoundEvent.Stop_Investigate_Hold && paramValue != (int) SoundEvent.Investigate_Hold)) {
         return;
      } else if (soundEvent == SoundEvent.None || (paramValue == (int) SoundEvent.Investigate_Hold && soundEvent == SoundEvent.Stop_Investigate_Hold)) {
         _fmodEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

         _canPlayAppearSound = _canPlayInvestigateAppearSound = soundEvent == SoundEvent.None;
         _canPlayInvestigateHoldSound = true;
      } else {
         _fmodEvent.setParameterByName(SoundEffectManager.AUDIO_SW, (int) soundEvent);
         _fmodEvent.start();

         if (soundEvent == SoundEvent.Appear) {
            _canPlayAppearSound = false;
         } else if (soundEvent == SoundEvent.Investigate_Appear) {
            _canPlayInvestigateAppearSound = false;
         } else if (soundEvent == SoundEvent.Investigate_Hold) {
            _canPlayInvestigateHoldSound = false;
         } else if (soundEvent == SoundEvent.Glow) {
            _fmodEvent.release();
         }
      }
   }

   #region Private Variables

   // How much progress has the player done exploring this discovery
   private float _currentProgress = default;

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

   // FMOD event
   private FMOD.Studio.EventInstance _fmodEvent;

   // The sound effect that should play once when the mist disappears
   private bool _canPlayAppearSound = true;

   // The sound effect that should play once when the Investigate button appears
   private bool _canPlayInvestigateAppearSound = true;

   // The sound effect that loops when the Investigate button is hold
   private bool _canPlayInvestigateHoldSound = true;

   // The investigate hold loop won't be cancelled after revealing the discovery
   private bool _canCancelInvestigateHoldSound = true;

   private enum SoundEvent
   {
      None = -2,
      Stop_Investigate_Hold = -1,
      Appear = 0,
      Investigate_Appear = 1,
      Investigate_Hold = 2,
      Glow = 3
   }

   #endregion
}
