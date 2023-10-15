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

using GONet;
using UnityEngine;
using System.Collections.Generic;
using NetcodeIO.NET;

#if USING_AMAZON_GAMELIFT
using Aws.GameLift.Server;
#endif

public class GONetSampleClientOrServer : MonoBehaviour
{
    public bool isServer = true;

    public bool isUsingAWSGameLift =
#if USING_AMAZON_GAMELIFT
        true;
#else
        false;
#endif


#if USING_AMAZON_GAMELIFT
    volatile bool hasUnprocessedAWSGameLiftEvent_onStartGameSession = false;
#endif

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (isServer)
        {
            InitServer();
        }
        else
        {
            InitClient();
        }
    }

    private void InitClient()
    {
        if (isUsingAWSGameLift)
        {
            InitClient_AWSGameLift();
        }
        else
        {
            GONetGlobal.ServerIPAddress_Actual = GONetGlobal.ServerIPAddress_Default;
            GONetGlobal.ServerPort_Actual = GONetGlobal.ServerPort_Default;

            OnReadyToStartGONet();
        }
    }

    private void InitClient_AWSGameLift()
    {
#if USING_AMAZON_GAMELIFT
        GameLift gameLift = new GameLift();
        gameLift.Awake();
        gameLift.Start();

        string auth = null;
        gameLift.GetConnectionInfo(ref GONetGlobal.serverIPAddress_Actual, ref GONetGlobal.serverPort_Actual, ref auth);

        { // messy and looks duplicative, but ensures event gets fired in there:
            GONetGlobal.ServerIPAddress_Actual = GONetGlobal.serverIPAddress_Actual;
            GONetGlobal.ServerPort_Actual = GONetGlobal.serverPort_Actual;
        }

        OnReadyToStartGONet();
#endif
    }

    private void InitServer()
    {
        Application.targetFrameRate = 128;

        if (isUsingAWSGameLift)
        {
            InitServer_AWSGameLift();
        }
        else
        {
            GONetGlobal.ServerIPAddress_Actual = GONetGlobal.ServerIPAddress_Default;
            GONetGlobal.ServerPort_Actual = GONetGlobal.ServerPort_Default;

            OnReadyToStartGONet();
        }
    }

    private void InitServer_AWSGameLift()
    {
#if USING_AMAZON_GAMELIFT
        //Identify port number (hard coded here for simplicity) the game server is listening on for player connections
        var listeningPort = GONetGlobal.ServerPort_Default;

        //InitSDK will establish a local connection with GameLift's agent to enable further communication.
        var initSDKOutcome = GameLiftServerAPI.InitSDK();
        if (initSDKOutcome.Success)
        {
            ProcessParameters processParameters = new ProcessParameters(
                (gameSession) =>
                {
                    //When a game session is created, GameLift sends an activation request to the game server and passes along the game session object containing game properties and other settings.
                    //Here is where a game server should take action based on the game session object.
                    //Once the game server is ready to receive incoming player connections, it should invoke GameLiftServerAPI.ActivateGameSession()

                    GONetGlobal.ServerIPAddress_Actual = gameSession.IpAddress;
                    GONetGlobal.ServerPort_Actual = gameSession.Port;
                    hasUnprocessedAWSGameLiftEvent_onStartGameSession = true;
                },
                (updateGameSession) =>
                {
                    //When a game session is updated (e.g. by FlexMatch backfill), GameLiftsends a request to the game
                    //server containing the updated game session object.  The game server can then examine the provided
                    //matchmakerData and handle new incoming players appropriately.
                    //updateReason is the reason this update is being supplied.
                },
                () =>
                {
                    //OnProcessTerminate callback. GameLift will invoke this callback before shutting down an instance hosting this game server.
                    //It gives this game server a chance to save its state, communicate with services, etc., before being shut down.
                    //In this case, we simply tell GameLift we are indeed going to shutdown.
                    GameLiftServerAPI.ProcessEnding();
                },
                () =>
                {
                    //This is the HealthCheck callback.
                    //GameLift will invoke this callback every 60 seconds or so.
                    //Here, a game server might want to check the health of dependencies and such.
                    //Simply return true if healthy, false otherwise.
                    //The game server has 60 seconds to respond with its health status. GameLift will default to 'false' if the game server doesn't respond in time.
                    //In this case, we're always healthy!
                    return true;
                },
                listeningPort, //This game server tells GameLift that it will listen on port 7777 for incoming player connections.
                new LogParameters(new List<string>()
                    {
                        //Here, the game server tells GameLift what set of files to upload when the game session ends.
                        //GameLift will upload everything specified here for the developers to fetch later.
                        "/local/game/logs/myserver.log"
                    })
            );

            //Calling ProcessReady tells GameLift this game server is ready to receive incoming game sessions!
            var processReadyOutcome = GameLiftServerAPI.ProcessReady(processParameters);
            if (processReadyOutcome.Success)
            {
                GONetLog.Info("ProcessReady success.");
            }
            else
            {
                GONetLog.Error("ProcessReady failure : " + processReadyOutcome.Error.ToString());
            }
        }
        else
        {
            GONetLog.Error("InitSDK failure : " + initSDKOutcome.Error.ToString());
        }
#endif
    }

    private void Update()
    {
#if USING_AMAZON_GAMELIFT
        if (hasUnprocessedAWSGameLiftEvent_onStartGameSession)
        {
            hasUnprocessedAWSGameLiftEvent_onStartGameSession = false;

            OnReadyToStartGONet(); // IMPORTANT: need to make sure this runs on main unity thread, which is why we are using hasUnprocessedAWSGameLiftEvent_onStartGameSession

            GameLiftServerAPI.ActivateGameSession();
        }
#endif
    }

    void OnApplicationQuit()
    {
#if USING_AMAZON_GAMELIFT
        if (isServer && isUsingAWSGameLift)
        {
            //Make sure to call GameLiftServerAPI.Destroy() when the application quits. This resets the local connection with GameLift's agent.
            GameLiftServerAPI.Destroy();
        }
#endif
    }

    private void OnReadyToStartGONet()
    {
        if (isServer)
        {
            GONetMain.gonetServer = new GONetServer(100, GONetGlobal.ServerIPAddress_Actual, GONetGlobal.ServerPort_Actual);
            GONetMain.gonetServer.Start();
        }
        else
        {
            GONetMain.GONetClient = new GONetClient(new Client());
            GONetMain.GONetClient.ConnectToServer(GONetGlobal.ServerIPAddress_Actual, GONetGlobal.ServerPort_Actual, 30);
        }
    }
}
