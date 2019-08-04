﻿/* Copyright (C) Shaun Curtis Sheppard - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Shaun Sheppard <shasheppard@gmail.com>, June 2019
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products
 */

using GONet.Utils;
using UnityEngine;

namespace GONet
{
    /// <summary>
    /// TODO: add good description.
    /// 
    /// Do NOT add to <see cref="GameObject"/> instances yourself.  This is more of an internal to GONet managed concept.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(GONetParticipant))]
    public class GONetSessionContext : MonoBehaviour
    {
        private const string WORKAROUND = "GONet logging entry point.  Please do not comment out the log statement using this.  This is a side effect of getting GONetLog to do static initialization inside the Unity main thread...or else!";

        private void Awake()
        {
            GONetLog.Debug(WORKAROUND);
        }

        private void Start()
        {
            DontDestroyOnLoad(gameObject); // IMPORTANT: This was moved from Awake to Start so it runs AFTER onSceneLoaded processes and this is recognized as a "design time GONetParticipant"...somehow moving into DDOL in Awake was too soon and it did not get categorized as design time
        }
    }
}
