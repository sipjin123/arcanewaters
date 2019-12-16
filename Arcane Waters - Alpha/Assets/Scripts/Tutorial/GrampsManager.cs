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
      if (Input.GetKeyUp(KeyCode.Space) && !ChatPanel.self.inputField.isFocused && !MailPanel.self.isWritingMail()) {
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
      Step currentStep = TutorialManager.currentStep;
      List<string> msgList = new List<string>();

      if (currentStep == Step.GetDressed) {
         msgList.Add("Hey there, squirt!  It's about time you got out of bed, we've got a big day ahead of us!  Hurry up and get dressed!");
      } else if (currentStep == Step.FindSeedBag) {
         msgList.Add("Now that you're awake, I could use your help running the farm.  If we sell the crops, it's a great way to make some extra money!");
         msgList.Add("The first thing we should do is plant some seeds.  I'm sure I left the seed bag somewhere outside the house.  Could you try and find it?");
      } else if (currentStep == Step.PlantSeeds) {
         msgList.Add("Great, you found it!  I had been looking everywhere for that thing.");
         msgList.Add("There are some holes in the dirt ready for seeds.  Press 1 to equip the seed bag, and go walk over each of the holes to plant the seeds.");
      } else if (currentStep == Step.GetWateringCan) {
         msgList.Add("Nice work!  Pretty soon, those plants are going to need some water.  I left a watering can in the house you can use.");
      } else if (currentStep == Step.StartWatering) {
         msgList.Add("Oh good, you found where I put it.  You can press 2 to equip the watering can, and then take it to the seeds that you planted.");
      } else if (currentStep == Step.FinishWatering) {
         msgList.Add("The crops will need to be watered a few times before they're ready for harvest.");
         msgList.Add("Some types of plants take a while before they're ready for water, but these crops should grow quite fast!");
      } else if (currentStep == Step.GetPitchfork) {
         msgList.Add("The crops are ready for harvest!  You'll need a pitchfork, I just left one in the house for you.");
      } else if (currentStep == Step.HarvestCrops) {
         msgList.Add("Press 3 to equip the pitchfork, and then go harvest your crops!");
      } else if (currentStep == Step.HeadToDocks) {
         msgList.Add("Now that we have some crops, we can sell them at another town for a profit!");
         msgList.Add("I'm going to give you my old merchant ship.  It's nothing fancy, but it's a start.");
         msgList.Add("Head to the docks south of town and hop on board!");
      } else if (currentStep == Step.FindTown) {
         msgList.Add("I've heard there's a town north of here a little way, start sailing that way and see if you can find it!");
      } else if (currentStep == Step.SellCrops) {
         msgList.Add("Great, you found it!  Head into the shop to sell your crops for some profit!");
      } else if (currentStep == Step.FindTreasureSite) {
         msgList.Add("Now that you know how to run the farm and make money, there's one more thing I want to teach you.");
         msgList.Add("There are some forests north of here that are rumored to have treasure.  See if you can find it!");
      } else if (currentStep == Step.DestroyGate) {
         msgList.Add("You found the treasure site!  But it looks like the entrance is blocked off to guard the treasure inside!");
         msgList.Add("If you hold right-click you can fire the cannons out the side of the ship.  Attack the encampment until the gates open, and then you can get inside!");
      } else if (currentStep == Step.EnterTreasureSite) {
         msgList.Add("You cleared the encampment gates!  Let's find out what's inside!");
      } else if (currentStep == Step.ExploreTreasureSite) {
         msgList.Add("The treasure site has been overrun with strange lizard creatures!  It's a good thing this is just a demo, or I bet they'd attack!");
         msgList.Add("Well, squirt, I've taught you everything I know now.  If you keep running the farm and finding treasure sites like this one, you'll be rich in no time!");
      }

      // Only add messages that we haven't already shown
      foreach (string msg in msgList) {
         if (!_textsShown.Contains(msg) && !_listOfThingsToSay.Contains(msg)) {
            _listOfThingsToSay.Add(msg);
         }
      }

      startSayingStuffFromList();
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
