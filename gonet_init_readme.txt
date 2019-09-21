export package notes:
1. Ensure to include required generated classes: SyncEvent_GONetParticipant_Xxx.cs

===============
Getting started with GONet:
===============
0. before importing GONet unity package into your project (Unity 2018.3+)
1. ensure usafe code allowed: Edit => Project Settings => Player => Allow 'unsafe' Code
2. ensure .NET API version: Edit => Project Settings => Player => Api Compatibility Level* => .NET 4.x
3. (optional) if you want GONet logging features, ensure logging configured: Edit => Project Settings => Player => Scripting Define Symbols (contains at least: "LOG_DEBUG;LOG_INFO;LOG_WARNING;LOG_ERROR;LOG_FATAL") 
4. import GONet unity package into your project
5. COMPILE (should happen automatically after import), or the subsequent steps are not going to work
6. drag Assets/GONet/Resources/GONet/GONet_GlobalContext into your start-up scene (optionally => open Assets/GONet/Sample/GONetSampleScene.unity that already has it instead of using your scene)
7. ensure Script Execution Order is setup: Edit => Project Settings => Script Execution Order (Add GONet.GONetGlobal at a value of -200 and GONet.GONetParticipant at a value of -199)  <== setting included in package
8. click Run/Play in Unity editor to play the scene (code generation will occur and scene should play....no errors/exceptions...if all is well)
===============
9. with scene running and Game windows focused, press the following keys simultaneously on the keyboard to spawn a server: left ALT + S (GONetServer(Clone) instance appears in the scene and server is now listening for connections from clients)
10. click Run/Play in Unity editor to stop playing the currently playing scene
=============== (if all that went well, you should be set to test some stuff in builds or just develop some and then test)
11. create a build (e.g., gonet_sample.exe on Windows)

12. If on Windows:
12a. open project folder => /Assets/StreamingAssets/GONet and copy Start_CLIENT.bat and Start_SERVER.bat
12b. paste files into build folder where gonet_sample.exe exists
12c. open both pasted files, changing GONetSandbox.exe to gonet_sample.exe, save files
12d. run Start_SERVER.bat (server needs to start first, running this bat file does that)
12e. run Start_CLIENT.bat (client will connect to local server)

13. If not on Windows:
13a. run first instance of build, focus mouse there, press left ALT + S (server needs to start first...BEWARE: there is really no indication the server is started, but if the window had focus and you pressed the key combo it started)
13b. run second instance of build, focus mouse there, press Left ALT + C (client will connect to local server...BEWARE: before pressing the key combo in the client window, the game might appear to be out of "data sync" and will correct itself once the client is connected to server....the client thinks he owns stuff that he does not until it is forced into submission!)
=============== (yeah...you are off the ground with GONet!  Go add some stuff to the scene with GONetParticipant component added to it and do something interesting with it, create another build, test again, rinse, repeat!)