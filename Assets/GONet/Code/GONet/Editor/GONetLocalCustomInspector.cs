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
