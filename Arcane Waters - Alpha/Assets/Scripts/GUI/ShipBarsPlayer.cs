using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class ShipBarsPlayer : ShipBars
{
   #region Public Variables

   // The guild icon
   public GuildIcon guildIcon;

   // An empty column used to correctly align icons in certain cases
   public GameObject togglableSpacer;

   // The character portrait
   public CharacterPortrait portrait;

   // The canvas group of the portrait
   public CanvasGroup portraitCanvasGroup;

   // The frame image of the character portrait
   public Image portraitFrameImage;

   // The frame used if the portrait is the local player's
   public Sprite localPlayerFrame;

   // The frame used if the portrait is not the local player's
   public Sprite nonLocalPlayerFrame;

   // The component that holds the outline/outsides of the name's UI
   public TextMeshProUGUI nameTextOutside;

   // The component that holds the insides of the name's UI
   public TextMeshProUGUI nameTextInside;

   // The width of the outline
   [Range(0.0f, 1.0f)]
   public float nameOutlineWidth;

   // The color the outline
   public Color32 nameOutlineColor;

   #endregion

   protected override void Start () {
      base.Start();

      if (_entity == null) {
         return;
      }

      StartCoroutine(CO_InitializeUserInfo());

      // Ship portrait and guild icon will be shown at all times
      guildIcon.show();
      showCharacterPortrait();
   }

   private void OnEnable () {
      InvokeRepeating(nameof(updateLocalPlayerPortrait), 0, 3.0f);
   }

   private void OnDisable () {
      CancelInvoke(nameof(updateLocalPlayerPortrait));
   }

   private IEnumerator CO_InitializeUserInfo () {
      // Wait until the entity has been initialized
      while (Util.isEmpty(_entity.entityName)) {
         yield return null;
      }

      initializeHealthBar();
      portrait.updateLayers(_entity);

      // Set the portrait frame for local or non local entities
      if (_entity.isLocalPlayer) {
         portraitFrameImage.sprite = localPlayerFrame;
      } else {
         portraitFrameImage.sprite = nonLocalPlayerFrame;
      }

      if (_entity.guildId > 0) {
         // If the player is in a pvp open world map but their pvp is set to inactive, do not show their guild icon
         if (!_entity.enablePvp && _entity.openWorldGameMode == PvpGameMode.GuildWars) {
            guildIcon.transform.parent.gameObject.SetActive(false);
         } else {
            PlayerShipEntity shipEntity = (PlayerShipEntity) _entity;

            if (!Util.isEmpty(shipEntity.guildIconBorder)) {
               guildIcon.setBorder(shipEntity.guildIconBorder);
            } else {
               guildIcon.border.enabled = false;
            }
            if (!Util.isEmpty(shipEntity.guildIconBackground)) {
               guildIcon.setBackground(shipEntity.guildIconBackground, shipEntity.guildIconBackPalettes);
            } else {
               guildIcon.background.enabled = false;
            }
            if (!Util.isEmpty(shipEntity.guildIconSigil)) {
               guildIcon.setSigil(shipEntity.guildIconSigil, shipEntity.guildIconSigilPalettes);
            } else {
               guildIcon.sigil.enabled = false;
            }
         }
      } else {
         guildIcon.transform.parent.gameObject.SetActive(false);
      }
   }

   private void showCharacterPortrait () {
      if (portrait != null && portraitCanvasGroup.alpha < 1) {
         portraitCanvasGroup.alpha = 1;
         portrait.updateBackground(_entity);
      }
   }

   private void updateLocalPlayerPortrait () {
      if (_entity == null) {
         return;
      }

      portrait.updateLayers(_entity);
   }

   #region Private Variables

   #endregion
}
