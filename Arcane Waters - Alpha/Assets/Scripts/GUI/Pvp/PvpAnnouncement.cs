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

   // Should the announcement blink?
   public bool isBlinking = false;

   // The frequency of the blinking effect
   public float blinkingFrequency = 1.0f;

   // The color used for the Blinking effect
   public Color blinkingColor = Color.red;

   // The duration of the announcement
   public int announceLifetimeSeconds = -1;

   // List of announce patterns
   public string[] announcePatterns;

   // List of announce patterns for structures
   public string[] structureAnnouncePatterns;

   // The importance of this announcement's message
   public Priority announcementPriority = Priority.None;

   public enum Priority
   {
      None = 0,
      PlayerKill = 1,
      ObjectiveUpdate = 2,
      ScoreChange = 3,
      GameEnd = 4
   }

   #endregion

   public void Start () {
      _creationTime = Time.realtimeSinceStartup;
      outlineText.fontMaterial = new Material(outlineText.fontMaterial);
   }

   public void Update () {
      updateAnnouncement();
      updateBlink();
   }

   public void announceTowerDestruction (NetEntity attacker, PvpTower target) {
      string targetIdentifier = $"a {target.faction} Tower";
      string pattern = "{0} destroyed {1}!";

      if (structureAnnouncePatterns != null && structureAnnouncePatterns.Length > 0) {
         Random.InitState(Mathf.FloorToInt(Time.realtimeSinceStartup));
         int randomPatternIndex = Random.Range(0, structureAnnouncePatterns.Length);
         pattern = structureAnnouncePatterns[randomPatternIndex];
      }

      pattern = pattern.Replace("{0}", attacker.entityName);
      pattern = pattern.Replace("{1}", targetIdentifier);
      setText(pattern);
   }

   public void announceKill (NetEntity attacker, NetEntity target) {
      string pattern = "{0} destroyed {1}!";

      if (announcePatterns != null && announcePatterns.Length > 0) {
         Random.InitState(Mathf.FloorToInt(Time.realtimeSinceStartup));
         int randomPatternIndex = Random.Range(0, announcePatterns.Length);
         pattern = announcePatterns[randomPatternIndex];
      }

      pattern = pattern.Replace("{0}", attacker.entityName);
      pattern = pattern.Replace("{1}", target.entityName);
      setText(pattern);
   }

   public void announce (string announcementText) {
      setText(announcementText);
   }

   public void hide () {
      Destroy(gameObject);
   }

   private void setText (string newText) {
      outlineText.text = newText;
      insideText.text = newText;
   }

   public string getText () {
      return insideText.text;
   }

   private void updateBlink () {
      if (isBlinking) {
         Color newColor = Color.Lerp(Color.white, blinkingColor, (Mathf.Sin(Time.time * blinkingFrequency) + 1) * 0.5f);
         setOutlineColor(newColor);
      }
   }

   private void updateAnnouncement () {
      if (announceLifetimeSeconds > 0 && _creationTime + announceLifetimeSeconds < Time.realtimeSinceStartup) {
         hide();
      }
   }

   private void setOutlineColor (Color newColor) {
      outlineText.fontMaterial.SetColor("_FaceColor", newColor);
   }

   #region Private Variables

   // Toggle time
   private float _creationTime;

   #endregion
}
