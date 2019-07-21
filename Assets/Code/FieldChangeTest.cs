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

using GONet;
using UnityEngine;

public class FieldChangeTest : MonoBehaviour
{
    public float someCoolGuyFloat;
    public float rottieTotty;

    [GONetAutoMagicalSync(SettingsProfileTemplateName = "FloatieMcFloats")]
    public float nada;

    TMPro.TextMeshProUGUI nadaText;

    Vector3 startPosition;
    Quaternion startRotation;

    float moveAmount = 5f;
    float moveAmount_rottieTotty = 0.25f;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        nadaText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
    }

    private void Update()
    {
        /* test with an ever-changing field value: */
        if (GONetMain.DoIHaveAuthorityOver(gameObject))
        {
            someCoolGuyFloat += moveAmount * Time.deltaTime;
            if (someCoolGuyFloat >= 6 || someCoolGuyFloat <= -6)
            {
                moveAmount *= -1;
            }

            rottieTotty += moveAmount_rottieTotty * Time.deltaTime;
            if (rottieTotty >= 2 || rottieTotty <= -1)
            {
                moveAmount_rottieTotty *= -1;
            }

            transform.rotation = Quaternion.Euler(startRotation.eulerAngles * rottieTotty);
            transform.position = startPosition + new Vector3(0, 0, someCoolGuyFloat);

            nada += 0.005f * Time.deltaTime;
        }
        /* */

        nadaText.text = nada.ToString();
    }
}
