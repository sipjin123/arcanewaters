using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class ChatInfo
{
   #region Public Variables

   // The Type of message
   public enum Type
   {
      Local = 1,
      Whisper = 2,
      Log = 3,
      Warning = 4,
      System = 5,
      Debug = 6,
      Error = 7,
      Trade = 8,
      Permit = 9,
      Guild = 10,
      Emote = 11,
      Global = 12,
      Group = 13,
      Officer = 14,
      PvpAnnouncement = 15,
      UserOnline = 16,
      UserOffline = 17,
      PendingFriendRequestsNotification = 18
   };

   // The Chat ID from the database
   public int chatId;

   // The type of message this is
   public Type messageType;

   // The text associated with this message
   public string text;

   // The name of the person who sent the message
   public string sender;

   // The name of the person who will receive the message
   public string recipient;

   // Bool to keep track if the sender has admin privileges
   public bool isSenderAdmin;

   // The user ID of the sender
   public int senderId;

   // The user ID of the recipient
   public int recipientId;

   // The time at which this message was received by the server
   public DateTime chatTime;

   // The guild ID associated with this chat, if any
   public int guildId;

   // The Guild Name
   public string guildName;

   // The data to construct the guild icon
   public GuildIconData guildIconData;

   // Is the sender muted?
   public bool isSenderMuted;

   // Extra
   public string extra;

   #endregion

   public ChatInfo () {

   }

   public ChatInfo (int chatId, string text, DateTime chatTime, Type messageType, string sender = "", string receiver = "", int senderId = 0, GuildIconData guildIconData = null, string guildName="", bool isSenderMuted = false, bool isSenderAdmin = false, int recipientId = 0, string extra = "") {
      this.chatId = chatId;
      this.text = text;
      this.chatTime = chatTime;
      this.messageType = messageType;
      this.sender = sender;
      this.recipient = receiver;
      this.senderId = senderId;
      this.recipientId = recipientId;
      this.guildIconData = guildIconData;
      this.guildName = guildName;
      this.isSenderMuted = isSenderMuted;
      this.isSenderAdmin = isSenderAdmin;
      this.extra = extra;

      // Fill in the sender for certain types of messages
      if (Util.isEmpty(sender)) {
         sender = getSender(messageType);
      }
   }

   protected static string getSender (Type messageType) {
      switch (messageType) {
         case Type.Debug:
            return "Debug";
         case Type.Error:
            return "Error";
         case Type.System:
            return "System";
         case Type.Warning:
            return "Warning";
         case Type.Log:
            return "Log";
      }

      return "";
   }

   #region Private Variables

   #endregion

}
