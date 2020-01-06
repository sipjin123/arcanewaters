using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GrampsManager : ClientMonoBehaviour {
   #region Public Variables

   // The component that holds gramps text
   public Text text;

   // The container for the Gramps panel
   public GameObject grampsContainer;

   // The component that animates the face
   public SimpleAnimation faceAnimation;

   // The Image for our face
   public Image faceImage;

   // The Container for the animated arrow
   public GameObject arrowContainer;

   // Gets set to true after Gramps has been manually closed
   public static bool hasBeenClosedOnce = false;

   // Self
   public static GrampsManager self;

   #endregion

   protected override void Awake () {
      base.Awake();

      // Store a reference
      self = this;
   }

   protected void Start () {
      // Start out hidden
      grampsContainer.SetActive(false);

      // Make sure our Gramps head has everything it needs to animate properly
      faceAnimation.initialize();

      EventManager.StartListening(EventManager.COMPLETED_TUTORIAL_STEP, completedTutorialStep);
   }

   protected void Update () {
      // IF we're back on the title screen, gramps goes away
      if (TitleScreen.self.isShowing()) {
         clear();
         return;
      }

      // Allow pressing certain keys, instead of clicking, to move the text forward
      if (Input.GetKeyUp(KeyCode.Space) && Util.isGeneralInputAllowed()) {
         wasClicked();
      }

      // Check if our text was just updated
      if (text.text != _previousText) {
         _lastTalkTime = Time.time;
      }

      // Hide the arrow until we finish talking
      arrowContainer.SetActive(!isTalking());

      // Animate the face while we're talking
      updateFaceAnimation();

      // Keep track of the text for the next frame
      _previousText = text.text;
   }

   public static bool isShowing () {
      return self.grampsContainer.activeSelf;
   }

   public static void startTalkingToPlayerAfterDelay (float delay = 3f) {
      // Give a bit of delay before we start blabbing
      self.Invoke(self.startTalkingToPlayer, delay);
   }

   public void wasClicked () {
      // If we're currently in the middle of saying something, finish it up
      if (isTalking()) {
         finishTypingText();
      } else {
         // If we had already finished what we were saying, then move on
         moveToNextText();
      }
   }

   public void clear () {
      // If we're typing out some text, finish it up
      if (isTalking()) {
         finishTypingText();
      }

      // Clear out our data
      _listOfThingsToSay.Clear();
      _textsShown.Clear();
      _lastTalkTime = float.MinValue;
      text.text = "";

      // Hide the panel
      grampsContainer.SetActive(false);
   }

   protected void startTalkingToPlayer () {
      // We can't do anything without a player
      if (Global.player == null) {
         return;
      }

      // Check which step of the tutorial the player is on
      int currentStep = TutorialManager.currentStep;
      List<string> msgList = new List<string>();
      TutorialData fetchData = TutorialManager.self.fetchTutorialData(currentStep);

      if (fetchData != null) {
         foreach (string dialogue in fetchData.msgList) {
            msgList.Add(dialogue);
         }

         // Only add messages that we haven't already shown
         foreach (string msg in msgList) {
            if (!_textsShown.Contains(msg) && !_listOfThingsToSay.Contains(msg)) {
               _listOfThingsToSay.Add(msg);
            }
         }

         startSayingStuffFromList();
      }
   }

   protected void finishTypingText () {
      if (_listOfThingsToSay.Count > 0) {
         AutoTyper.FinishText(text, _listOfThingsToSay[0]);
      }
   }

   protected void startSayingStuffFromList () {
      // If we don't have anything to say, we're done
      if (_listOfThingsToSay.Count <= 0) {
         return;
      }

      string textToShow = _listOfThingsToSay[0];

      // If we're already showing some text, don't show it again
      if (text.text.Contains(textToShow) || isTalking()) {
         return;
      }

      // Make sure the panel is enabled
      grampsContainer.SetActive(true);

      // Start typing it out
      AutoTyper.SlowlyRevealText(text, textToShow);

      // Keep track of the texts that we've shown
      _textsShown.Add(textToShow);
   }

   protected void moveToNextText () {
      // Remove the thing we just said
      if (_listOfThingsToSay.Count > 0) {
         _listOfThingsToSay.RemoveAt(0);
      }

      // Check if there's anything else to say
      if (_listOfThingsToSay.Count > 0) {
         startSayingStuffFromList();
      } else {
         // There was nothing else, so hide the panel
         grampsContainer.SetActive(false);

         // Note that the player has closed Gramps
         hasBeenClosedOnce = true;
      }
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

   protected void completedTutorialStep () {
      D.debug("Completed a tutorial step, current step is: " + TutorialManager.currentStep);

      // After completing a step, clear everything out
      GrampsManager.self.clear();

      // When they complete a step, have Gramps start talking about the next one
      startTalkingToPlayer();
   }

   #region Private Variables

   // The time at which the text last changed
   protected float _lastTalkTime;

   // The text that we had in the previous frame
   protected string _previousText;

   // The list of things we want to say
   protected List<string> _listOfThingsToSay = new List<string>();

   // Keeps track of the texts that we've shown to the player since they launched the client
   protected static HashSet<string> _textsShown = new HashSet<string>();

   #endregion
}
