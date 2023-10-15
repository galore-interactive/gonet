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

using GONet.Generation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.GONet.Code.GONet.Editor.Generation
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
