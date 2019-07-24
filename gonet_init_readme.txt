1. import package into project (Unity 2018.3+)
2. ensure usafe code allowed: Edit => Project Settings => Player => Allow 'unsafe' Code
3. ensure .NET API version: Edit => Project Settings => Player => Api Compatibility Level* => .NET 4.x
4. ensure logging configured: Edit => Project Settings => Player => Scripting Define Symbols (contains at least: "LOG_DEBUG;LOG_INFO;LOG_WARNING;LOG_ERROR;LOG_FATAL")
4a. manually copy the unzipped contents of gonet_logConfig.zip into the root of the project folder
5. drag Resources/GONet_GlobalContext into your start-up scene (optionally => open Assets/GONet/Sample/GONetSampleScene.unity that already has it instead of using your scene)
6 click Run/Play in Unity editor to play the scene (code generation will occur and scene should play....no errors/exceptions...if all is well)
===============
7. with scene running and Game windows focused, press the following keys simultaneously on the keyboard to spawn a server: left ALT + S (GONetServer(Clone) instance appears in the scene and server is now listening for connections from clients)
8. click Run/Play in Unity editor to stop playing the currently playing scene
=============== (if all that went well, you should be set to test some stuff in builds or just develop some and then test)
9. create a build (e.g., gonet_sample.exe)
10. run first instance of build, focus mouse there, press left ALT + S (server needs to start first...BEWARE: there is really no indication the server is started, but if the window had focus and you pressed the key combo it started)
11. run second instance of build, focus mouse there, press Left ALT + C (client will connect to local server...BEWARE: before pressing the key combo in the client window, the game might appear to be out of "data sync" and will correct itself once the client is connected to server....the client thinks he owns stuff that he does not until it is forced into submission!)
=============== (yeah...you are off the ground with GONet!  Go add some stuff to the scene with GONetParticipant component added to it and do something interesting with it, create another build, test again, rinse, repeat!)