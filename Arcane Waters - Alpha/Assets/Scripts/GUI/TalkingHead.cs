using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class TalkingHead : MonoBehaviour {
   #region Public Variables

   // The text object that we monitor
   public TMP_Text text;

   // Our face animator
   public SimpleAnimation faceAnimation;

   // Our face Image
   public Image faceImage;

   #endregion

   private void Start () {
      _textFade = this.text.GetComponent<RollingTextFade>();
   }

   protected void Update () {
      // Check if our text is currently fading in
      if (!_textFade.isDone) {
         _lastTalkTime = Time.time;
      }

      // Animate the face while we're talking
      updateFaceAnimation();

      // Keep track of the text for the next frame
      _previousText = text.text;
   }

   protected void updateFaceAnimation () {
      if (isTalking()) {
         faceAnimation.enabled = true;
      } else {
         faceAnimation.enabled = false;
         faceImage.sprite = faceAnimation.getInitialSprite();
      }
   }

   protected bool isTalking () {
      return (Time.time - _lastTalkTime < .15f);
   }

   #region Private Variables

   // The time at which the text last changed
   protected float _lastTalkTime;

   // The text that we had in the previous frame
   protected string _previousText;

   // The component that slowly reveals the head's text
   protected RollingTextFade _textFade;
      
   #endregion
}
