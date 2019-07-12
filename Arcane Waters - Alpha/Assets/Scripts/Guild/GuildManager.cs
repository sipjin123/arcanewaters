using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class GuildManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static GuildManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      // Routinely clear out any old invites
      InvokeRepeating("clearOldInvites", 0f, 60f);
   }

   protected void clearOldInvites () {
      // Copy the past invites into a list, so that we can modify the hash set
      List<GuildInvite> invites = new List<GuildInvite>();
      invites.AddRange(_pastInvites);

      // Cycle over all of the past invites, looking for old ones
      foreach (GuildInvite invite in invites) {
         DateTime sendTime = DateTime.FromBinary(invite.inviteTime);

         // If enough time has passed, remove it from the hash set
         if (sendTime.AddMinutes(5) < DateTime.Now) {
            _pastInvites.Remove(invite);
         }
      }
   }

   public void handleInvite (NetEntity sender, int recipientId, GuildInfo guildInfo) {
      BodyEntity recipient = BodyManager.self.getBody(recipientId);

      // Make sure the sender is in a guild and the recipient is not
      if (sender.guildId == 0 || recipient.guildId != 0) {
         D.warning("Invalid guild invite from " + sender + " to: " + recipientId);
         return;
      }

      // Make sure the sender isn't spamming
      if (getRecentInviteCount(sender.userId) > 3) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, sender, "You sent too many guild invites recently to send another.");
         return;
      }

      // Create the invite
      GuildInvite invite = new GuildInvite();
      invite.guildId = sender.guildId;
      invite.senderId = sender.userId;
      invite.senderName = sender.entityName;
      invite.guildName = guildInfo.guildName;
      invite.recipientId = recipientId;
      invite.inviteTime = DateTime.Now.ToBinary();

      // Make sure this invite doesn't already exist
      if (_pastInvites.Contains(invite)) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, sender, "This invite has already been sent.");
         return;
      }

      // Store the invite
      _pastInvites.Add(invite);

      // Send the invite to the target
      recipient.rpc.Target_ReceiveGuildInvite(recipient.connectionToClient, invite);
   }

   public void acceptInviteOnClient (GuildInvite invite) {
      Global.player.rpc.Cmd_AcceptInvite(invite);

      // Hide the confirm panel
      PanelManager.self.confirmScreen.hide();
   }

   public void acceptInviteOnServer (NetEntity recipient, GuildInvite invite) {
      // Make sure the player isn't already in a guild
      if (recipient.guildId != 0) {
         return;
      }

      // Make sure the invite hasn't expired
      if (!_pastInvites.Contains(invite)) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, recipient, "This invite no longer exists.");
         return;
      }

      // Update the guild
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.assignGuild(recipient.userId, invite.guildId);
         recipient.guildId = invite.guildId;

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, recipient, "You have joined the guild " + invite.guildName + "!");
         });
      });
   }

   protected int getRecentInviteCount (int userId) {
      int count = 0;

      foreach (GuildInvite invite in _pastInvites) {
         if (invite.senderId == userId) {
            count += 1;
         }
      }

      return count;
   }

   #region Private Variables

   // Stores past invites that have been sent
   protected HashSet<GuildInvite> _pastInvites = new HashSet<GuildInvite>();

   #endregion
}
