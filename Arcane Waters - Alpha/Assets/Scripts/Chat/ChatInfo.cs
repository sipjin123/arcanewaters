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
   public enum Type {
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
   };

   // The Chat ID from the database
   public int chatId;

   // The type of message this is
   public Type messageType;

   // The text associated with this message
   public string text;

   // The name of the person who sent the message
   public string sender;

   // The user ID of the sender
   public int senderId;

   // The time at which this message was received by the server
   public DateTime chatTime;

   // The guild ID associated with this chat, if any
   public int guildId;

   // The Guild Name
   public string guildName;

   // The data to construct the guild icon
   public GuildIconData guildIconData;
   #endregion

   public ChatInfo () {

   }

   public ChatInfo (int chatId, string text, DateTime chatTime, Type messageType, string sender = "", int senderId = 0, GuildIconData guildIconData = null) {
      this.chatId = chatId;
      this.text = text;
      this.chatTime = chatTime;
      this.messageType = messageType;
      this.sender = sender;
      this.senderId = senderId;
      this.guildIconData = guildIconData;

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
