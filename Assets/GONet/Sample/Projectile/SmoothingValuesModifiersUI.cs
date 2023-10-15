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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SmoothingValuesModifiersUI : MonoBehaviour
{
    public Toggle toggle_INPUTS_1;
    public Toggle toggle_INPUTS_2;
    public Toggle toggle_INPUTS_3;
    public Toggle toggle_OUTPUTS_1;
    public Toggle toggle_OUTPUTS_2;

    public Slider slider_INPUTS_1;
    public Slider slider_INPUTS_2;
    public Slider slider_INPUTS_3;
    public Slider slider_OUTPUTS_1;
    public Slider slider_OUTPUTS_2;

    public Text text_INPUTS_1;
    public Text text_INPUTS_2;
    public Text text_INPUTS_3;
    public Text text_OUTPUTS_1;
    public Text text_OUTPUTS_2;

    static SmoothingValuesModifiersUI Instance;

    public static readonly float[] INPUTS = new float[3];
    public static readonly float[] OUTPUTS = new float[2];

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    Dictionary<Slider, float> lastKnownValues = new Dictionary<Slider, float>();
    const int MAX_TOGGLE_LOCK_COUNT = 3;
    int toggleLockedCount = 0;

    private void OnEnable()
    {
        toggleLockedCount = 0;

        if (toggle_INPUTS_1.isOn) ++toggleLockedCount;
        if (toggle_INPUTS_2.isOn) ++toggleLockedCount;
        if (toggle_INPUTS_3.isOn) ++toggleLockedCount;
        if (toggle_OUTPUTS_1.isOn) ++toggleLockedCount;
        if (toggle_OUTPUTS_2.isOn) ++toggleLockedCount;

        lastKnownValues[slider_INPUTS_1] = slider_INPUTS_1.value;
        lastKnownValues[slider_INPUTS_2] = slider_INPUTS_2.value;
        lastKnownValues[slider_INPUTS_3] = slider_INPUTS_3.value;
        lastKnownValues[slider_OUTPUTS_1] = slider_OUTPUTS_1.value;
        lastKnownValues[slider_OUTPUTS_2] = slider_OUTPUTS_2.value;

        toggle_INPUTS_1.onValueChanged.AddListener(isOn => DoTogglese(isOn, toggle_INPUTS_1));
        toggle_INPUTS_2.onValueChanged.AddListener(isOn => DoTogglese(isOn, toggle_INPUTS_2));
        toggle_INPUTS_3.onValueChanged.AddListener(isOn => DoTogglese(isOn, toggle_INPUTS_3));
        toggle_OUTPUTS_1.onValueChanged.AddListener(isOn => DoTogglese(isOn, toggle_OUTPUTS_1));
        toggle_OUTPUTS_2.onValueChanged.AddListener(isOn => DoTogglese(isOn, toggle_OUTPUTS_2));

        slider_INPUTS_1.onValueChanged.AddListener(newValue => DoSlidese(newValue, slider_INPUTS_1));
        slider_INPUTS_2.onValueChanged.AddListener(newValue => DoSlidese(newValue, slider_INPUTS_2));
        slider_INPUTS_3.onValueChanged.AddListener(newValue => DoSlidese(newValue, slider_INPUTS_3));
        slider_OUTPUTS_1.onValueChanged.AddListener(newValue => DoSlidese(newValue, slider_OUTPUTS_1));
        slider_OUTPUTS_2.onValueChanged.AddListener(newValue => DoSlidese(newValue, slider_OUTPUTS_2));

        isDoingManualSlidese = false;
    }

    bool isDoingManualSlidese;

    private void DoSlidese(float newValue, Slider slid)
    {
        if (!isDoingManualSlidese)
        {
            isDoingManualSlidese = true;

            float previousValue = lastKnownValues[slid];
            float diff = newValue - previousValue;

            float distributeToEachAmount = -diff / (float)(5 - toggleLockedCount - 1); // NOTE: using -diff because we added diff to slid and we need to subtract diff from all others
            if (slider_INPUTS_1 != slid && !toggle_INPUTS_1.isOn) slider_INPUTS_1.value = slider_INPUTS_1.value + distributeToEachAmount;
            if (slider_INPUTS_2 != slid && !toggle_INPUTS_2.isOn) slider_INPUTS_2.value = slider_INPUTS_2.value + distributeToEachAmount;
            if (slider_INPUTS_3 != slid && !toggle_INPUTS_3.isOn) slider_INPUTS_3.value = slider_INPUTS_3.value + distributeToEachAmount;
            if (slider_OUTPUTS_1 != slid && !toggle_OUTPUTS_1.isOn) slider_OUTPUTS_1.value = slider_OUTPUTS_1.value + distributeToEachAmount;
            if (slider_OUTPUTS_2 != slid && !toggle_OUTPUTS_2.isOn) slider_OUTPUTS_2.value = slider_OUTPUTS_2.value + distributeToEachAmount;

            lastKnownValues[slider_INPUTS_1] = slider_INPUTS_1.value;
            lastKnownValues[slider_INPUTS_2] = slider_INPUTS_2.value;
            lastKnownValues[slider_INPUTS_3] = slider_INPUTS_3.value;
            lastKnownValues[slider_OUTPUTS_1] = slider_OUTPUTS_1.value;
            lastKnownValues[slider_OUTPUTS_2] = slider_OUTPUTS_2.value;
        }

        isDoingManualSlidese = false;
    }

    private void DoTogglese(bool isOn, Toggle toggled)
    {
        if (isOn)
        {
            if (++toggleLockedCount > MAX_TOGGLE_LOCK_COUNT)
            {
                toggled.isOn = false;
            }
        }
        else
        {
            --toggleLockedCount;
        }
    }

    void Update()
    {
        slider_INPUTS_1.enabled = !toggle_INPUTS_1.isOn;
        slider_INPUTS_2.enabled = !toggle_INPUTS_2.isOn;
        slider_INPUTS_3.enabled = !toggle_INPUTS_3.isOn;
        slider_OUTPUTS_1.enabled = !toggle_OUTPUTS_1.isOn;
        slider_OUTPUTS_2.enabled = !toggle_OUTPUTS_2.isOn;

        text_INPUTS_1.text = slider_INPUTS_1.value.ToString();
        text_INPUTS_2.text = slider_INPUTS_2.value.ToString();
        text_INPUTS_3.text = slider_INPUTS_3.value.ToString();
        text_OUTPUTS_1.text = slider_OUTPUTS_1.value.ToString();
        text_OUTPUTS_2.text = slider_OUTPUTS_2.value.ToString();

        INPUTS[0] = slider_INPUTS_1.value;
        INPUTS[1] = slider_INPUTS_2.value;
        INPUTS[2] = slider_INPUTS_3.value;
        OUTPUTS[0] = slider_OUTPUTS_1.value;
        OUTPUTS[1] = slider_OUTPUTS_2.value;
    }
}
