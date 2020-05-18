#if NUBIS
using UnityEngine;
using System;


public class NubisLogger
{
   public static bool i (string message) {
      if (!System.IO.File.Exists(NubisConfiguration.LogFilePath())) return false;
      string msg = string.Format("{0} - {1}\r\n", DateTime.Now.ToUniversalTime().ToString(), message);
      try {
         Debug.Log(message);
         System.IO.File.AppendAllText(NubisConfiguration.LogFilePath(), msg);
         Console.WriteLine(msg);
         System.Diagnostics.Debug.WriteLine(msg);
      } catch {
      }
      return true;
   }

   public static bool e (Exception ex) {
      return i(ex.Message + " | STACKTRACE: " + ex.StackTrace);
   }

}
#endif