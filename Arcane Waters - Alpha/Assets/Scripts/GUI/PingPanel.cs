using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PingPanel : ClientMonoBehaviour {
   #region Public Variables

   // The Image that shows our ping
   public Image pingIcon;

   // The animation for our ping icon
   public SimpleAnimation pingAnimation;

   // The textures we choose from
   public Texture2D pingGreen;
   public Texture2D pingYellow;
   public Texture2D pingRed;

   // The container for the ping panel
   public GameObject container;

   // Self
   public static PingPanel self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   void Update () {
      int ping = getPing();

      // Check if we're connected to the network
      bool isConnected = NetworkManager.singleton != null && NetworkClient.active;

      // Check if we're on one of the intro screens
      bool isShowingIntroScreens = TitleScreen.self.isShowing() || CharacterScreen.self.isShowing();

      // Only show the ping panel at the appropriate times
      container.SetActive(isConnected && !isShowingIntroScreens);

      // Update the color of the image based on the current ping
      if (ping <= 120) {
         pingAnimation.setNewTexture(pingGreen);
      } else if (ping <= 240) {
         pingAnimation.setNewTexture(pingYellow);
      } else {
         pingAnimation.setNewTexture(pingRed);
      }
   }

   public int getPing () {
      // Check if we're connected to the network
      bool isConnected = NetworkManager.singleton != null && NetworkClient.active;

      // Calculate our ping when we're connected
      int ping = isConnected ? (int) (NetworkTime.rtt * 1000) : 0;

      return ping;
   }

   public string getPingText () {
      // Set up some text we can show in the tooltip
      string pingText = "Ping: " + getPing();

      return pingText;

   }

   #region Private Variables

   #endregion
}
