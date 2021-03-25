using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Text;
using System;
using System.Security.Cryptography;

public class DBZipTester : MonoBehaviour {
   #region Public Variables

   public static string ServerDataText = "C:/TestText/ServerDataText.txt";
   public static string ClientDataText = "C:/TestText/ClientDataText.txt";
   public static string ClientDataZip = "C:/TestText/ClientDataZip.zip";
   public static string NewServerDataZip = "C:/TestText/ServerTestDataZip.zip";
   public static string ServerDataZip = "C:/XmlZipFile/ServerDataZip.zip";

   #endregion

   private void Update () {
      if (SystemInfo.deviceName == NubisDataFetchTest.DEVICE_NAME1) {
      }
   }

   #region Private Variables

   #endregion
}
