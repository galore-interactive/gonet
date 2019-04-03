using System.Collections.Generic;

namespace GONet
{
    public interface IGONetEvent
    {
    }

    public class AutoMagicalSync_ValueChangesMessage : IGONetEvent
    {
        List<GONetMain.AutoMagicalSync_ValueMonitoringSupport> changes;
    }

}
