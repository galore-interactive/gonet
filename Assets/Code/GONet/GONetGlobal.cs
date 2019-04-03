using UnityEngine;

namespace GONet
{
    /// <summary>
    /// Very important, in fact required, that this get added to one and only one <see cref="GameObject"/> in the first scene loaded in your game.
    /// This is where all the links into Unity life cycle stuffs start for GONet at large.
    /// </summary>
    [RequireComponent(typeof(GONetSessionContext))] // NOTE: requiring GONetSessionContext will thereby get the DontDestroyOnLoad behavior
    public sealed class GONetGlobal : MonoBehaviour
    {
        #region TODO this should be configurable/set elsewhere potentially AFTER loading up and depending on other factors like match making etc...

        //public string serverIP;

        //public int serverPort;

        #endregion

        private void Awake()
        {
            GONetMain.GlobalSessionContext = gameObject.GetComponent<GONetSessionContext>();
        }

        private void Update()
        {
            GONetMain.Update();
        }

        private void OnApplicationQuit()
        {
            GONetMain.Shutdown();
        }
    }
}
