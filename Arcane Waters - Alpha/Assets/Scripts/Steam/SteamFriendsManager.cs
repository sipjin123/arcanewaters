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

      // Check if this game was launched from steam command line
      handleLaunchParameters();
   }

   private void Update () {
      if (KeyUtils.GetKeyDown(UnityEngine.InputSystem.Key.Numpad8)) {
         if (SteamManager.Initialized) {
            SteamFriends.ActivateGameOverlay("");
         } else {
            D.error("STEAM NOT INITIALIZED");
         }
      }

      if (KeyUtils.GetKeyDown(UnityEngine.InputSystem.Key.Numpad9)) {
         SteamManager.testTryInitializeUnityEditor();
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

   private void onGameLobbyJoinRequested (GameLobbyJoinRequested_t callback) {
      // If user is in game, try warping him to friend
      if (Global.player != null) {
         Global.player.rpc.Cmd_WarpToFriend(callback.m_steamIDFriend.m_SteamID);
      }
   }

   public static void inviteFriendToGame (SteamFriendData friend) {
      SteamFriends.InviteUserToGame(new CSteamID(friend.steamId), "");
   }

   private void onNewUrlLaunchParameters (NewUrlLaunchParameters_t callback) {
      handleLaunchParameters();
   }

   private void handleLaunchParameters () {
      string lobbyIdString = SteamApps.GetLaunchQueryParam("+connect_lobby");
      D.log("Steam join LobbyIdString: " + lobbyIdString);
      if (!string.IsNullOrWhiteSpace(lobbyIdString)) {
         if (ulong.TryParse(lobbyIdString, out ulong lobbyIdUlong)) {
            CSteamID lobbyId = new CSteamID(lobbyIdUlong);

            // The lobby owner is the target user we want to join
            CSteamID lobbyOwnerId = SteamMatchmaking.GetLobbyOwner(lobbyId);

            if (!lobbyOwnerId.IsValid()) {
               D.error("Steam target ID is not valid");
               return;
            }

            // If user is in game, try warping him to friend, otherwise schedule this for later
            if (Global.player != null) {
               Global.player.rpc.Cmd_WarpToFriend(lobbyOwnerId.m_SteamID);
            } else {
               Global.joinSteamFriendID = lobbyOwnerId.m_SteamID;
            }

         } else {
            D.error("Unable to parse lobby id");
         }
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
         return;
      }

      joiner.admin.forceWarpToLocation(joinerUserId, location);
   }

   [Server]
   public static bool canPlayerJoinFriend (int userId, NetEntity friend) {
      if (userId <= 0 || friend == null) {
         return false;
      }

      // Don't allow to join if friend is in open world
      if (WorldMapManager.isWorldMapArea(friend.areaKey)) {
         return false;
      }

      return true;
   }

   #region Private Variables

   // Callback used when user presses 'Join' on his steam friends
   protected Callback<GameLobbyJoinRequested_t> _gameLobbyJoinRequested;

   // Called when launch params are changed when game is still running
   protected Callback<NewUrlLaunchParameters_t> _newUrlLaunchParameters;

   #endregion
}
