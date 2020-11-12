using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

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

   #endregion

   protected override void Start () {
      base.Start();

      if (_entity == null) {
         return;
      }

      StartCoroutine(CO_InitializeUserInfo());
   }

   protected override void Update () {
      base.Update();

      if (_entity == null) {
         return;
      }

      if (_showShipInfo) {
         guildIcon.show();
         showCharacterPortrait();
      } else {
         guildIcon.hide();
         hideCharacterPortrait();
      }

      // Enable the empty column to correctly align the character portrait when there is no guild icon
      togglableSpacer.SetActive(!guildIcon.gameObject.activeSelf && !barsContainer.activeSelf);
   }

   private IEnumerator CO_InitializeUserInfo () {
      // Wait until the entity has been initialized
      while (Util.isEmpty(_entity.entityName)) {
         yield return null;
      }

      portrait.initialize(_entity);

      // Set the portrait frame for local or non local entities
      if (_entity.isLocalPlayer) {
         portraitFrameImage.sprite = localPlayerFrame;
      } else {
         portraitFrameImage.sprite = nonLocalPlayerFrame;
      }

      if (_entity.guildId > 0) {
         PlayerShipEntity shipEntity = (PlayerShipEntity) _entity;

         if (!Util.isEmpty(shipEntity.guildIconBorder)) {
            guildIcon.setBorder(shipEntity.guildIconBorder);
         }
         if (!Util.isEmpty(shipEntity.guildIconBackground)) {
            guildIcon.setBackground(shipEntity.guildIconBackground, shipEntity.guildIconBackPalettes);
         }
         if (!Util.isEmpty(shipEntity.guildIconSigil)) {
            guildIcon.setSigil(shipEntity.guildIconSigil, shipEntity.guildIconSigilPalettes);
         }
      } else {
         guildIcon.gameObject.SetActive(false);
      }
   }

   private void showCharacterPortrait () {
      if (portrait != null && portraitCanvasGroup.alpha < 1) {
         portraitCanvasGroup.alpha = 1;
         portrait.updateBackground(_entity);
      }
   }

   private void hideCharacterPortrait () {
      if (portrait != null && portraitCanvasGroup.alpha > 0) {
         portraitCanvasGroup.alpha = 0;
      }
   }

   #region Private Variables

   #endregion
}
