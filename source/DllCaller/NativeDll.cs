using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace DllCaller
{
    public static class NativeDll
    {
        public static T CreateInstance<T>()
            where T : class
        {
            return CreateInstance<T>(false);
        }

        public static T CreateInstance<T>(bool assemblyOutput)
            where T : class
        {
            var type = typeof(T);
            if (!type.IsInterface)
            {
                throw new InvalidOperationException("Type '" + type + "' is not interface.");
            }

            var globalNativeMethodAttr = type.GetAttibute<GlobalNativeMethodAttribute>();

            AssemblyName asmName = new AssemblyName(type.Name + "Impl");

            AssemblyBuilder dynamicAsm;
            ModuleBuilder dynamicMod;
            if (assemblyOutput)
            {
                dynamicAsm = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave);
                dynamicMod = dynamicAsm.DefineDynamicModule(asmName.Name, asmName.Name + ".dll");
            }
            else
            {
                dynamicAsm = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
                dynamicMod = dynamicAsm.DefineDynamicModule(asmName.Name);
            }

            TypeBuilder typeBuilder = dynamicMod.DefineType(type.Name + "ImplClass", TypeAttributes.Public | TypeAttributes.AnsiClass);
            typeBuilder.AddInterfaceImplementation(type);

            var dllName = LocateDllPath<T>();

            var setPInvokeDataMethod = typeof(TypeBuilder).GetMethod("SetPInvokeData", BindingFlags.NonPublic | BindingFlags.Static);
            var setTokenMethod = typeof(MethodBuilder).GetMethod("SetToken", BindingFlags.NonPublic | BindingFlags.Instance);
            var runtimeModule = typeof(ModuleBuilder).GetMethod("GetNativeHandle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(dynamicMod, null);

            var methods = type.GetMethods();
            foreach (var method in methods)
            {
                var nativeMethodAttr = method.GetAttibute<NativeMethodAttribute>() ?? (NativeMethodAttributeBase)globalNativeMethodAttr;
                if (nativeMethodAttr == null)
                {
                    throw new InvalidOperationException("Method '" + method.Name + "' has not NativeMethodAttribute.");
                }

                var methodParams = method.GetParameters();
                Type[] parameterTypes = methodParams.Select(p => p.ParameterType).ToArray();

                MethodBuilder pInvokeMethodBuilder =
                    GeneratePInvokeMethod(
                        typeBuilder,
                        dllName,
                        setPInvokeDataMethod,
                        runtimeModule,
                        setTokenMethod,
                        method,
                        nativeMethodAttr,
                        methodParams,
                        parameterTypes);

                GenerateInstanceMethod(typeBuilder, method, methodParams, parameterTypes, pInvokeMethodBuilder);
            }

            // Create the class and test the method.
            Type t = typeBuilder.CreateType();

            if (assemblyOutput)
            {
                // Produce the .dll file.
                Console.WriteLine("Saving: " + asmName.Name + ".dll");
                dynamicAsm.Save(asmName.Name + ".dll");
            }

            return (T)Activator.CreateInstance(t);
        }


        private static TAttribute GetAttibute<TAttribute>(this MemberInfo obj)
            where TAttribute : Attribute
        {
            return obj.GetCustomAttributes(typeof(TAttribute), false).Cast<TAttribute>().FirstOrDefault();
        }

        private static string LocateDllPath<T>()
        {
            var attr = typeof(T).GetAttibute<NativeDllLocationAttribute>();
            var typeAttr = typeof(T).GetAttibute<NativeDllLocatorAttribute>();
            if (attr == null && typeAttr == null)
            {
                throw new InvalidOperationException("NativeDllLocator attribute not found.");
            }

            string libPath;
            if (typeAttr != null)
            {
                var locatorType = typeAttr.Type;
                if (!typeof(INativeDllLocator).IsAssignableFrom(locatorType))
                {
                    throw new InvalidOperationException("NativeDllLocator class not implemented INativeDllLocator interface.");
                }

                var locator = (INativeDllLocator)Activator.CreateInstance(locatorType);
                libPath = locator.Locate(typeof(T));
            }
            else
            {
                libPath = IntPtr.Size == 8 ? attr.X64 : attr.X86;
            }

            return libPath;
        }


        private static MethodBuilder GenerateInstanceMethod(TypeBuilder typeBuilder, MethodInfo method, ParameterInfo[] methodParams, Type[] parameterTypes, MethodBuilder pInvokeMethodBuilder)
        {
            var instanceMethodBuilder = typeBuilder.DefineMethod(
                method.Name,
                MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                method.ReturnType,
                parameterTypes);

            for (int i = 0; i < methodParams.Length; i++)
            {
                var p = methodParams[i];
                instanceMethodBuilder.DefineParameter(i + 1, p.Attributes, p.Name);
            }

            ImplementInvokeMethod(instanceMethodBuilder, pInvokeMethodBuilder, methodParams);

            return instanceMethodBuilder;
        }

        private static MethodBuilder GeneratePInvokeMethod(TypeBuilder typeBuilder, string dllName, MethodInfo setPInvokeDataMethod, object runtimeModule, MethodInfo setTokenMethod, MethodInfo method, NativeMethodAttributeBase nativeMethodAttr, ParameterInfo[] methodParams, Type[] parameterTypes)
        {
            MethodBuilder pInvokeMethodBuilder = typeBuilder.DefineMethod(
                "PIvk_" + method.Name,
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl | MethodAttributes.HideBySig,
                CallingConventions.Standard,
                method.ReturnType,
                parameterTypes);

            // Set PInvokeData.
            var entryName = nativeMethodAttr.EntryPoint ?? method.Name;
            var token = pInvokeMethodBuilder.GetToken();
            PInvokeMap linkFlags = GetLinkFlags(nativeMethodAttr);
            setPInvokeDataMethod.Invoke(null, new object[] { runtimeModule, dllName, entryName, token.Token, (int)linkFlags });
            setTokenMethod.Invoke(pInvokeMethodBuilder, new object[] { token });

            // Add PreserveSig to the method implementation flags.
            // NOTE: If this line is commented out, the return value will be zero when the method is invoked.
            pInvokeMethodBuilder.SetImplementationFlags(pInvokeMethodBuilder.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);

            var returnParameter = pInvokeMethodBuilder.DefineParameter(0, method.ReturnParameter.Attributes, null);
            var returnParameterAttrs = CustomAttributeData.GetCustomAttributes(method.ReturnParameter);
            foreach (var attr in returnParameterAttrs)
            {
                returnParameter.SetCustomAttribute(CreateAttributeBuilder(attr));
            }

            for (int i = 0; i < methodParams.Length; i++)
            {
                var p = methodParams[i];
                var paramBuilder = pInvokeMethodBuilder.DefineParameter(i + 1, p.Attributes, p.Name);
                foreach (var paramAttr in CustomAttributeData.GetCustomAttributes(p))
                {
                    paramBuilder.SetCustomAttribute(CreateAttributeBuilder(paramAttr));
                }
            }

            // The PInvoke method does not have a method body. 
            return pInvokeMethodBuilder;
        }

        private static void ImplementInvokeMethod(MethodBuilder instanceMethodBuilder, MethodBuilder pInvokeMethodBuilder, ParameterInfo[] methodParams)
        {
            var il = instanceMethodBuilder.GetILGenerator();
            for (int i = 0; i < methodParams.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        il.Emit(OpCodes.Ldarg_1);
                        break;
                    case 1:
                        il.Emit(OpCodes.Ldarg_2);
                        break;
                    case 2:
                        il.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        il.Emit(OpCodes.Ldarg_S, (byte)(i + 1));
                        break;
                }
            }

            il.EmitCall(OpCodes.Call, pInvokeMethodBuilder, null);
            il.Emit(OpCodes.Ret);
        }

        private static PInvokeMap GetLinkFlags(NativeMethodAttributeBase nativeMethodAttr)
        {
            PInvokeMap linkFlags = 0;

            if (nativeMethodAttr.ExactSpelling)
            {
                linkFlags |= PInvokeMap.NoMangle;
            }

            switch (nativeMethodAttr.CallingConvention)
            {
                case CallingConvention.Winapi:
                    linkFlags |= PInvokeMap.CallConvWinapi;
                    break;
                case CallingConvention.Cdecl:
                    linkFlags |= PInvokeMap.CallConvCdecl;
                    break;
                case CallingConvention.StdCall:
                    linkFlags |= PInvokeMap.CallConvStdcall;
                    break;
                case CallingConvention.ThisCall:
                    linkFlags |= PInvokeMap.CallConvThiscall;
                    break;
                case CallingConvention.FastCall:
                    linkFlags |= PInvokeMap.CallConvFastcall;
                    break;
            }

            switch (nativeMethodAttr.CharSet)
            {
                case CharSet.None:
                    linkFlags |= PInvokeMap.CharSetNotSpec;
                    break;
                case CharSet.Ansi:
                    linkFlags |= PInvokeMap.CharSetAnsi;
                    break;
                case CharSet.Unicode:
                    linkFlags |= PInvokeMap.CharSetUnicode;
                    break;
                case CharSet.Auto:
                    linkFlags |= PInvokeMap.CharSetAuto;
                    break;
            }

            return linkFlags;
        }

        private static CustomAttributeBuilder CreateAttributeBuilder(CustomAttributeData attrData)
        {
            List<object> namedFieldValues = new List<object>();
            List<FieldInfo> fields = new List<FieldInfo>();
            List<object> constructorArguments = new List<object>();
            foreach (CustomAttributeTypedArgument cata in attrData.ConstructorArguments)
            {
                constructorArguments.Add(cata.Value);
            }

            if (attrData.NamedArguments.Count > 0)
            {
                var attrType = attrData.Constructor.DeclaringType;
                FieldInfo[] possibleFields = attrType.GetFields();

                foreach (CustomAttributeNamedArgument cana in attrData.NamedArguments)
                {
                    if (attrType == typeof(MarshalAsAttribute) && cana.MemberInfo.Name == "MarshalType")
                    {
                        continue;
                    }

                    foreach (var field in possibleFields)
                    {
                        if (field.Name.Equals(cana.MemberInfo.Name, StringComparison.Ordinal))
                        {
                            fields.Add(field);
                            namedFieldValues.Add(cana.TypedValue.Value);
                            break;
                        }
                    }
                }
            }

            if (namedFieldValues.Count > 0)
            {
                return new CustomAttributeBuilder(attrData.Constructor, constructorArguments.ToArray(), fields.ToArray(), namedFieldValues.ToArray());
            }
            else
            {
                return new CustomAttributeBuilder(attrData.Constructor, constructorArguments.ToArray());
            }
        }


        // This Enum matchs the CorPinvokeMap defined in CorHdr.h
        [Serializable]
        private enum PInvokeMap
        {
            NoMangle = 0x0001,   // Pinvoke is to use the member name as specified.
            CharSetMask = 0x0006,   // Heuristic used in data type & name mapping.
            CharSetNotSpec = 0x0000,
            CharSetAnsi = 0x0002,
            CharSetUnicode = 0x0004,
            CharSetAuto = 0x0006,

            PinvokeOLE = 0x0020,   // Heuristic: pinvoke will return hresult, with return value becoming the retval param. Not relevant for fields. 
            SupportsLastError = 0x0040,   // Information about target function. Not relevant for fields.

            BestFitMask = 0x0030,
            BestFitEnabled = 0x0010,
            BestFitDisabled = 0x0020,
            BestFitUseAsm = 0x0030,

            ThrowOnUnmappableCharMask = 0x3000,
            ThrowOnUnmappableCharEnabled = 0x1000,
            ThrowOnUnmappableCharDisabled = 0x2000,
            ThrowOnUnmappableCharUseAsm = 0x3000,

            // None of the calling convention flags is relevant for fields.
            CallConvMask = 0x0700,
            CallConvWinapi = 0x0100,   // Pinvoke will use native callconv appropriate to target windows platform.
            CallConvCdecl = 0x0200,
            CallConvStdcall = 0x0300,
            CallConvThiscall = 0x0400,   // In M9, pinvoke will raise exception.
            CallConvFastcall = 0x0500,
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public abstract class NativeMethodAttributeBase : Attribute
    {
        public string EntryPoint;
        public bool ExactSpelling;
        public CallingConvention CallingConvention;
        public bool BestFitMapping;
        public CharSet CharSet;
        public bool SetLastError;
        public bool ThrowOnUnmappableChar;
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class NativeMethodAttribute : NativeMethodAttributeBase
    {
    }

    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class GlobalNativeMethodAttribute : NativeMethodAttributeBase
    {
    }

    [AttributeUsage(AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
    public sealed class NativeDllLocationAttribute : Attribute
    {
        public NativeDllLocationAttribute(string x86, string x64)
        {
            this.X86 = x86;
            this.X64 = x64;
        }

        public string X86 { get; private set; }
        public string X64 { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class NativeDllLocatorAttribute : Attribute
    {
        public NativeDllLocatorAttribute(Type type)
        {
            this.Type = type;
        }

        public Type Type { get; private set; }
    }

    public interface INativeDllLocator
    {
        string Locate(Type type);
    }
}
