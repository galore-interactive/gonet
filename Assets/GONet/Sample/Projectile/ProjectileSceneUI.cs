/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using GONet;
using UnityEngine;

public class ProjectileSceneUI : MonoBehaviour
{
    private const string YES = "Yes";
    private const string NO = "No";

    public TMPro.TextMeshProUGUI time;
    public TMPro.TextMeshProUGUI isServer;
    public TMPro.TextMeshProUGUI isClient;
    bool wasServerSpawned;

    private void Update()
    {
        if (time)
        {
            time.text = GONetMain.Time.ElapsedSeconds.ToString();
        }

        if (isServer)
        {
            const string ALT_S = "<press left Alt+S>";
            const string SERVER_GO = "GONetSampleServer(Clone)";
            wasServerSpawned |= GameObject.Find(SERVER_GO);

            isServer.text = GONetMain.IsServer ? (wasServerSpawned ? YES : ALT_S) : NO;
        }

        if (isClient)
        {
            isClient.text = GONetMain.IsClient ? YES : NO;
        }
    }
}
