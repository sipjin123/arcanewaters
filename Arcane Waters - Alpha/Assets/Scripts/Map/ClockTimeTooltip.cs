using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ClockTimeTooltip : ClientMonoBehaviour {
   #region Public Variables

   // The object containing the label
   public GameObject labelUI;

   // Text component which contains current system time
   public TMPro.TextMeshProUGUI timeText;

   #endregion

   protected override void Awake () {
      base.Awake();

      // Look up components
      _outline = GetComponentInChildren<SpriteOutline>();
      _clickableBox = GetComponentInChildren<ClickableBox>();
   }

   public void Update () {
      // Figure out whether our outline should be showing
      handleSpriteOutline();
   }

   private void FixedUpdate () {
      System.DateTime time = System.DateTime.Now;
      timeText.text = "The clock reads " + time.ToString("h:mm tt") + " and " + time.ToString("ss") + " seconds";

      // Use Time.fixedDeltaTime to ensure that sound will be played only once
      if (time.Millisecond < Time.fixedDeltaTime * 1000 && time.Second == 0 && time.Minute == 0) {
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.ON_THE_HOUR_CHIME, transform.position);
      }
   }

   public void handleSpriteOutline () {
      if (_outline == null || _clickableBox == null) {
         return;
      }

      // Only show our outline when the mouse is over us
      bool isHovering = MouseManager.self.isHoveringOver(_clickableBox);
      _outline.setVisibility(isHovering);
      labelUI.SetActive(isHovering);
   }

   #region Private Variables

   // Outline of clock object
   protected SpriteOutline _outline;

   // Button which is used to check if mouse is above it
   protected ClickableBox _clickableBox;

   #endregion
}
