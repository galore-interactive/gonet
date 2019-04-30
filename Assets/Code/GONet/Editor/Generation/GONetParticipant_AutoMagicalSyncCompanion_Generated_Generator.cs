using Assets.Code.GONet.Editor.Generation;
using GONet.Utils;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GONet.Generation
{
    [InitializeOnLoad] // ensure class initializer is called whenever scripts recompile
    public static class GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator
    {
        /// <summary>
        /// IMPORTANT: This is duplicate thought as <see cref="GONetParticipant_ComponentsWithAutoSyncMembers"/>...TODO: need to merge these!
        /// 
        /// Each item in the outer list maps to a single <see cref="GameObject"/> found in a Unity scene and/or prefab that has a <see cref="GONetParticipant"/> installed on it AND also has
        /// one or more <see cref="MonoBehaviour"/> instances installed on it that have one or more fields/properties decorated with <see cref="GONetAutoMagicalSyncAttribute"/>.
        /// 
        /// The index of the outer item matches to the generated class name suffix (e.g., GONetParticipant_AutoMagicalSyncCompanion_Generated_15).
        /// The index of the inner item matches to the place in order of that attribute being encountered during discovery/enumeration (has to be deterministic).
        /// </summary>
        static List<List<AutoMagicalSyncAttribute_GenerationSupport>> gonetParticipantCombosEncountered = new List<List<AutoMagicalSyncAttribute_GenerationSupport>>();
        internal const string GENERATION_FILE_PATH = "Assets/Code/GONet/Generation/";
        internal const string GENERATED_FILE_PATH = GENERATION_FILE_PATH + "Generated/";
        const string FILE_SUFFIX = ".cs";

        static GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator()
        {
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;

            GONetParticipant.EditorOnlyDefaultContructor += GONetParticipant_EditorOnlyDefaultContructor;
            GONetParticipant.EditorOnlyReset += GONetParticipant_EditorOnlyReset;
            GONetParticipant.EditorOnlyAwake += GONetParticipant_EditorOnlyAwake;
            GONetParticipant.EditorOnlyOnDestroy += GONetParticipant_EditorOnlyOnDestroy;

#if UNITY_2018_1_OR_NEWER
            EditorApplication.projectChanged += OnProjectChanged;
#else
            EditorApplication.projectWindowChanged += OnProjectChanged;
#endif

            EditorSceneManager.sceneSaved += EditorSceneManager_sceneSaved;
        }

        internal static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string str in importedAssets)
            {
                Debug.Log("Reimported Asset: " + str);
            }
            foreach (string str in deletedAssets)
            {
                Debug.Log("Deleted Asset: " + str);
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
            }
        }

        private static void OnEditorPlayModeStateChanged(PlayModeStateChange state)
        {
            bool canASSume_GoingFrom_Editer_To_Play = !EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode; // see http://wiki.unity3d.com/index.php/SaveOnPlay for the idea here!
            if (canASSume_GoingFrom_Editer_To_Play)
            {
                GONetLog.Debug("[DREETS] entering play mode...not there yet, but about to be....about to generate as that needs to happen before playing!");
                DoAllTheGenerationStuffs();
            }
        }

        private static void EditorSceneManager_sceneSaved(Scene scene)
        {
            GONetLog.Debug("[DREETS] saved scene: " + scene.name);
            DoAllTheGenerationStuffs();
        }

        private static void OnProjectChanged()
        {
            if (IsPrefab(Selection.activeGameObject))
            {
                GONetParticipant gonetParticipant_onPrefab = Selection.activeGameObject.GetComponent<GONetParticipant>();
                if (gonetParticipant_onPrefab != null)
                {
                    OnGONetParticipantPrefabCreated_Editor(gonetParticipant_onPrefab);
                }
            }

            // TODO need to figure out what to check in order to call OnGONetParticipantPrefabDeleted_Editor
        }

        static readonly List<GONetParticipant> gonetParticipants_prefabsCreatedSinceLastGeneratorRun = new List<GONetParticipant>();

        private static void OnGONetParticipantPrefabCreated_Editor(GONetParticipant gonetParticipant_onPrefab)
        {
            GONetLog.Debug("[DREETS] *********GONetParticipant PREFAB*********** created.");
            gonetParticipants_prefabsCreatedSinceLastGeneratorRun.Add(gonetParticipant_onPrefab);
        }

        private static void OnGONetParticipantPrefabDeleted_Editor(GONetParticipant gonetParticipant_onPrefab)
        {
            GONetLog.Debug("[DREETS] *********GONetParticipant PREFAB*********** deleted.");
        }

        private static void GONetParticipant_EditorOnlyDefaultContructor(GONetParticipant gonetParticipant)
        {

        }

        private static bool IsPrefab(UnityEngine.Object @object)
        {
            bool isPrefab = @object != null &&
#if UNITY_2018_3_OR_NEWER
                PrefabUtility.GetCorrespondingObjectFromSource(@object) != null;
#else
                PrefabUtility.GetPrefabParent(@object.gameObject) == null && PrefabUtility.GetPrefabObject(@object.gameObject) != null;
#endif
            return isPrefab;
        }

        private static void GONetParticipant_EditorOnlyAwake(GONetParticipant gonetParticipant)
        {
            if (IsPrefab(gonetParticipant))
            {
                GONetLog.Debug("[DREETS] *********PREFAB*********** GONetParticipant straight been woke!");
            }
            else
            {
                GONetLog.Debug("[DREETS] woke!");
            }
        }

        private static void GONetParticipant_EditorOnlyReset(GONetParticipant gonetParticipant)
        {
            GONetLog.Debug("[DREETS] added GONetParticipant");
        }

        private static void GONetParticipant_EditorOnlyOnDestroy(GONetParticipant gonetParticipant)
        {
            GONetLog.Debug("[DREETS] ***DESTROYED*** GONetParticipant....");
        }

        [DidReloadScripts]
        public static void OnScriptsReloaded()
        {

        }

        internal static void Reset_OnAddedToGameObjectForFirstTime_EditorOnly(GONetParticipant gonetParticipant)
        {

        }

        internal static void DoAllTheGenerationStuffs()
        {
            /* auto-sync code gen psuedo:

            // NOTE: This method is thought to be called when editor save scene called
            // 		 TODO: need to account for not saving scene and running from editor......that run needs the latest code gen and id mappings etc...TODO: look for callback for when going into play mode in editor

            -in static initializer that runs before any GONet stuff, attempt load hold all unique encapsulated data (snaps) from persistence
            -if snaps not found, create empty list/set to hold all unique encapsulated data
            -find all GONetParticipants (gos) // MUST include in all scene and all prefabs ==AND== MUST be deterministically ordered!
            -for each GONetParticipant in gos (go)
	            -find all [AutoMagicalSync]s on go (amss) // MUST be deterministically ordered!
	            -ask utility for unique snap // pass all info available, for current go and amss combo
		            -utility => if snap not found in snaps (i.e., no match any other snap layout/content 1-to-1), add snap to snaps and return snap
	            -annotate go with snap.id (likely matches the index inside snaps), replacing existing value if present (in private [SerializeField] i suppose)
                    -NOTE: see GONetParticipant.codeGenerationId
            -for each snap in snaps
	            -generate a C# class for auto-magical sync support (class name suffix is "_<snap.id>")
            -compile and save etc...
            -persist latest unique list/set of snaps (overwriting whatever was there previously)
            */

            List<GONetParticipant_ComponentsWithAutoSyncMembers> allUniqueSnapsForPersistence = LoadAllSnapsFromPersistence();
            allUniqueSnapsForPersistence.ForEach(x => GONetLog.Debug("from persistence x.codeGenerationId: " + x.codeGenerationId + " x.ComponentMemberNames_By_ComponentTypeFullName.Length: " + x.ComponentMemberNames_By_ComponentTypeFullName.Length));

            List<GONetParticipant> gonetParticipantsInOpenScenes = new List<GONetParticipant>();

            for (int iOpenScene = 0; iOpenScene < SceneManager.sceneCount; ++iOpenScene)
            {
                Scene scene = SceneManager.GetSceneAt(iOpenScene);
                GameObject[] rootGOs = scene.GetRootGameObjects();
                for (int iRootGO = 0 ; iRootGO < rootGOs.Length; ++iRootGO)
                {
                    GONetParticipant[] gonetParticipantsInOpenScene = rootGOs[iRootGO].GetComponentsInChildren<GONetParticipant>();
                    gonetParticipantsInOpenScenes.AddRange(gonetParticipantsInOpenScene);
                }

            }

            List<GONetParticipant_ComponentsWithAutoSyncMembers> possibleNewUniqueSnaps = new List<GONetParticipant_ComponentsWithAutoSyncMembers>();
            gonetParticipantsInOpenScenes.ForEach(gonetParticipant => possibleNewUniqueSnaps.Add(new GONetParticipant_ComponentsWithAutoSyncMembers(gonetParticipant)));
            // TODO add in stuff to possibleNewUniqueSnaps from gonetParticipants_prefabsCreatedSinceLastGeneratorRun before clearing it out below

            byte max_codeGenerationId = allUniqueSnapsForPersistence.Count > 0 ? allUniqueSnapsForPersistence.Max(x => x.codeGenerationId) : GONetParticipant.CodeGenerationId_Unset;
            GONetLog.Debug("max_codeGenerationId: " + max_codeGenerationId);
            GONetLog.Debug("gonetParticipantsInOpenScenes.count: " + gonetParticipantsInOpenScenes.Count);
            GONetLog.Debug("before allUniqueSnapsForPersistence.count: " + allUniqueSnapsForPersistence.Count);
            possibleNewUniqueSnaps.ForEach(possibleNew => {
                if (!allUniqueSnapsForPersistence.Contains(possibleNew, SnapComparer.Instance))
                {
                    possibleNew.codeGenerationId = ++max_codeGenerationId; // TODO need to account for any gaps in this list if some are removed at any point
                    GONetLog.Debug("just assigned codeGenerationId: " + possibleNew.codeGenerationId);
                    allUniqueSnapsForPersistence.Add(possibleNew);
                }
            });
            GONetLog.Debug("after allUniqueSnapsForPersistence.count: " + allUniqueSnapsForPersistence.Count);

            bool shouldSaveScene_weChangedCodeGenerationIds = false;
            { // make sure all GONetParticipants have assigned codeGenerationId, which is vitally important for game play runtime
                possibleNewUniqueSnaps.ForEach(possibleNew => {
                    var matchFromPersistence = allUniqueSnapsForPersistence.First(unique => SnapComparer.Instance.Equals(possibleNew, unique));
                    if (possibleNew.codeGenerationId                    != matchFromPersistence.codeGenerationId ||
                        possibleNew.gonetParticipant.codeGenerationId   != matchFromPersistence.codeGenerationId)
                    {
                        GONetLog.Debug("match found from persistence... BEFORE possibleNew.codeGenerationId: " + possibleNew.codeGenerationId);

                        possibleNew.codeGenerationId = matchFromPersistence.codeGenerationId;
                        { // cannot simply do the following: possibleNew.gonetParticipant.codeGenerationId = matchFromPersistence.codeGenerationId;
                            SerializedObject so = new SerializedObject(possibleNew.gonetParticipant);
                            so.Update();
                            so.FindProperty(nameof(GONetParticipant.codeGenerationId)).intValue = matchFromPersistence.codeGenerationId;
                            so.ApplyModifiedProperties();
                            //EditorSceneManager.MarkAllScenesDirty(); this causes endless loop
                        }

                        GONetLog.Debug("match found from persistence... AFTER possibleNew.codeGenerationId: " + possibleNew.codeGenerationId);

                        shouldSaveScene_weChangedCodeGenerationIds = true;
                    }
                    else
                    {
                        GONetLog.Debug("already matched??? rere");
                    }
                });
            }

            if (shouldSaveScene_weChangedCodeGenerationIds)
            {
                // TODO save scene, but gotta be careful we do not get into an endless cycle as the method we are in now is called when scene saved.
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                // TODO call this for stuff in gonetParticipants_prefabsCreatedSinceLastGeneratorRun: PrefabUtility.SavePrefabAsset()
            }

            { // Do generation stuffs? .... based on what has changed in scene since last save
                int count = allUniqueSnapsForPersistence.Count;
                for (int i = 0; i < count; ++i)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers one = allUniqueSnapsForPersistence[i];
                    GenerateClass(one);
                }

                byte ASSumedMaxCodeGenerationId = (byte)count;
                BobWad_Generated_Generator.GenerateClass(ASSumedMaxCodeGenerationId);

                AssetDatabase.SaveAssets(); // since we are generating the class that is the real thing of value here, ensure we also save the asset to match current state
                AssetDatabase.Refresh(); // get the Unity editor to recognize any new code just added and recompile it
            }

            { // clean up
                gonetParticipants_prefabsCreatedSinceLastGeneratorRun.Clear();

                DeleteAllSnapsFromPersistence();
                SaveAllSnapsToPersistence(allUniqueSnapsForPersistence);
            }
        }

        #region persistence

        const string SNAPS_FILE = GENERATION_FILE_PATH + "Unique_GONetParticipant_ComponentsWithAutoSyncMembers.bin";

        /// <summary>
        /// returns empty list if nothing persisted.
        /// </summary>
        private static List<GONetParticipant_ComponentsWithAutoSyncMembers> LoadAllSnapsFromPersistence()
        {
            if (File.Exists(SNAPS_FILE))
            {
                byte[] snapsFileBytes = File.ReadAllBytes(SNAPS_FILE);
                var l = SerializationUtils.DeserializeFromBytes<List<GONetParticipant_ComponentsWithAutoSyncMembers>>(snapsFileBytes);
                foreach (var ll in l)
                {
                    foreach (var lll in ll.ComponentMemberNames_By_ComponentTypeFullName)
                    {
                        foreach (var llll in lll.autoSyncMembers)
                        {
                            llll.PostDeserialize_InitAttribute(lll.componentTypeAssemblyQualifiedName);
                        }
                    }
                }
                return l;
            }
            else
            {
                return new List<GONetParticipant_ComponentsWithAutoSyncMembers>();
            }
        }

        private static void SaveAllSnapsToPersistence(List<GONetParticipant_ComponentsWithAutoSyncMembers> allSnaps)
        {
            if (!Directory.Exists(GENERATION_FILE_PATH))
            {
                Directory.CreateDirectory(GENERATION_FILE_PATH);
            }

            allSnaps.ForEach(x => GONetLog.Debug("SAVING to persistence x.codeGenerationId: " + x.codeGenerationId + " x.ComponentMemberNames_By_ComponentTypeFullName.Length: " + x.ComponentMemberNames_By_ComponentTypeFullName.Length));

            byte[] snapsFileBytes = SerializationUtils.SerializeToBytes(allSnaps);
            File.WriteAllBytes(SNAPS_FILE, snapsFileBytes);
        }

        private static void DeleteAllSnapsFromPersistence()
        {
            // TODO FIXME delete all (rows?) from Snap persistence
        }

        #endregion

        private static void GenerateClass(GONetParticipant_ComponentsWithAutoSyncMembers uniqueEntry)
        {
            var t4Template = new GONetParticipant_AutoMagicalSyncCompanion_GeneratedTemplate(uniqueEntry);
            string generatedClassText = t4Template.TransformText();

            if (!Directory.Exists(GENERATED_FILE_PATH))
            {
                Directory.CreateDirectory(GENERATED_FILE_PATH);
            }
            string writeToPath = string.Concat(GENERATED_FILE_PATH, t4Template.ClassName, FILE_SUFFIX);
            File.WriteAllText(writeToPath, generatedClassText);
        }
    }

    /// <summary>
    /// IMPORTANT: do NOT use.  This is for deserialize/load from persistence:
    /// </summary>
    [MessagePackObject]
    public class GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember
    {
        /// <summary>
        /// If false, the member is a property.
        /// </summary>
        [Key(0)]
        public bool isField;
        [Key(1)]
        public string memberTypeFullName;
        [Key(2)]
        public string memberName;

        [IgnoreMember]
        public GONetAutoMagicalSyncAttribute attribute;

        /// <summary>
        /// IMPORTANT: do NOT use.  This is for deserialize/load from persistence:
        /// </summary>
        public GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember() { }

        internal GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember(MemberInfo syncMember)
        {
            isField = syncMember.MemberType == MemberTypes.Field;
            memberTypeFullName = syncMember.MemberType == MemberTypes.Property
                                    ? ((PropertyInfo)syncMember).PropertyType.FullName
                                    : ((FieldInfo)syncMember).FieldType.FullName;
            memberName = syncMember.Name;

            attribute = (GONetAutoMagicalSyncAttribute)syncMember.GetCustomAttribute(typeof(GONetAutoMagicalSyncAttribute), true);
        }

        internal void PostDeserialize_InitAttribute(string memberOwner_componentTypeAssemblyQualifiedName)
        {
            Type memberOwnerType = Type.GetType(memberOwner_componentTypeAssemblyQualifiedName);
            MemberInfo syncMember = memberOwnerType.GetMember(memberName, BindingFlags.Public | BindingFlags.Instance)[0];
            attribute = (GONetAutoMagicalSyncAttribute)syncMember.GetCustomAttribute(typeof(GONetAutoMagicalSyncAttribute), true);
        }
    }

    /// <summary>
    /// IMPORTANT: do NOT use.  This is for deserialize/load from persistence:
    /// </summary>
    [MessagePackObject]
    public class GONetParticipant_ComponentsWithAutoSyncMembers_Single
    {
        [Key(0)]
        public string componentTypeName;
        [Key(1)]
        public string componentTypeFullName;
        [Key(2)]
        public string componentTypeAssemblyQualifiedName;

        /// <summary>
        /// in deterministic order!
        /// </summary>
        [Key(3)]
        public GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember[] autoSyncMembers;

        /// <summary>
        /// IMPORTANT: do NOT use.  This is for deserialize/load from persistence:
        /// </summary>
        public GONetParticipant_ComponentsWithAutoSyncMembers_Single() {}

        /// <param name="component_autoSyncMembers">in deterministic order!</param>
        internal GONetParticipant_ComponentsWithAutoSyncMembers_Single(MonoBehaviour component, GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember[] component_autoSyncMembers)
        {
            Type componentType = component.GetType();
            componentTypeName = componentType.Name;
            componentTypeFullName = componentType.FullName;
            componentTypeAssemblyQualifiedName = componentType.AssemblyQualifiedName;

            autoSyncMembers = component_autoSyncMembers;
        }
    }

    /// <summary>
    /// This is what represents a unique combination of <see cref="GONetAutoMagicalSyncAttribute"/>s on a <see cref="GameObject"/> with <see cref="GONetParticipant"/> "installed."
    /// A set of these gets persisted into a file at <see cref="GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.SNAPS_FILE"/>.
    /// 
    /// NOTE: once called this Snap...renamed to this.
    /// 
    /// </summary>
    [MessagePackObject]
    public class GONetParticipant_ComponentsWithAutoSyncMembers
    {
        /// <summary>
        /// Assigned once know to be a unique combo.
        /// Relates directly to <see cref="GONetParticipant.codeGenerationId"/>.
        /// IMPORTANT: MessagePack for C# is not supporting internal field.....so, this is now public...boo!
        /// </summary>
        [Key(0)]
        public byte codeGenerationId;

        /// <summary>
        /// In deterministic order....with the one for <see cref="GONetParticipant.GONetId"/> first due to special case processing....also <see cref="GONetParticipant.ASSumed_GONetId_INDEX"/>
        /// </summary>
        [Key(1)]
        public GONetParticipant_ComponentsWithAutoSyncMembers_Single[] ComponentMemberNames_By_ComponentTypeFullName;

        /// <summary>
        /// This is ONLY populated when <see cref="GONetParticipant_ComponentsWithAutoSyncMembers.GONetParticipant_ComponentsWithAutoSyncMembers(GONetParticipant)"/> is used during 
        /// the time leading up to doing a generation call (i.e., <see cref="GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.DoAllTheGenerationStuffs"/>).
        /// </summary>
        [IgnoreMember]
        internal GONetParticipant gonetParticipant;

        /// <summary>
        /// IMPORTANT: Do NOT use this.  Being public is required to work with deserialize/load from persistence:
        /// </summary>
        public GONetParticipant_ComponentsWithAutoSyncMembers() { }

        internal GONetParticipant_ComponentsWithAutoSyncMembers(GONetParticipant gonetParticipant)
        {
            this.gonetParticipant = gonetParticipant;

            int iSinglesAdded = -1;
            int gonetIdOriginalIndex_single = -1;
            int gonetIdOriginalIndex_singleMember = -1;

            var componentMemberNames_By_ComponentTypeFullName = new LinkedList<GONetParticipant_ComponentsWithAutoSyncMembers_Single>();
            foreach (MonoBehaviour component in gonetParticipant.GetComponents<MonoBehaviour>().OrderBy(c => c.GetType().FullName))
            {
                var componentAutoSyncMembers = new LinkedList<GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember>();

                IEnumerable<MemberInfo> syncMembers = component
                    .GetType()
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                    .Where(member => (member.MemberType == MemberTypes.Property || member.MemberType == MemberTypes.Field)
                                    && member.GetCustomAttribute(typeof(GONetAutoMagicalSyncAttribute), true) != null)
                    .OrderBy(member => member.Name);

                int syncMemberCount = syncMembers.Count();
                bool willBeAddingBelow = syncMemberCount > 0;
                if (willBeAddingBelow)
                {
                    ++iSinglesAdded;
                    bool isGONetParticipant = component.GetType() == typeof(GONetParticipant);
                    if (isGONetParticipant)
                    {
                        gonetIdOriginalIndex_single = iSinglesAdded;
                    }

                    for (int iSyncMember = 0; iSyncMember < syncMemberCount; ++iSyncMember)
                    {
                        MemberInfo syncMember = syncMembers.ElementAt(iSyncMember);
                        var singleMember = new GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember(syncMember);
                        componentAutoSyncMembers.AddLast(singleMember);

                        if (isGONetParticipant && syncMember.Name == nameof(GONetParticipant.GONetId))
                        {
                            gonetIdOriginalIndex_singleMember = iSyncMember;
                        }
                    }

                    var newSingle = new GONetParticipant_ComponentsWithAutoSyncMembers_Single(component, componentAutoSyncMembers.ToArray());
                    componentMemberNames_By_ComponentTypeFullName.AddLast(newSingle);
                }
            }

            ComponentMemberNames_By_ComponentTypeFullName = componentMemberNames_By_ComponentTypeFullName.ToArray();
            GONetLog.Debug("new ComponentMemberNames_By_ComponentTypeFullName.length: " + ComponentMemberNames_By_ComponentTypeFullName.Length);

            { // now that we have arrays, we can swap some stuff to ensure GONetId field is index 0!
                if (gonetIdOriginalIndex_single > 0) // if it is already 0, nothing to do
                {
                    var tmp = ComponentMemberNames_By_ComponentTypeFullName[0];
                    ComponentMemberNames_By_ComponentTypeFullName[0] = ComponentMemberNames_By_ComponentTypeFullName[gonetIdOriginalIndex_single];
                    ComponentMemberNames_By_ComponentTypeFullName[gonetIdOriginalIndex_single] = tmp;
                }

                // at this point, we know ComponentMemberNames_By_ComponentTypeFullName[0] represents the GONetParticipant component
                if (gonetIdOriginalIndex_singleMember > 0) // if it is already 0, nothing to do...yet again
                {
                    var gonetParticipantCompnent = ComponentMemberNames_By_ComponentTypeFullName[0];
                    var tmp = gonetParticipantCompnent.autoSyncMembers[0];
                    gonetParticipantCompnent.autoSyncMembers[0] = gonetParticipantCompnent.autoSyncMembers[gonetIdOriginalIndex_singleMember];
                    gonetParticipantCompnent.autoSyncMembers[gonetIdOriginalIndex_singleMember] = tmp;
                }
            }
        }
    }

    /// <summary>
    /// NOTE: Does NOT consider <see cref="GONetParticipant_ComponentsWithAutoSyncMembers.codeGenerationId"/>.
    /// </summary>
    class SnapComparer : IEqualityComparer<GONetParticipant_ComponentsWithAutoSyncMembers>
    {
        internal static SnapComparer Instance => new SnapComparer();

        public bool Equals(GONetParticipant_ComponentsWithAutoSyncMembers x, GONetParticipant_ComponentsWithAutoSyncMembers y)
        {
            bool areInitiallyEqual =
                x != null && y != null &&
                x.ComponentMemberNames_By_ComponentTypeFullName != null && y.ComponentMemberNames_By_ComponentTypeFullName != null &&
                x.ComponentMemberNames_By_ComponentTypeFullName.Length == y.ComponentMemberNames_By_ComponentTypeFullName.Length;

            GONetLog.Debug("areInitiallyEqual: " + areInitiallyEqual);

            if (areInitiallyEqual)
            {
                int componentCount = x.ComponentMemberNames_By_ComponentTypeFullName.Length;
                for (int iComponent = 0; iComponent < componentCount; ++iComponent)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers_Single xSingle = x.ComponentMemberNames_By_ComponentTypeFullName[iComponent];
                    int xCount = xSingle.autoSyncMembers.Length;

                    GONetParticipant_ComponentsWithAutoSyncMembers_Single ySingle = y.ComponentMemberNames_By_ComponentTypeFullName[iComponent];
                    int yCount = ySingle.autoSyncMembers.Length;

                    if (xSingle.componentTypeAssemblyQualifiedName == ySingle.componentTypeAssemblyQualifiedName && xCount == yCount)
                    {
                        for (int iMember = 0; iMember < xCount; ++iMember)
                        {
                            string xMemberName = xSingle.autoSyncMembers[iMember].memberName;
                            string yMemberName = ySingle.autoSyncMembers[iMember].memberName;
                            if (xMemberName != yMemberName)
                            {
                                GONetLog.Debug("member names differ, iMember: " + iMember + " xMemberName: " + xMemberName + " yMemberName: " + yMemberName);
                                return false;
                            }
                        }
                    }
                    else
                    {
                        GONetLog.Debug("qualified names not same or different count");
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public int GetHashCode(GONetParticipant_ComponentsWithAutoSyncMembers obj)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// TODO: Hmm...how is this different than <see cref="GONetMain.AutoMagicalSync_ValueMonitoringSupport"/>
    /// </summary>
    struct AutoMagicalSyncAttribute_GenerationSupport
    {
        Type monoBehaviourType;
        MemberInfo decoratedMember;
        GONetAutoMagicalSyncAttribute syncAttribute;
    }

    /// <summary>
    /// <see cref="GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator"/> is static and in order to extend 
    /// <see cref="AssetPostprocessor"/>, you obviously cannot be static....that is why I exist and then delegate on to it.
    /// </summary>
    class Magoo : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.OnPostprocessAllAssets(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }
    }
}
