/* Copyright (C) Shaun Curtis Sheppard - All Rights Reserved
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

        /// <summary>
        /// Creates one constructor for each public constructor in the base class. Each constructor simply
        /// forwards its arguments to the base constructor, and matches the base constructor's signature.
        /// Supports optional values, and custom attributes on constructors and parameters.
        /// Does not support n-ary (variadic) constructors
        /// </summary>
        public static void DefinePassThroughConstructors(this TypeBuilder builder, Type baseType)
        {
            foreach (ConstructorInfo baseConstructor in baseType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                ParameterInfo[] parameters = baseConstructor.GetParameters();
                if (parameters.Length > 0 && parameters.Last().IsDefined(typeof(ParamArrayAttribute), false))
                {
                    //throw new InvalidOperationException("Variatic constructors are not supported");
                    continue;
                }

                Type[] parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
                Type[][] requiredCustomModifiers = parameters.Select(p => p.GetRequiredCustomModifiers()).ToArray();
                Type[][] optionalCustomModifiers = parameters.Select(p => p.GetOptionalCustomModifiers()).ToArray();

                ConstructorBuilder constructorBuilder = builder.DefineConstructor(baseConstructor.Attributes, baseConstructor.CallingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
                int parameterCount = parameters.Length;
                for (int i = 0; i < parameterCount; ++i)
                {
                    var parameter = parameters[i];
                    var parameterBuilder = constructorBuilder.DefineParameter(i + 1, parameter.Attributes, parameter.Name);
                    if (((int)parameter.Attributes & (int)ParameterAttributes.HasDefault) != 0)
                    {
                        parameterBuilder.SetConstant(parameter.RawDefaultValue);
                    }

                    foreach (CustomAttributeBuilder attribute in BuildCustomAttributes(parameter.GetCustomAttributesData()))
                    {
                        parameterBuilder.SetCustomAttribute(attribute);
                    }
                }

                foreach (CustomAttributeBuilder attribute in BuildCustomAttributes(baseConstructor.GetCustomAttributesData()))
                {
                    constructorBuilder.SetCustomAttribute(attribute);
                }

                ILGenerator emitter = constructorBuilder.GetILGenerator();
                emitter.Emit(OpCodes.Nop);

                if (!baseConstructor.IsPrivate)
                { // Load `this` and call base constructor with arguments (well, as long as the base is not private and not callable from a child)
                    emitter.Emit(OpCodes.Ldarg_0);
                    for (var i = 1; i <= parameterCount; ++i)
                    {
                        emitter.Emit(OpCodes.Ldarg, i);
                    }
                    emitter.Emit(OpCodes.Call, baseConstructor);
                }

                emitter.Emit(OpCodes.Ret);
            }
        }

        private static CustomAttributeBuilder[] BuildCustomAttributes(IEnumerable<CustomAttributeData> customAttributes)
        {
            return customAttributes.Select(attribute => {
                var attributeArgs = attribute.ConstructorArguments.Select(a => a.Value).ToArray();
                var namedPropertyInfos = attribute.NamedArguments.Select(a => a.MemberInfo).OfType<PropertyInfo>().ToArray();
                var namedPropertyValues = attribute.NamedArguments.Where(a => a.MemberInfo is PropertyInfo).Select(a => a.TypedValue.Value).ToArray();
                var namedFieldInfos = attribute.NamedArguments.Select(a => a.MemberInfo).OfType<FieldInfo>().ToArray();
                var namedFieldValues = attribute.NamedArguments.Where(a => a.MemberInfo is FieldInfo).Select(a => a.TypedValue.Value).ToArray();
                return new CustomAttributeBuilder(attribute.Constructor, attributeArgs, namedPropertyInfos, namedPropertyValues, namedFieldInfos, namedFieldValues);
            }).ToArray();
        }
    }
}
