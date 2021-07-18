using System.Collections.Generic;

public class RemoteSettingCollection
{
   #region Public Variables

   // The set of settings included in this collection
   public List<RemoteSetting> settings = new List<RemoteSetting>();

   #endregion

   public void addSetting(RemoteSetting setting) {
      RemoteSetting probeSetting = settings.Find(_ => _.name == setting.name);

      if (probeSetting != null) {
         return;
      }

      settings.Add(setting);
   }

   public RemoteSetting getSetting(string settingName) {
      return settings.Find(_ => _.name == settingName);
   }

   #region Private Variables

   #endregion
}
