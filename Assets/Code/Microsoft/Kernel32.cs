using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Kernel32 {
#if (UNITY_BUILD)
        const string DLL_NAME = "kernel32";
#else
    const string DLL_NAME = "kernel32.dll";
#endif

    [DllImport(DLL_NAME)]
    public static extern bool GetSystemPower(out int system_power_status);
}
