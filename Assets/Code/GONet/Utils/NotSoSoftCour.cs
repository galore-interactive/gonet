using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace GONet.Utils
{
#if CSHARP_7_3_OR_NEWER
    public static class NotSoSoftCour
    {
        private static readonly MethodInfo getAddressOf_methodInfoGeneric = typeof(NotSoSoftCour).GetMethod(nameof(GetAddressOfInternal), BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly List<GCHandle> getAddressOf_autoHandles = new List<GCHandle>(10000);

        /// <summary>
        /// If you made any calls to <see cref="GetAddressOfField(object, string)"/>, ensure you call this prior to exiting the application (i.e., killing the process).
        /// </summary>
        public static void FreeAllGCHandles()
        {
            foreach (var gcHandle in getAddressOf_autoHandles)
            {
                gcHandle.Free();
            }
        }

        private const BindingFlags getAddressOf_bindingFlags = 
            BindingFlags.NonPublic |
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly;

        /// <summary>
        /// PRE: <paramref name="pinnedObject_fieldOwner"/> needs to have been passed in to <see cref="GCHandle.Alloc(object)"/> prior to calling this in order for it to be pinned for use herein.
        ///      Furthermore, <see cref="GCHandle.Free"/> must not be called prior to making use of the returned <see cref="IntPtr"/>.
        /// </summary>
        public static IntPtr GetAddressOfField_AlreadyPinned(object pinnedObject_fieldOwner, string fieldName)
        {
            Type ownerType = pinnedObject_fieldOwner.GetType();
            FieldInfo fieldInfo = ownerType.GetField(fieldName, getAddressOf_bindingFlags);
            if (fieldInfo == null)
            {
                throw new MissingFieldException(ownerType.Name, fieldName);
            }

            MethodInfo methodInfoSpecific = getAddressOf_methodInfoGeneric.MakeGenericMethod(pinnedObject_fieldOwner.GetType(), fieldInfo.FieldType);

            // TODO: since the invokation of the method below uses emit, it will not be supported on AOT-based platforms (e.g., iOS or if using IL2CPP), so perhaps to support those platforms, we have to generate/emit that code and save it in a dll at design/editor time!
            // TODO: see https://docs.unity3d.com/Manual/ScriptingRestrictions.html to know which platforms are AOT-based
            IntPtr addressOfOwnersField = (IntPtr)methodInfoSpecific.Invoke(null, new object[] { pinnedObject_fieldOwner, fieldInfo });

            return addressOfOwnersField;
        }

        /// <summary>
        /// POST: <paramref name="unpinnedObject_fieldOwner"/> will have been passed in to <see cref="GCHandle.Alloc(object)"/> in order for it to be pinned for use herein.
        ///      Furthermore, <see cref="GCHandle.Free"/> must not be called prior to making use of the returned <see cref="IntPtr"/>.
        /// </summary>
        public static IntPtr GetAddressOfField(object unpinnedObject_fieldOwner, string fieldName)
        {
            GCHandle handle = GCHandle.Alloc(unpinnedObject_fieldOwner); // NOTE: this effectively pins the object, so we can do the cool guy stuff below
            getAddressOf_autoHandles.Add(handle);

            Type ownerType = unpinnedObject_fieldOwner.GetType();
            FieldInfo fieldInfo = ownerType.GetField(fieldName, getAddressOf_bindingFlags);
            if (fieldInfo == null)
            {
                throw new MissingFieldException(ownerType.Name, fieldName);
            }

            MethodInfo methodInfoSpecific = getAddressOf_methodInfoGeneric.MakeGenericMethod(unpinnedObject_fieldOwner.GetType(), fieldInfo.FieldType);

            // TODO: since the invokation of the method below uses emit, it will not be supported on AOT-based platforms (e.g., iOS or if using IL2CPP), so perhaps to support those platforms, we have to generate/emit that code and save it in a dll at design/editor time!
            // TODO: see https://docs.unity3d.com/Manual/ScriptingRestrictions.html to know which platforms are AOT-based
            IntPtr addressOfOwnersField = (IntPtr)methodInfoSpecific.Invoke(null, new object[] { unpinnedObject_fieldOwner, fieldInfo });

            return addressOfOwnersField;
        }

        // https://stackoverflow.com/a/45046664/425678

        private delegate ref TField AddressGetter<TOwner, TField>(TOwner pinnedObject_fieldOwner);
        private static IntPtr GetAddressOfInternal__<TOwner, TField>(TOwner pinnedObject_fieldOwner, FieldInfo fieldInfo)
        {
            const string REFGET = "__refget_";
            const string FI = "_fi_";
            string dynamicMethodName = string.Concat(REFGET, typeof(TOwner).Name, FI, fieldInfo.Name);

            // workaround for using ref-return with DynamicMethod:
            //   a.) initialize with dummy return value
            DynamicMethod dynamicMethod = new DynamicMethod(dynamicMethodName, typeof(TField), new[] { typeof(TOwner) }, typeof(TOwner), true);

            //   b.) replace with desired 'ByRef' return value
            const string RETURN_TYPE = "returnType"; // source/version of original code/runtime/test, the following was the name of the field: "m_returnType".....since it changed once, it goes to show this code is definitely brittle!!!
            dynamicMethod.GetType().GetField(RETURN_TYPE, getAddressOf_bindingFlags).SetValue(dynamicMethod, typeof(TField).MakeByRefType());

            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, fieldInfo);
            il.Emit(OpCodes.Ret);

            AddressGetter<TOwner, TField> addressGetter = (AddressGetter<TOwner, TField>)dynamicMethod.CreateDelegate(typeof(AddressGetter<TOwner, TField>));

            unsafe
            {
                TypedReference typedReference = __makeref(addressGetter(pinnedObject_fieldOwner));
                IntPtr ptr = *((IntPtr*)&typedReference);
                return ptr;
            }
        }
        
        private static IntPtr GetAddressOfInternal<TOwner, TField>(TOwner pinnedObject_fieldOwner, FieldInfo fieldInfo)
        {
            var asmName = new AssemblyName("WidgetDynamicAssembly." + Guid.NewGuid().ToString());
            var asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndCollect);
            var moduleBuilder = asmBuilder.DefineDynamicModule("Module");
            var typeBuilder = moduleBuilder.DefineType("WidgetHelper");
            var methodBuilder = typeBuilder.DefineMethod("GetLength", MethodAttributes.Static | MethodAttributes.Public, typeof(TField).MakeByRefType(), new[] { typeof(TOwner) });
            var ilGen = methodBuilder.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldflda, fieldInfo);
            ilGen.Emit(OpCodes.Ret);
            var type = typeBuilder.CreateType();
            var mi = type.GetMethod(methodBuilder.Name);
            var addressGetter = (AddressGetter<TOwner, TField>)mi.CreateDelegate(typeof(AddressGetter<TOwner, TField>));
            ref TField t = ref addressGetter(pinnedObject_fieldOwner);
            UnityEngine.Debug.Log("t: " + t);
            unsafe
            {
                
                TypedReference typedReference = __makeref(t);
                //UnityEngine.Debug.Log(string.Concat("t1: ", (IntPtr)&typedReference));
                IntPtr ptr = *(IntPtr*)(&typedReference);
                return ptr;
            }
        }
        
    }

    public class GetAddressOfField_Example   // this class is defined in a different assembly and is not known at compile time
    {
        public double foo;
        public int foo5;
        public char foo6;

        public static void Go()
        {
            GetAddressOfField_Example obj = new GetAddressOfField_Example();
            obj.foo = 0;
            obj.foo5 = 17;
            obj.foo6 = '\0';

            GCHandle gcHandle = GCHandle.Alloc(obj);

            unsafe
            {
                //IntPtr foo_add = NotSoSoftCour.GetAddressOfField_AlreadyPinned(obj, nameof(foo));
                //double* ptr = (double*)foo_add;
                //UnityEngine.Debug.Log("Current Value: " + *ptr);
                //*ptr = 4;
                //UnityEngine.Debug.Log("Updated Value: " + *ptr);


                IntPtr foo_add = NotSoSoftCour.GetAddressOfField_AlreadyPinned(obj, nameof(foo5));
                int* ptr = (int*)foo_add;
                UnityEngine.Debug.Log("Current Value: " + *ptr);



            }

            gcHandle.Free();

            /*
            Wrapper wrap = new Wrapper();
            ref double foo2 = ref wrap.GetFoo();
            foo2 = 456;
            wrap.PrintFoo();
            */
        }
    }

    public class Wrapper
    {
        GetAddressOfField_Example myField = new GetAddressOfField_Example() { foo = 123.321, foo5 = 123, foo6 = 'f' };

        public Wrapper()
        {
        }

        public ref double GetDouble(int index)
        {
            switch (index)
            {
                case 0:
                    return ref myField.foo;
                default:
                    throw new Exception();
            }
        }

        public ref int GetInt(int index)
        {
            switch (index)
            {
                case 1:
                    return ref myField.foo5;
                default:
                    throw new Exception();
            }
        }

        public ref char Getchar(int index)
        {
            switch (index)
            {
                case 2:
                    return ref myField.foo6;
                default:
                    throw new Exception();
            }
        }

        public ref double GetFoo()
        {
            return ref myField.foo;
        }

        public void PrintFoo()
        {
            UnityEngine.Debug.Log("private foo: " + myField.foo);
        }
    }
#endif
}