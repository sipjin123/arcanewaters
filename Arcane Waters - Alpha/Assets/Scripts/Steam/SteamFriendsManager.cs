using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Steamworks;
using Steam;

public class SteamFriendsManager : MonoBehaviour
{
   #region Public Variables

   // Key we use to set join friend connect parameter
   public const string JOIN_FRIEND_PARAM_PREFIX = "join-friend-";

   #endregion

   private void OnEnable () {
      if (!SteamManager.Initialized) {
         if (Util.isCloudBuild()) {
            D.error("Steam Manager is not initialized!");
         }
         return;

      }

      // Hook up callbacks
      _gameRichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(onGameRichPresenceJoinRequested);
      _newUrlLaunchParameters = Callback<NewUrlLaunchParameters_t>.Create(onNewUrlLaunchParameters);

      // Check if this game has connect parameters in command line
      tryJoinUserFromLaunchParameters();

      // Set rich presence key for how friends can connect
      SteamFriends.SetRichPresence("connect", JOIN_FRIEND_PARAM_PREFIX + SteamUser.GetSteamID().m_SteamID.ToString());
   }

   public static List<SteamFriendData> getSteamFriends () {
      List<SteamFriendData> result = new List<SteamFriendData>();

      if (SteamManager.Initialized) {
         int n = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
         for (int i = 0; i < n; i++) {
            CSteamID id = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
            if (id.IsValid()) {
               bool playingArcaneWaters = false;
               if (SteamFriends.GetFriendGamePlayed(id, out FriendGameInfo_t info)) {
                  if (info.m_gameID.Type() == CGameID.EGameIDType.k_EGameIDTypeApp) {
                     if (SteamStatics.ALL_CLIENT_APPIDS.Contains(info.m_gameID.AppID().ToString())) {
                        playingArcaneWaters = true;
                     }
                  }
               }

               result.Add(new SteamFriendData {
                  steamId = id.m_SteamID,
                  name = SteamFriends.GetFriendPersonaName(id),
                  personaState = SteamFriends.GetFriendPersonaState(id),
                  playingArcaneWaters = playingArcaneWaters,
                  avatarImageIndex = SteamFriends.GetSmallFriendAvatar(id)
               });
            }
         }
      }

      return result;
   }

   public static void requestFriendListImages (List<SteamFriendData> friends) {
      if (SteamManager.Initialized && friends.Count > 0) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            foreach (SteamFriendData friend in friends) {
               // Capture variable for new thread
               SteamFriendData capture = friend;
               // Get data from steam
               if (friend.avatarImageIndex > 0) {
                  if (SteamUtils.GetImageSize(friend.avatarImageIndex, out uint w, out uint h)) {
                     int height = (int) h;
                     int width = (int) w;
                     int bufferSize = 4 * width * height;
                     byte[] buffer = new byte[bufferSize];
                     if (SteamUtils.GetImageRGBA(friend.avatarImageIndex, buffer, bufferSize)) {
                        // Transform byte array into pixels
                        Color32[] pixels = new Color32[width * height];
                        for (int i = 0; i < bufferSize; i += 4) {
                           int pixel = i / 4;
                           int x = pixel % width;
                           int y = height - 1 - pixel / width;
                           pixels[x + y * width] = new Color32(buffer[i], buffer[i + 1], buffer[i + 2], buffer[i + 3]);
                        }

                        // Send the sprite to friends list
                        UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                           // Form the sprite
                           Texture2D tex = new Texture2D((int) width, (int) height);
                           tex.SetPixels32(pixels);
                           tex.Apply();
                           Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), Vector2.one * 0.5f);

                           // Send image to friend panel
                           FriendListPanel.self.receiveSteamFriendAvatar(capture, sprite);
                        });
                     }
                  }
               }
            }
         });
      }
   }

   public static string getFriendName (ulong steamId) {
      string result = "";
      if (SteamManager.Initialized) {
         result = SteamFriends.GetFriendPersonaName(new CSteamID(steamId));
      }

      if (string.IsNullOrWhiteSpace(result)) {
         result = "Unknown";
      }

      return result;
   }

   public static void setSteamDisplayStatus (string status) {
      if (SteamManager.Initialized) {
         SteamFriends.SetRichPresence("steam_display", status);
      }
   }

   public static void activateSteamOverlay () {
      if (SteamManager.Initialized) {
         SteamFriends.ActivateGameOverlay("Friends");
      }
   }


   public static void inviteFriendToGame (SteamFriendData friend) {
      // Inviting user to friend
      SteamFriends.InviteUserToGame(new CSteamID(friend.steamId), JOIN_FRIEND_PARAM_PREFIX + SteamUser.GetSteamID().m_SteamID.ToString());
   }

   public static void onGameRichPresenceJoinRequested (GameRichPresenceJoinRequested_t callback) {
      // Callback should have friends id embeded in connect string
      if (callback.m_rgchConnect.StartsWith(JOIN_FRIEND_PARAM_PREFIX)) {
         if (ulong.TryParse(callback.m_rgchConnect.Replace(JOIN_FRIEND_PARAM_PREFIX, ""), out ulong id)) {
            if (id > 0) {
               if (Global.player != null) {
                  Global.player.rpc.Cmd_WarpToFriend(id);
               }
            }
         }
      }
   }

   private void onNewUrlLaunchParameters (NewUrlLaunchParameters_t callback) {
      tryJoinUserFromLaunchParameters();
   }

   private bool tryJoinUserFromLaunchParameters () {
      CSteamID friendId = new CSteamID(0);
      string connectParam = SteamApps.GetLaunchQueryParam("connect");

      if (string.IsNullOrWhiteSpace(connectParam)) {
         // Try command line if we couldn't extract from steam connect
         SteamApps.GetLaunchCommandLine(out string launchCmd, 1000);
         if (tryGetJoinFriendIdFromCmd(launchCmd, out ulong id)) {
            friendId = new CSteamID(id);
         } else if (tryGetJoinFriendIdFromCmd(System.Environment.CommandLine, out id)) {
            friendId = new CSteamID(id);
         }
      }

      if (friendId.m_SteamID == 0 || !friendId.IsValid()) {
         return false;
      }

      if (SteamFriends.GetFriendRelationship(friendId) != EFriendRelationship.k_EFriendRelationshipFriend) {
         D.log("Trying to connect to a user that is not our friend");
         return false;
      }

      // If user is in game, try warping him to friend, otherwise schedule this for later
      if (Global.player != null) {
         Global.player.rpc.Cmd_WarpToFriend(friendId.m_SteamID);
      } else {
         Global.joinSteamFriendID = friendId.m_SteamID;
      }

      return true;
   }

   private bool tryGetJoinFriendIdFromCmd (string cmd, out ulong id) {
      int parIndex = cmd.IndexOf(JOIN_FRIEND_PARAM_PREFIX);
      if (parIndex >= 0) {
         string valString = "";
         for (int i = parIndex + JOIN_FRIEND_PARAM_PREFIX.Length; i < cmd.Length; i++) {
            if (char.IsDigit(cmd[i])) {
               valString += cmd[i];
            } else {
               break;
            }
         }

         if (!string.IsNullOrWhiteSpace(valString)) {
            if (ulong.TryParse(valString, out ulong lid)) {
               id = lid;
               return true;
            }
         }
      }

      id = 0;
      return false;
   }

   [Server]
   public static void joinFriend (NetEntity joiner, ulong joineeSteamId) {
      ServerNetworkingManager.self.findUserLocationForSteamFriendJoin(joiner.userId, joineeSteamId);
   }

   [Server]
   public static void pickedFriendJoinLocation (int joinerUserId, UserLocationBundle location) {
      NetEntity joiner = EntityManager.self.getEntity(joinerUserId);
      if (joiner == null) {
         return;
      }

      if (!canPlayerJoinFriend(joinerUserId, location)) {
         // Let user know if we can't join friend
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, joiner, "Can't access friend's location via Steam (not in Town)");
      } else {
         joiner.admin.forceWarpToLocation(joinerUserId, location);
      }
   }

   [Server]
   public static bool canPlayerJoinFriend (int userId, UserLocationBundle location) {
      if (userId <= 0 || location == null) {
         return false;
      }

      // Only allow joining in town
      if (AreaManager.self.tryGetAreaInfo(location.areaKey, out var map)) {
         if (map.specialType == Area.SpecialType.Town) {
            return true;
         }
      }

      return false;
   }

   #region Private Variables
   // Called when launch params are changed when game is still running
   protected Callback<NewUrlLaunchParameters_t> _newUrlLaunchParameters;

   // Called when a join game to friend is requested
   protected Callback<GameRichPresenceJoinRequested_t> _gameRichPresenceJoinRequested;

   // Lobby id which we are waiting for to get data
   protected ulong _waitingForLobbyId = 0;

   #endregion
}
