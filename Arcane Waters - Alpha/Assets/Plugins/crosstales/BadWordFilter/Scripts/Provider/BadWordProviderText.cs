using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

namespace Crosstales.BWF.Provider
{
    /// <summary>Text-file based bad word provider.</summary>
    [HelpURL("https://www.crosstales.com/media/data/assets/badwordfilter/api/class_crosstales_1_1_b_w_f_1_1_provider_1_1_bad_word_provider_text.html")]
    public class BadWordProviderText : BadWordProvider
    {

        #region Implemented methods

        public override void Load()
        {
            base.Load();

            if (Sources != null)
            {
                loading = true;

                if (Util.Helper.isEditorMode)
                {
#if UNITY_EDITOR
                    foreach (Data.Source source in Sources)
                    {
                        if (source != null)
                        {
                            if (source.Resource != null)
                            {
                                loadResourceInEditor(source);
                            }

                            if (!string.IsNullOrEmpty(source.URL))
                            {
                                loadWebInEditor(source);
                            }
                        }
                    }

                    init();
#endif
                }
                else
                {
                    foreach (Data.Source source in Sources)
                    {
                        if (source != null)
                        {
                            if (source.Resource != null)
                            {
                                StartCoroutine(loadResource(source));
                            }

                            if (!string.IsNullOrEmpty(source.URL))
                            {
                                StartCoroutine(loadWeb(source));
                            }
                        }
                    }
                }
            }
        }

        public override void Save()
        {
            Debug.LogWarning("Save not implemented!");
        }

        #endregion


        #region Private methods

        private IEnumerator loadWeb(Data.Source src)
        {
            string uid = System.Guid.NewGuid().ToString();
            coRoutines.Add(uid);

            if (!string.IsNullOrEmpty(src.URL))
            {
                using (UnityWebRequest www = UnityWebRequest.Get(src.URL.Trim()))
                {
#if UNITY_2017_1_OR_NEWER
                    www.timeout = 5;
#endif
#if UNITY_2017_2_OR_NEWER
                    www.downloadHandler = new DownloadHandlerBuffer();
                    yield return www.SendWebRequest();
#else
                    yield return www.Send();
#endif

#if UNITY_2017_1_OR_NEWER
                if (!www.isHttpError && !www.isNetworkError)
#else
                    if (string.IsNullOrEmpty(www.error))
#endif
                    {
                        System.Collections.Generic.List<string> list = Util.Helper.SplitStringToLines(www.downloadHandler.text);

                        yield return null;

                        if (list.Count > 0)
                        {
                            badwords.Add(new Model.BadWords(src, list));
                        }
                        else
                        {
                            Debug.LogWarning("Source: '" + src.URL + "' does not contain any active bad words!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Could not load source: '" + src.URL + "'" + System.Environment.NewLine + www.error + System.Environment.NewLine + "Did you set the correct 'URL'?");
                    }
                }
            }
            else
            {
                Debug.LogWarning("'URL' is null or empty!" + System.Environment.NewLine + "Please add a valid URL.");
            }

            coRoutines.Remove(uid);

            if (loading && coRoutines.Count == 0)
            {
                loading = false;
                init();
            }
        }

        private IEnumerator loadResource(Data.Source src)
        {
            string uid = System.Guid.NewGuid().ToString();
            coRoutines.Add(uid);

            if (src.Resource != null)
            {
                System.Collections.Generic.List<string> list = Util.Helper.SplitStringToLines(src.Resource.text);

                yield return null;

                if (list.Count > 0)
                {
                    badwords.Add(new Model.BadWords(src, list));
                }
                else
                {
                    Debug.LogWarning("Resource: '" + src.Resource + "' does not contain any active bad words!");
                }
            }
            else
            {
                Debug.LogWarning("Resource field 'Source' is null or empty!" + System.Environment.NewLine + "Please add a valid resource.");
            }

            coRoutines.Remove(uid);

            if (loading && coRoutines.Count == 0)
            {
                loading = false;
                init();
            }
        }

        #endregion


        #region Editor-only methods

#if UNITY_EDITOR

        private void loadWebInEditor(Data.Source src)
        {
            if (!string.IsNullOrEmpty(src.URL))
            {
                try
                {
                    System.Net.ServicePointManager.ServerCertificateValidationCallback = Util.Helper.RemoteCertificateValidationCallback;

                    using (System.Net.WebClient client = new Common.Util.CTWebClient())
                    {
                        string content = client.DownloadString(src.URL.Trim());

                        System.Collections.Generic.List<string> list = Util.Helper.SplitStringToLines(content);

                        if (list.Count > 0)
                        {
                            badwords.Add(new Model.BadWords(src, list));
                        }
                        else
                        {
                            Debug.LogWarning("Source: '" + src.URL + "' does not contain any active bad words!");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("Could not load source: '" + src.URL + "'" + System.Environment.NewLine + ex + System.Environment.NewLine + "Did you set the correct 'URL'?");
                }
            }
            else
            {
                Debug.LogWarning("'URL' is null or empty!" + System.Environment.NewLine + "Please add a valid URL.");
            }

            //Debug.Log("Source: '" + src.URL + "' loaded");
        }

        private void loadResourceInEditor(Data.Source src)
        {
            if (src.Resource != null)
            {
                System.Collections.Generic.List<string> list = Util.Helper.SplitStringToLines(src.Resource.text);

                if (list.Count > 0)
                {
                    badwords.Add(new Model.BadWords(src, list));
                }
                else
                {
                    Debug.LogWarning("Resource: '" + src.Resource + "' does not contain any active bad words!");
                }
            }
            else
            {
                Debug.LogWarning("Resource field 'Source' is null or empty!" + System.Environment.NewLine + "Please add a valid resource.");
            }
        }

#endif

        #endregion
    }
}
// © 2016-2019 crosstales LLC (https://www.crosstales.com)