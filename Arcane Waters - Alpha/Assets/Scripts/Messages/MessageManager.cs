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
      NetworkClient.RegisterHandler<CharacterEquipmentMessage>(ClientMessageManager.On_EquipmentList);
      NetworkClient.RegisterHandler<LogInCompleteMessage>(ClientMessageManager.On_LoginIsComplete);
      NetworkClient.RegisterHandler<StoreMessage>(ClientMessageManager.On_Store);
      NetworkClient.RegisterHandler<EquipMessage>(ClientMessageManager.On_Equip);
      NetworkClient.RegisterHandler<DisconnectMessage>(ClientMessageManager.On_FailedToConnectToServer);
   }

   public static void registerServerHandlers () {
      NetworkServer.RegisterHandler<LogInUserMessage>(ServerMessageManager.On_LogInUserMessage);
      NetworkServer.RegisterHandler<CreateUserMessage>(ServerMessageManager.On_CreateUserMessage);
      NetworkServer.RegisterHandler<DeleteUserMessage>(ServerMessageManager.On_DeleteUserMessage);
   }

   #region Private Variables

   #endregion
}
