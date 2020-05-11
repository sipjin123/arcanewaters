#define NUBIS
#if NUBIS
using UnityEngine;
using System;

public class NubisLogger
{
   public static void i (string message) {
      Debug.Log(message);
   }
   public static void e (Exception ex) {
      i(ex.Message + "STACKTRACE: " + ex.StackTrace);
   }
}

#endif