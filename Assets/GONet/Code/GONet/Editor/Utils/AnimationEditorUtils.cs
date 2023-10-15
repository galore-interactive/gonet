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
using System.Reflection;
using UnityEditor.Animations;
using UnityEngine;

namespace GONet.Utils
{
    public static class AnimationEditorUtils
    {
        /// <summary>
        /// Returns the array of <see cref="AnimatorControllerParameter"/> that are within the <see cref="RuntimeAnimatorController"/> of this <see cref="Animator"/>
        /// This method will fail (and as a consequence return false) in case that the <see cref="Animator"/> component or the <see cref="RuntimeAnimatorController"/> inside it is null.
        /// If the <see cref="RuntimeAnimatorController"/> does not have any parameter this method will return true but with an empty array of parameters.
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="parameters">The </param>
        /// <returns></returns>
        public static bool TryGetAnimatorControllerParameters(Animator animator, out AnimatorControllerParameter[] parameters)
        {
            if (animator != null && animator.runtimeAnimatorController != null) // IMPORTANT: in editor, looks like animator.parameterCount is [sometimes!...figured out when...it is only when the Animator window is open and its controller is selected...editor tries to do tricky stuff that whacks this all out for some reason] 0 even when shit is there....hence the usage of animator.runtimeAnimatorController.parameters instead of animator.parameters
            {// intrinsic Animator properties that cannot manually have the [GONetAutoMagicalSync] added.... (e.g., transform rotation and position)
                object valueObject = null;
                if (animator.runtimeAnimatorController is AnimatorController) //If this RuntimeAnimatorController is an of type AnimatorController too we need to get the parameters from this class
                {
                    Type type = animator.runtimeAnimatorController.GetType();
                    PropertyInfo propertyInfo = type.GetProperty(nameof(Animator.parameters), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    valueObject = propertyInfo.GetValue(animator.runtimeAnimatorController);
                }
                else if (animator.runtimeAnimatorController is AnimatorOverrideController) //If this RuntimeAnimatorController is an of type AnimatorOverrideController too we need to search for the AnimatorController that it overrides and then get the parameters from that class.
                {
                    Type type = ((AnimatorOverrideController)animator.runtimeAnimatorController).runtimeAnimatorController.GetType();
                    PropertyInfo propertyInfo = type.GetProperty(nameof(Animator.parameters), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    valueObject = propertyInfo.GetValue(((AnimatorOverrideController)animator.runtimeAnimatorController).runtimeAnimatorController);
                }

                parameters = (AnimatorControllerParameter[])valueObject;
                return true;
            }
            else
            {
                parameters = null;
                return false;
            }
        }
    }
}