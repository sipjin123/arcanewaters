using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

// The types of chat commands
public enum CommandType
{
   None = 0,
   Admin = 1,
   Bug = 2,
   Follow = 3,
   Emote = 4,
   Invite = 5,
   Group = 6,
   Officer = 7,
   Guild = 8,
   Complain = 9,
   Roll = 10
}

public class ChatUtil
{
   // All possible variations that can be typed to execute each type of chat command
   public static Dictionary<CommandType, List<string>> commandTypePrefixes = new Dictionary<CommandType, List<string>>()
   {
      { CommandType.Admin, new List<string>(){ "/admin", "/a", "/ad", "adm" } },
      { CommandType.Bug, new List<string>(){ "/bug" } },
      { CommandType.Follow, new List<string>(){ "/follow" } },
      { CommandType.Emote, new List<string>(){ "/emote", "/em", "/e", "/emo", "/me" } },
      { CommandType.Invite, new List<string>(){ "/invite", "/inv" } },
      { CommandType.Group, new List<string>(){ "/group", "/party", "/gr", "/p" } },
      { CommandType.Officer, new List<string>(){ "/officer", "/off", "/of", "/o" } },
      { CommandType.Guild, new List<string>(){ "/guild", "/gld", "/g" } },
      { CommandType.Complain, new List<string>(){ "/complain", "/report" } },
      { CommandType.Roll, new List<string>(){ "/roll", "/random" } },
   };
}
