# GONet
Quickly add multiplayer capability into your game. GONet is the high-performance code base that optimizes resource utilization so you multiplayer game can do more fun stuff players love.

Here is the Unity Asset Store description:

GONet is the Unity3D GameObject Networking solution with tight, developer-friendly integration into the Unity architecture/runtime.  If you need to network something and a GameObject is involved, GONet is the answer.
<br><br>
<strong>TL;DR</strong>
<br>State of affairs for multiplayer/networking in Unity:
<br>*Netcode for GameObjects (NGO) can leave much to be desired
<br>*(NetCode for) Entities can be a real challenge to adopt
<br>*Other multiplayer/networking options do an ~OK~ job of integrating with Unity (some things done well and others not so well)
<br>*You need a robust Unity-centric networking library <strong>now</strong> to deliver your multiplayer game to fans
<br>*GONet == solution (auto-magical data sync & value blending & compression & encryption, extensible pub/sub events, pure C# w/ all source code) 
<br><br>
<strong>GONet mantra:</strong>
<br>-doing the easy/common stuff should be dead simple (no programming)
<br>-doing the (<em>seemingly</em>) hard stuff should first be possible and second be approachable
<br>-high performance and low resource utilization are kept at the forefront of design, development, decision making and implementation details
<br>-there is much more to a great networked multi-player game than syncing transforms, GONet helps take you the rest of the way with features and implementation experts to <em>"save your bacon"</em>
<br>-there is no "asset fairy" to auto-magically <em>multi-playerify</em> your entire game; <strong>BUT</strong> a robust toolset is a must - enter GONet
<br>-TCP has no place here (AAA studio grade solutions are UDP-only when any competitive and/or near-realtime gameplay needs exist - GONet is UDP-only)
<br><br>
<strong>NOTE:</strong> Instead of slow runtime reflection or error-prone/confusing byte weaving, GONet utilizes (automatic) design-time code generation that is runtime debuggable. Unity-friendly multi-threading where appropriate. Works well with default settings, but tweak away as needed. <strong>REPLAY support is out of the box</strong> (PRO Only)!  And the box is not black, as <strong><em>source code is included</em></strong>.
<br><br>
<strong>FEATURES:</strong>
<br><strong>Wide platform/runtime support:</strong>
<br>*All managed C#, no native libraries
<br>*Ahead of Time (AOT) compilation support (e.g., iOS)
<br>*IL2CPP
<br><br><strong>Auto-magical data sync:</strong>
<br>*Transform - position/localPosition, rotation/localRotation, scale
<br>*Animator Controller parameters
<br>*Any MonoBehaviour fields ([SyncVar] replacement)
<br>*Omni-directional from owner to non-owners (server and clients alike)
<br>*GameObject.Instantiate() is auto-magically networked from the owner/instantiator to all interested parties/machines (optionally use a prefab alternate for non-owners)
<br>*You control sync settings applied to groups of data items: default and/or user-defined (via profiles/templates, for when you want/need more control)
<br>*Auto-magically blend smoothly between received values for non-owners: default interpolation/extrapolation and/or user-defined implementation (great for dealing with unreliable <em>"streams"</em>)
<br><br><strong>Event Bus:</strong>
<br>*Publish and subscribe to your own omni-directional (<em>arbitrary</em> content between server <-> clients) communication  ([Command], [ClientRpc] and [TargetRpc] replacement)
<br>*Auto-magically sync'd changes also (automatically) publish events to which you can subscribe (e.g., energy dropped below 10 => change avatar material)
<br>*Transient events - delivered to clients connected to game session when event occurs
<br>*Persistent events - like Transient event with additional delivery to any clients connecting to the game session after event occurs and will exist in recorded/replay data
<br>*Any user-defined event class - can be either transient or persistent
<br>*Promotes industry proven event-driven game architecture and facilitates GONet Record+Replay
<br><br><strong>Serialization options:</strong>
<br>*Default Auto-magical data sync:  custom bit packing (with LZ4 compression)
<br>*Default Event Bus traffic: MessagePack (with LZ4 compression)
<br>*User-defined custom overrides: if you have special cases or feel you can do it better, by all means, a mechanism to do so is provided
<br>*Configurable value quantization: choose # bits to occupy and auto-magically quantizes/compresses to fit (great for LOD, <em>see below</em>)
<br><br><strong>(PRO Only) Record+Replay GONet sessions:</strong>
<br>*(PRO Only) Everything GONet sends and receives is recorded (server and clients alike)
<br>*(PRO Only) Replay the recorded gameplay. Great for: (eSports) In-game instant replay, Location Based Entertainment takeaway not to mention development troubleshooting and bug reproduction (i.e., a <em>"save your bacon"</em> feature)
<br>*(PRO Only) Recorded session data feeds into time series, statistics-based graphs for analysis during development and perhaps more importantly after game is released
<br><br><strong>(PRO Only) Level of Detail (LOD) management:</strong>
<br>*(PRO Only) Control the Who/What/When/Where/Why/How of data detail
<br>*(PRO Only) Default implementation uses distance to client's main player controlled GameObject (closer = high data resolution, further = lower data resolution...down to excluded entirely)
<br>*(PRO Only) User-defined custom overrides: decide what causes changes to a GameObject's data LOD, when it should be applied, how much it affects LOD, etc...
<br>*(PRO Only) Facilitates large worlds and/or large numbers of networked GameObjects and sync'd values (e.g., MMO)
<br><br><strong>Network Transport:</strong>
<br>*High level (just add GONetParticipant to GameObjects for transform/animation and [GONetAutoMagicalSync] to MonoBehaviour fields/properties and you are <em>in business</em>)
<br>*Mid level (create/publish/subscribe custom events and also fine tune available network settings to meet particular needs)
<br>*Low level (send/receive/manage data however you need to, but you probably will never need this)
<br>*UDP (unreliable+unordered) and RUDP (reliable+ordered UDP)
<br>*Encryption (customized version of Bouncy Castle Crypto API)
<br>*Configurable channels (optional)
<br><br><strong>Network topologies:</strong>
<br>*Dedicated game server (client-server DGS)
<br>*Local (LAN): good for location-based entertainment (LBE)
<br>*(PRO Only) Peer-to-Peer (P2P): NAT punchthrough etc...
<br><br><strong>Support:</strong>
<br>*Basic tutorials, examples and forum/email back and forth as time/resources permit
<br>*(PRO Only) Fixes to unique and/or uncommon bugs/issues
<br>*(PRO Only) Screen sharing collaboration sessions and more in-depth responses at front of email support line
<br>*(PRO Only) Access to extensive example projects with detailed documentation
<br>*(Premium Only) GONet support team working directly with your team with off-site and on-site options
<br>*(Premium Only) GONet custom feature requests
<br><br><strong>General:</strong>
<br>*RigidBody support
<br>*Run-time authority transition (e.g., client to server for projectiles)
<br>*Source code (well commented and debuggable C#)
<br><br><strong>Logging:</strong>
<br>*Built atop the robust, well-known log4net api/library
<br>*Writes to Console in editor and to "logs/gonet.log" file in builds
<br>*Outputs logging level, thread #, system time, frame time and your message with every log statement (if you have ever performed troubleshooting on a networked game, you know this is one of those <em>"save your bacon"</em> features)
<br>*Used in GONet code and fully available to use wherever you like
<br><br>
<strong>Recommended Minimum System Specifications (Running GONet Games):</strong>
<br>*Baseline requirements listed on Unity site: https://unity3d.com/unity/system-requirements
<br>*64-bit CPU with 4 cores
<br><br>
Let GONet take on the most difficult burden you face, so your team can focus on interesting creative matters.  GONet integrates nicely into existing game project code bases and effortlessly for those lucky to be in green field development.
<br><br>
Manual and API Documentation 
Please discuss GONet in the forum
Contact support to discuss consulting assistance for implementation/integration/migration with your game project team.  We have a multi-tiered support structure to suit your needs, from trivial to highly involved.  Our implementation experts will help you from getting off the ground up to crossing the finish line with you.
<br><br>
<br>Thanks for making all the way!  When it comes to getting your game networked and multi-player capable, you need to know what you are getting yourself into.  GONet is the real deal.
