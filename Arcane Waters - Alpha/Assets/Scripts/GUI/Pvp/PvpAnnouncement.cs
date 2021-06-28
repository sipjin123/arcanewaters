using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class PvpAnnouncement : MonoBehaviour
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

   // The duration of the announcement. If the value is not positive the announcement will not disappear automatically
   public int announceLifetimeSeconds = -1;

   // List of announce patterns
   public string[] announcePatterns;

   // self
   public static PvpAnnouncement self;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      // Start hidden
      toggle(false);
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

   public void toggleBlink (bool blink) {
      CancelInvoke(nameof(CO_Blink));
      if (blink) {
         outlineText.fontMaterial = new Material(outlineText.fontMaterial);
         InvokeRepeating(nameof(CO_Blink), 0, 0.16f);
      }
   }

   public void toggle (bool show) {
      if (show) {
         if (announceLifetimeSeconds > 0) {
            startCountdown();
         }
      } else {
         toggleBlink(false);
         stopCountdown();
      }
      this.gameObject.SetActive(show);
   }

   private void CO_Blink () {
      Color newColor = Color.Lerp(Color.white, blinkingColor, (Mathf.Sin(Time.time * blinkingFrequency) + 1) * 0.5f);
      setOutlineColor(newColor);
   }

   private void CO_Update () {
      if (announceLifetimeSeconds < 0) {
         return;
      }

      if (_toggleTime + announceLifetimeSeconds < Time.realtimeSinceStartup) {
         toggle(false);
      }
   }

   private void startCountdown () {
      _toggleTime = Time.realtimeSinceStartup;
      InvokeRepeating(nameof(CO_Update), 0, 1);
   }

   private void stopCountdown () {
      CancelInvoke(nameof(CO_Update));
   }

   private void setOutlineColor (Color newColor) {
      outlineText.fontMaterial.SetColor("_FaceColor", newColor);
   }

   #region Private Variables

   // Toggle time
   private float _toggleTime;

   #endregion
}
