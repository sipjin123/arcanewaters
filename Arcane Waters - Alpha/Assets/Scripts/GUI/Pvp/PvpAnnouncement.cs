using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class PvpAnnouncement : ClientMonoBehaviour
{
   #region Public Variables

   // The Outline of the Announcement's text
   public TextMeshProUGUI outlineText;

   // The Announcement's Text
   public TextMeshProUGUI insideText;

   // The color of the Outline
   public Color outlineColor = Color.white;

   // The frequency of the blinking effect
   public float blinkingFrequency = 1.0f;

   // The color used for the Blinking effect
   public Color blinkingColor = Color.red;

   // The duration of the announcement
   public int announceLifetimeSeconds = -1;

   // List of announce patterns
   public string[] announcePatterns;

   // Self
   public static PvpAnnouncement self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   private void Start () {
      // Start hidden
      hide();
   }

   public void setText (string newText) {
      outlineText.text = newText;
      insideText.text = newText;
   }

   public void announceKill (NetEntity attacker, NetEntity target) {
      string pattern = "{0} destroyed {1}!";
      if (announcePatterns != null && announcePatterns.Length > 0) {
         int randomPatternIndex = Random.Range(0, announcePatterns.Length);
         pattern = announcePatterns[randomPatternIndex];
      }
      pattern = pattern.Replace("{0}", attacker.entityName);
      pattern = pattern.Replace("{1}", target.entityName);
      setText(pattern);
   }

   public string getText () {
      return insideText.text;
   }

   public void startBlink () {
      stopBlink();
      outlineText.fontMaterial = new Material(outlineText.fontMaterial);
      InvokeRepeating(nameof(updateBlink), 0, 0.16f);
   }

   public void stopBlink () {
      CancelInvoke(nameof(updateBlink));
   }

   private void updateBlink () {
      Color newColor = Color.Lerp(Color.white, blinkingColor, (Mathf.Sin(Time.time * blinkingFrequency) + 1) * 0.5f);
      setOutlineColor(newColor);
   }

   public void show () {
      if (announceLifetimeSeconds > 0) {
         startCountdown();
         startVisibilityCheck();
      }
      this.gameObject.SetActive(true);
   }

   public void hide () {
      stopBlink();
      stopCountdown();
      stopVisibilityCheck();
      this.gameObject.SetActive(false);
   }

   public bool isShowing () {
      return this.gameObject.activeSelf;
   }

   private void startCountdown () {
      _toggleTime = Time.realtimeSinceStartup;
      InvokeRepeating(nameof(updateAnnouncement), 0, 1);
   }

   private void stopCountdown () {
      CancelInvoke(nameof(updateAnnouncement));
   }

   private void updateAnnouncement () {
      if (announceLifetimeSeconds < 0) {
         return;
      }

      if (_toggleTime + announceLifetimeSeconds < Time.realtimeSinceStartup) {
         hide();
      }
   }

   private void startVisibilityCheck () {
      InvokeRepeating(nameof(updateVisibilityCheck), 0, 1);
   }

   private void stopVisibilityCheck () {
      CancelInvoke(nameof(updateVisibilityCheck));
   }

   private void updateVisibilityCheck () {
      // Check if the panel should be hidden
      if (isShowing()) {
         if (Global.player == null) {
            hide();
            return;
         }

         Instance instance = Global.player.getInstance();
         if (instance == null || !instance.isPvP) {
            hide();
         }
      }
   }

   private void setOutlineColor (Color newColor) {
      outlineText.fontMaterial.SetColor("_FaceColor", newColor);
   }

   #region Private Variables

   // Toggle time
   private float _toggleTime;

   #endregion
}
