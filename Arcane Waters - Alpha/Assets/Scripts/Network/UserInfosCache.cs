using System.Collections.Generic;
using System;

public class UserInfosCache
{
   #region Public Variables

   // The lifetime of the cache in seconds
   public static readonly TimeSpan CACHE_LIFETIME = new TimeSpan(0, 1, 0);

   // The cached set of user infos
   public static IEnumerable<UserInfo> cachedUserInfos = null;

   // The cached timestamp
   public static DateTime? timestamp = null;

   #endregion

   public static bool needsUpdate () {
      if (cachedUserInfos == null || timestamp == null || (DateTime.Now - CACHE_LIFETIME) > timestamp.Value) {
         return true;
      }

      return false;
   }

   public static void updateCache(IEnumerable<UserInfo> userInfos) {
      cachedUserInfos = userInfos;
      timestamp = DateTime.Now;
   }

   public static IEnumerable<UserInfo> getCache () {
      return cachedUserInfos;
   }
}
