using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System;

public class GuildPanel : Panel, IPointerClickHandler {
   #region Public Variables

   // The button for creating a guild
   public Button createButton;

   // The button for leaving a guild
   public Button leaveButton;

   // The container for our guild member list
   public GameObject memberContainer;

   // The prefab we use for creating a guild member row
   public GameObject guildMemberPrefab;

   // Our various texts
   public Text nameText;
   public Text dateText;
   public Text levelText;
   public Text countText;

   // Our various images
   public Image flagImage;

   // The guild icon
   public GuildIcon guildIcon;

   // Self
   public static GuildPanel self;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public void receiveDataFromServer (GuildInfo info) {
      bool inGuild = Global.player.guildId != 0;

      // Disable and enable buttons and images
      createButton.interactable = !inGuild;
      leaveButton.interactable = inGuild;
      guildIcon.gameObject.SetActive(inGuild);
      flagImage.enabled = inGuild;

      // Fill in the texts
      nameText.text = inGuild ? info.guildName : "";
      dateText.text = inGuild ? DateTime.FromBinary(info.creationTime).ToString("MMMM yyyy") : "";
      levelText.text = "Level " + (inGuild ? "1" : "");
      countText.text = inGuild ? ("Members: " + info.guildMembers.Length + " / " + GuildManager.MAX_MEMBERS) : "";

      // Set the guild icon
      if (inGuild) {
         guildIcon.setBorder(info.iconBorder);
         guildIcon.setBackground(info.iconBackground, info.iconBackPalette1, info.iconBackPalette2);
         guildIcon.setSigil(info.iconSigil, info.iconSigilPalette1, info.iconSigilPalette2);
      }

      // Clear out any old member info
      memberContainer.DestroyChildren();

      if (info.guildMembers != null) {
         foreach (UserInfo member in info.guildMembers) {
            GameObject memberRow = Instantiate(guildMemberPrefab);
            memberRow.GetComponent<Text>().text = member.username;
            memberRow.transform.SetParent(memberContainer.transform);
         }
      }
   }

   public void createGuildPressed () {
      PanelManager.self.pushPanel(Type.GuildCreate);
   }

   public void leaveGuildPressed () {
      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmedLeaveGuild());

      // Show a confirmation panel with the user name
      PanelManager.self.confirmScreen.show("Are you sure you want to leave your guild?");
   }

   protected void confirmedLeaveGuild () {
      Global.player.rpc.Cmd_LeaveGuild();

      // Exit all panels
      PanelManager.self.confirmScreen.hide();
      PanelManager.self.popPanel();
   }

   public void OnPointerClick (PointerEventData eventData) {
      // If the black background outside is clicked, hide the panel
      if (eventData.rawPointerPress == this.gameObject) {
         PanelManager.self.popPanel();
      }
   }

   #region Private Variables

   #endregion
}
