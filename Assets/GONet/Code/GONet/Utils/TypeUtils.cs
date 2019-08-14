/* Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GONet.Utils
{
    public static class TypeUtils
    {
        public static bool IsStruct(Type type)
        {
            return type != null && type.IsValueType && !type.IsEnum && !type.IsPrimitive;
        }

        public static Type GetElementType(Type possibleIEnumerableImplementorType)
        {
            Type elementType = null;
            Type[] interfaces = possibleIEnumerableImplementorType.GetInterfaces();
            foreach (Type i in interfaces)
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>)))
                {
                    elementType = i.GetGenericArguments()[0];
                }
            }
            return elementType;
        }

        public static bool IsStruct(object @object)
        {
            if (@object != null)
            {
                Type type = @object.GetType();
                return type != null && type.IsValueType && !type.IsEnum && !type.IsPrimitive;
            }
            return false;
        }

        public static bool DoesTypeImlementGenericInterface(Type type, Type genericInterfaceType)
        {
            if (type == null || genericInterfaceType == null)
            {
                return false;
            }

            return type.GetInterfaces()
                .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == genericInterfaceType);
        }

        public static bool DoTypesShareAncestor(Type typeA, Type typeB, Type ancestorType)
        {
            return IsTypeAInstanceOfTypeB(typeA, ancestorType) && IsTypeAInstanceOfTypeB(typeB, ancestorType);
        }

        public static Tuple<Type[], Type[]> GetGenericArgumentsForSharedAncestor(Type typeA, Type typeB, Type ancestorType)
        {
            if ((ancestorType.IsGenericType || ancestorType.IsGenericTypeDefinition) && DoTypesShareAncestor(typeA, typeB, ancestorType))
            {
                Type[] genericArgsA = GetGenericArgumentsForAncestor(typeA, ancestorType);
                Type[] genericArgsB = GetGenericArgumentsForAncestor(typeB, ancestorType);
                return Tuple.Create(genericArgsA, genericArgsB);
            }

            return null;
        }

        private static Type[] GetGenericArgumentsForAncestor(Type typeA, Type ancestorType)
        {
            Type t = typeA;
            Type ancestorTypeGTD = ancestorType.GetGenericTypeDefinition();
            while (t != null)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == ancestorTypeGTD)
                {
                    return t.GetGenericArguments();
                }
                t = t.BaseType; // avoid executing this after we have the match!
            }
            return null;
        }

        public static void ConvertValueToAppropriateTargetType(Type targetType, ref object currentValue)
        {
            if (targetType.IsEnum)
            {
                currentValue = Enum.ToObject(targetType, currentValue);
            }
            else
            {
                currentValue = Convert.ChangeType(currentValue, targetType);
            }
        }

        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            // TODO: Argument validation
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        public static Type GetTypeByClassName_AsExpensiveAsItMayBeToDo(string className, bool throwOnError)
        {
            try
            {
                // TODO for effeciency's sake, check the current assembly first!

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    var classType = assembly.GetTypes().FirstOrDefault(type => type.Name == className);
                    if (classType != null)
                    {
                        return classType;
                    }
                }
            }
            catch (Exception e)
            {
                if (throwOnError)
                {
                    throw e;
                }
            }
            return null;
        }

        public static bool IsTypeAInstanceOfTypeB(Type typeA, Type typeB)
        {
            if (typeA == null || typeB == null)
            {
                return false;
            }

            if (typeA == typeB)
            {
                return true;
            }

            if (!typeB.IsGenericTypeDefinition)
            {
                return typeB.IsAssignableFrom(typeA);
            }

            var toCheckTypes = new List<Type> { typeA }; // TODO FIXME newing up memory here is not good!
            if (typeB.IsInterface)
            {
                toCheckTypes.AddRange(typeA.GetInterfaces());
            }

            var basedOn = typeA;
            while (basedOn.BaseType != null)
            {
                toCheckTypes.Add(basedOn.BaseType);
                basedOn = basedOn.BaseType;
            }

            return toCheckTypes.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeB); // TODO using Linq here is not good
        }
    }
}
