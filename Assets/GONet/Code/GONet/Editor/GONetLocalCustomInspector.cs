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

using UnityEditor;

namespace GONet.Editor
{
    [CustomEditor(typeof(GONetLocal))]
    public class GONetLocalCustomInspector : GNPListCustomInspector
    {
        GONetLocal targetGNL;

        private void OnEnable()
        {
            targetGNL = (GONetLocal)target;

            /* this will not work since it runs on a non-main unity thread...darn...will use RequiresConstantRepaint instead
            var subscription = GONetMain.EventBus.Subscribe<GONetParticipantStartedEvent>(_ => Repaint());
            subscription.SetSubscriptionPriority(short.MaxValue); // setting priority to run last so the GONetGlobal instance has a chance to add the new GNP to its list before we repaint here

            var subscription2 = GONetMain.EventBus.Subscribe<SyncEvent_GONetParticipant_OwnerAuthorityId>(_ => Repaint());
            subscription2.SetSubscriptionPriority(short.MaxValue); // setting priority to run last so the GONetGlobal instance has a chance to (possibly) add/remove the GNP to/from its list before we repaint here

            var subscription3 = GONetMain.EventBus.Subscribe<GONetParticipantDisabledEvent>(_ => Repaint());
            subscription3.SetSubscriptionPriority(short.MaxValue); // setting priority to run last so the GONetGlobal instance has a chance to remove the GNP from its list before we repaint here
            */
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            const string MY = "*My Enabled GONetParticipants:";
            DrawGNPList(targetGNL.MyEnabledGONetParticipants, MY, true);
        }
    }
}
