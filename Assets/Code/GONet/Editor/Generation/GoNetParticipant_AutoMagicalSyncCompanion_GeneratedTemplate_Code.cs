using GONet.Generation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Code.GONet.Editor.Generation
{
    public partial class GONetParticipant_AutoMagicalSyncCompanion_GeneratedTemplate
    {
        const string UNDIE = "_";
        internal string ClassName => string.Concat(nameof(GONetParticipant_AutoMagicalSyncCompanion_Generated), UNDIE, uniqueEntry.codeGenerationId);

        GONetParticipant_ComponentsWithAutoSyncMembers uniqueEntry;

        public GONetParticipant_AutoMagicalSyncCompanion_GeneratedTemplate(GONetParticipant_ComponentsWithAutoSyncMembers uniqueEntry)
        {
            this.uniqueEntry = uniqueEntry;
        }
    }
}
