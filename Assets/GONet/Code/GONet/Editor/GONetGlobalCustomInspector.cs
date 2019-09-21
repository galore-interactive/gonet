using UnityEditor;

namespace GONet.Editor
{
    [CustomEditor(typeof(GONetGlobal))]
    public class GONetGlobalCustomInspector : GNPListCustomInspector
    {
        GONetGlobal targetGNG;

        private void OnEnable()
        {
            targetGNG = (GONetGlobal)target;

            /* this will not work since it runs on a non-main unity thread...darn...will use RequiresConstantRepaint instead
            var subscription = GONetMain.EventBus.Subscribe<GONetParticipantStartedEvent>(_ => Repaint());
            subscription.SetSubscriptionPriority(short.MaxValue); // setting priority to run last so the GONetGlobal instance has a chance to add the new GNP to its list before we repaint here

            var subscription2 = GONetMain.EventBus.Subscribe<GONetParticipantDisabledEvent>(_ => Repaint());
            subscription2.SetSubscriptionPriority(short.MaxValue); // setting priority to run last so the GONetGlobal instance has a chance to remove the GNP from its list before we repaint here
            */
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            const string ALL = "ALL Enabled GONetParticipants:";
            DrawGNPList(targetGNG.EnabledGONetParticipants, ALL, false);
        }
    }
}
