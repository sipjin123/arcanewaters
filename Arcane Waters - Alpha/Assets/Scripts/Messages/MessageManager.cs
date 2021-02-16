using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class MessageManager : MonoBehaviour {
   #region Public Variables

   #endregion

   public static void registerClientHandlers () {
      NetworkClient.RegisterHandler<RedirectMessage>(ClientMessageManager.On_Redirect);
      NetworkClient.RegisterHandler<ErrorMessage>(ClientMessageManager.On_ErrorMessage);
      NetworkClient.RegisterHandler<ConfirmMessage>(ClientMessageManager.On_ConfirmMessage);
      NetworkClient.RegisterHandler<CharacterListMessage>(ClientMessageManager.On_CharacterList);
      NetworkClient.RegisterHandler<LogInCompleteMessage>(ClientMessageManager.On_LoginIsComplete);
      NetworkClient.RegisterHandler<CharacterCreationValidMessage>(ClientMessageManager.On_CharacterCreationValid);
      NetworkClient.RegisterHandler<StoreMessage>(ClientMessageManager.On_Store);
      NetworkClient.ReplaceHandler<DisconnectMessage>(ClientMessageManager.On_FailedToConnectToServer);
   }

   public static void unregisterClientHandlers () {      
      NetworkClient.UnregisterHandler<RedirectMessage>();
      NetworkClient.UnregisterHandler<ErrorMessage>();
      NetworkClient.UnregisterHandler<ConfirmMessage>();
      NetworkClient.UnregisterHandler<CharacterListMessage>();
      NetworkClient.UnregisterHandler<LogInCompleteMessage>();
      NetworkClient.UnregisterHandler<CharacterCreationValidMessage>();
      NetworkClient.UnregisterHandler<StoreMessage>();
      NetworkClient.UnregisterHandler<DisconnectMessage>();
      
   }

   public static void registerServerHandlers () {
      NetworkServer.RegisterHandler<LogInUserMessage>(ServerMessageManager.On_LogInUserMessage);
      NetworkServer.RegisterHandler<CreateUserMessage>(ServerMessageManager.On_CreateUserMessage);
      NetworkServer.RegisterHandler<DeleteUserMessage>(ServerMessageManager.On_DeleteUserMessage);
   }

   public static void unregisterServerHandlers () {
      NetworkServer.UnregisterHandler<LogInUserMessage>();
      NetworkServer.UnregisterHandler<CreateUserMessage>();
      NetworkServer.UnregisterHandler<DeleteUserMessage>();
   }

   #region Private Variables

   #endregion
}
