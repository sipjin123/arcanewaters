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

   // Key we use to set lobby owner metadata
   public const string LOBBY_OWNER_KEY = "lobby-owner";

   #endregion

   private void OnEnable () {
      if (!SteamManager.Initialized) {
         if (Util.isCloudBuild()) {
            D.error("Steam Manager is not initialized!");
         }
         return;

      }

      // Hook up callbacks
      _gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(onGameLobbyJoinRequested);
      _newUrlLaunchParameters = Callback<NewUrlLaunchParameters_t>.Create(onNewUrlLaunchParameters);
      _lobbyCreated = Callback<LobbyCreated_t>.Create(onLobbyCreated);
      _lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(onLobbyDataUpdate);

      // Check if this game was launched from steam command line
      if (!tryJoinUserFromLaunchParameters()) {
         // If we didn't find a lobby in launch parameters, we may already be automatically added to one
         tryConnectToCurrentLobbyOwnerOnStartup();
      }
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

   private void onGameLobbyJoinRequested (GameLobbyJoinRequested_t callback) {
      // If user is in game, try warping him to friend
      if (Global.player != null) {
         Global.player.rpc.Cmd_WarpToFriend(callback.m_steamIDFriend.m_SteamID);
      }
   }

   public static void inviteFriendToGame (SteamFriendData friend) {
      SteamFriends.InviteUserToGame(new CSteamID(friend.steamId), "");
   }

   private void onLobbyCreated (LobbyCreated_t callback) {
      D.log("Lobby was created " + callback.m_ulSteamIDLobby + " " + callback.m_eResult.ToString());
      CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
      if (lobbyId.IsLobby() && lobbyId.IsValid()) {
         D.log("Setting lobby metadata");
         SteamMatchmaking.SetLobbyData(lobbyId, LOBBY_OWNER_KEY, SteamUser.GetSteamID().ToString());
      }
   }

   private void onNewUrlLaunchParameters (NewUrlLaunchParameters_t callback) {
      tryJoinUserFromLaunchParameters();
   }

   private void tryConnectToCurrentLobbyOwnerOnStartup () {
      D.log("Checking current game info for lobby");
      if (SteamFriends.GetFriendGamePlayed(SteamUser.GetSteamID(), out FriendGameInfo_t gameInfo)) {
         if (gameInfo.m_steamIDLobby.IsValid()) {
            D.log("Found lobby for user from current game info");
            _waitingForLobbyId = gameInfo.m_steamIDLobby.m_SteamID;
            SteamMatchmaking.RequestLobbyData(gameInfo.m_steamIDLobby);
         }
      } else {
         D.log("Could not get game played data for user " + SteamUser.GetSteamID().m_SteamID);
      }
   }

   private bool tryJoinUserFromLaunchParameters () {
      CSteamID lobbyId = new CSteamID(0);
      string lobbyIdString = SteamApps.GetLaunchQueryParam("+connect_lobby");

      D.log("Steam join LobbyIdString: " + lobbyIdString);
      if (!string.IsNullOrWhiteSpace(lobbyIdString)) {
         if (ulong.TryParse(lobbyIdString, out ulong lobbyIdUlong)) {
            lobbyId = new CSteamID(lobbyIdUlong);
            D.log("Found lobby id from steam launch " + lobbyIdUlong);
         } else {
            D.error("Unable to parse lobby id");
         }
      }

      if (lobbyId.m_SteamID == 0) {
         // Try command line if we couldn't extract from steam connect
         string cmd = System.Environment.CommandLine;
         int parIndex = cmd.IndexOf("+connect_lobby ");

         // If we found the parameter, extract it's value
         if (parIndex >= 0) {
            D.log("Found +connect_lobby in command");
            string valString = "";
            for (int i = parIndex + "+connect_lobby ".Length; i < cmd.Length; i++) {
               if (char.IsDigit(cmd[i])) {
                  valString += cmd[i];
               } else {
                  break;
               }
            }
            D.log("Resulting connect_lobby target id: " + valString);
            if (!string.IsNullOrWhiteSpace(valString)) {
               if (ulong.TryParse(valString, out ulong lid)) {
                  lobbyId = new CSteamID(lid);
                  D.log("Found lobby id from command line " + lid);
               }
            }
         }

      }

      if (lobbyId.m_SteamID == 0 || !lobbyId.IsLobby() || !lobbyId.IsValid()) {
         return false;
      }

      _waitingForLobbyId = lobbyId.m_SteamID;
      SteamMatchmaking.RequestLobbyData(lobbyId);
      return true;
   }

   private void onLobbyDataUpdate (LobbyDataUpdate_t callback) {
      D.log("On lobby data update");
      if (callback.m_ulSteamIDLobby == 0 || callback.m_ulSteamIDLobby != _waitingForLobbyId) {
         return;
      }
      D.log("On lobby data update - valid lobby id");
      // The lobby owner is the target user we want to join
      string lobbyOwnerString = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), LOBBY_OWNER_KEY);
      D.log("Lobby owner string: " + lobbyOwnerString);
      CSteamID lobbyOwnerId = CSteamID.Nil;
      if (ulong.TryParse(lobbyOwnerString, out ulong ownerIdLong)) {
         lobbyOwnerId = new CSteamID(ownerIdLong);
      }

      if (!lobbyOwnerId.IsValid()) {
         D.error("Steam target ID is not valid " + lobbyOwnerId.m_SteamID);
         return;
      }

      // If this lobby's owner is not use, leave it - we only joined lobby to access friend's location
      if (lobbyOwnerId.m_SteamID != SteamUser.GetSteamID().m_SteamID) {
         D.log("Leaving lobby");
         SteamMatchmaking.LeaveLobby(new CSteamID(callback.m_ulSteamIDLobby));
      }

      // If user is in game, try warping him to friend, otherwise schedule this for later
      if (Global.player != null) {
         Global.player.rpc.Cmd_WarpToFriend(lobbyOwnerId.m_SteamID);
      } else {
         Global.joinSteamFriendID = lobbyOwnerId.m_SteamID;
      }
   }

   [Server]
   public static void joinFriend (NetEntity joiner, ulong joineeSteamId) {
      ServerNetworkingManager.self.findUserLocationForSteamFriendJoin(joiner.userId, joineeSteamId);
   }

   [Server]
   public static void pickedFriendJoinLocation (int joinerUserId, UserLocationBundle location) {
      NetEntity joiner = EntityManager.self.getEntity(joinerUserId);
      if (joiner == null) {
         D.log("User that wants to join a friend is null");
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

   // Callback used when user presses 'Join' on his steam friends
   protected Callback<GameLobbyJoinRequested_t> _gameLobbyJoinRequested;

   // Called when launch params are changed when game is still running
   protected Callback<NewUrlLaunchParameters_t> _newUrlLaunchParameters;

   // Called when a lobby is created
   protected Callback<LobbyCreated_t> _lobbyCreated;

   // Called when lobby data is updated
   protected Callback<LobbyDataUpdate_t> _lobbyDataUpdate;

   // Lobby id which we are waiting for to get data
   protected ulong _waitingForLobbyId = 0;

   #endregion
}
