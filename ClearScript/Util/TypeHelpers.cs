﻿// 
// Copyright (c) Microsoft Corporation. All rights reserved.
// 
// Microsoft Public License (MS-PL)
// 
// This license governs use of the accompanying software. If you use the
// software, you accept this license. If you do not accept the license, do not
// use the software.
// 
// 1. Definitions
// 
//   The terms "reproduce," "reproduction," "derivative works," and
//   "distribution" have the same meaning here as under U.S. copyright law. A
//   "contribution" is the original software, or any additions or changes to
//   the software. A "contributor" is any person that distributes its
//   contribution under this license. "Licensed patents" are a contributor's
//   patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// 
//   (A) Copyright Grant- Subject to the terms of this license, including the
//       license conditions and limitations in section 3, each contributor
//       grants you a non-exclusive, worldwide, royalty-free copyright license
//       to reproduce its contribution, prepare derivative works of its
//       contribution, and distribute its contribution or any derivative works
//       that you create.
// 
//   (B) Patent Grant- Subject to the terms of this license, including the
//       license conditions and limitations in section 3, each contributor
//       grants you a non-exclusive, worldwide, royalty-free license under its
//       licensed patents to make, have made, use, sell, offer for sale,
//       import, and/or otherwise dispose of its contribution in the software
//       or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// 
//   (A) No Trademark License- This license does not grant you rights to use
//       any contributors' name, logo, or trademarks.
// 
//   (B) If you bring a patent claim against any contributor over patents that
//       you claim are infringed by the software, your patent license from such
//       contributor to the software ends automatically.
// 
//   (C) If you distribute any portion of the software, you must retain all
//       copyright, patent, trademark, and attribution notices that are present
//       in the software.
// 
//   (D) If you distribute any portion of the software in source code form, you
//       may do so only under this license by including a complete copy of this
//       license with your distribution. If you distribute any portion of the
//       software in compiled or object code form, you may only do so under a
//       license that complies with this license.
// 
//   (E) The software is licensed "as-is." You bear the risk of using it. The
//       contributors give no express warranties, guarantees or conditions. You
//       may have additional consumer rights under your local laws which this
//       license cannot change. To the extent permitted under your local laws,
//       the contributors exclude the implied warranties of merchantability,
//       fitness for a particular purpose and non-infringement.
//       

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.ClearScript.Util
{
    internal static class TypeHelpers
    {
        private static readonly string[] importBlackList =
        {
            "FXAssembly",
            "ThisAssembly",
            "AssemblyRef",
            "SRETW",
            "MatchState",
            "__DynamicallyInvokableAttribute"
        };

        private static readonly Dictionary<TypeCode, TypeCode[]> implicitNumericConversions = new Dictionary<TypeCode, TypeCode[]>
        {
            { TypeCode.SByte, new[] { TypeCode.Int16, TypeCode.Int32, TypeCode.Int64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal } },
            { TypeCode.Byte, new[] { TypeCode.Int16, TypeCode.UInt16, TypeCode.Int32, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal } },
            { TypeCode.Int16, new[] { TypeCode.Int32, TypeCode.Int64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal } },
            { TypeCode.UInt16, new[] { TypeCode.Int32, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal } },
            { TypeCode.Int32, new[] { TypeCode.SByte, TypeCode.Byte, TypeCode.Int16, TypeCode.UInt16, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal } },
            { TypeCode.UInt32, new[] { TypeCode.Int64, TypeCode.UInt64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal } },
            { TypeCode.Int64, new[] { TypeCode.Single, TypeCode.Double, TypeCode.Decimal } },
            { TypeCode.UInt64, new[] { TypeCode.Single, TypeCode.Double, TypeCode.Decimal } },
            { TypeCode.Char, new[] { TypeCode.UInt16, TypeCode.Int32, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal } },
            { TypeCode.Single, new[] { TypeCode.Double } },
        };

        private static readonly HashSet<Type> nullableNumericTypes = new HashSet<Type>
        {
            typeof(char?),
            typeof(sbyte?),
            typeof(byte?),
            typeof(short?),
            typeof(ushort?),
            typeof(int?),
            typeof(uint?),
            typeof(long?),
            typeof(ulong?),
            typeof(float?),
            typeof(double?),
            typeof(decimal?)
        };

        public static bool IsStatic(this Type type)
        {
            return type.IsAbstract && type.IsSealed;
        }

        public static bool IsSpecific(this Type type)
        {
            return !type.IsGenericParameter && !type.ContainsGenericParameters;
        }

        public static bool IsCompilerGenerated(this Type type)
        {
            return type.IsDefined(typeof(CompilerGeneratedAttribute), false);
        }

        public static bool IsFlagsEnum(this Type type)
        {
            return type.IsEnum && type.IsDefined(typeof(FlagsAttribute), false);
        }

        public static bool IsImportable(this Type type)
        {
            if (!type.IsNested && !type.IsSpecialName && !type.IsCompilerGenerated())
            {
                var locator = type.GetLocator();
                return !importBlackList.Contains(locator) && IsValidLocator(locator);
            }

            return false;
        }

        public static bool IsNumeric(this Type type)
        {
            if (type.IsEnum)
            {
                return false;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
            }

            return false;
        }

        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        public static bool IsNullableNumeric(this Type type)
        {
            return nullableNumericTypes.Contains(type);
        }

        public static bool IsUnknownCOMObject(this Type type)
        {
            return type.IsCOMObject && (type.GetInterfaces().Length < 1);
        }

        public static bool IsAssignableFrom(this Type type, ref object value)
        {
            if (type.IsByRef)
            {
                type = type.GetElementType();
            }

            if (type.IsNullable())
            {
                return (value == null) || (Nullable.GetUnderlyingType(type).IsAssignableFrom(ref value));
            }

            if (value == null)
            {
                return !type.IsValueType;
            }

            var valueType = value.GetType();
            if (valueType == type)
            {
                return true;
            }

            if (!type.IsValueType)
            {
                return type.IsAssignableFrom(valueType);
            }

            if (!valueType.IsValueType)
            {
                return false;
            }

            if (type.IsEnum)
            {
                return Enum.GetUnderlyingType(type).IsAssignableFrom(ref value) && (value.DynamicCast<int>() == 0);
            }

            if (valueType.IsEnum)
            {
                return false;
            }

            TypeCode[] typeCodes;
            if (implicitNumericConversions.TryGetValue(Type.GetTypeCode(valueType), out typeCodes))
            {
                var typeCode = Type.GetTypeCode(type);
                if (typeCodes.Contains(Type.GetTypeCode(type)))
                {
                    value = Convert.ChangeType(value, typeCode);
                    return true;
                }
            }

            return false;
        }

        public static bool HasExtensionMethods(this Type type)
        {
            return type.IsDefined(typeof(ExtensionAttribute), false);
        }

        public static string GetRootName(this Type type)
        {
            return StripGenericSuffix(type.Name);
        }

        public static string GetFullRootName(this Type type)
        {
            return StripGenericSuffix(type.FullName);
        }

        public static string GetFriendlyName(this Type type)
        {
            return type.GetFriendlyName(GetRootName);
        }

        public static string GetFullFriendlyName(this Type type)
        {
            return type.GetFriendlyName(GetFullRootName);
        }

        public static string GetLocator(this Type type)
        {
            Debug.Assert(!type.IsNested);
            return type.GetFullRootName();
        }

        public static int GetGenericParamCount(this Type type)
        {
            return type.GetGenericArguments().Count(typeArg => typeArg.IsGenericParameter);
        }

        public static IEnumerable<EventInfo> GetScriptableEvents(this Type type, BindingFlags bindFlags, ScriptAccess defaultAccess)
        {
            var events = type.GetEvents(bindFlags).AsEnumerable();
            if (type.IsInterface)
            {
                events = events.Concat(type.GetInterfaces().SelectMany(interfaceType => interfaceType.GetScriptableEvents(bindFlags, defaultAccess)));
            }

            return events.Where(eventInfo => eventInfo.IsScriptable(defaultAccess));
        }

        public static EventInfo GetScriptableEvent(this Type type, string name, BindingFlags bindFlags, ScriptAccess defaultAccess)
        {
            return type.GetScriptableEvents(bindFlags, defaultAccess).FirstOrDefault(eventInfo => eventInfo.GetScriptName() == name);
        }

        public static IEnumerable<FieldInfo> GetScriptableFields(this Type type, BindingFlags bindFlags, ScriptAccess defaultAccess)
        {
            return type.GetFields(bindFlags).Where(field => field.IsScriptable(defaultAccess));
        }

        public static FieldInfo GetScriptableField(this Type type, string name, BindingFlags bindFlags, ScriptAccess defaultAccess)
        {
            return type.GetScriptableFields(bindFlags, defaultAccess).FirstOrDefault(field => field.GetScriptName() == name);
        }

        public static IEnumerable<MethodInfo> GetScriptableMethods(this Type type, BindingFlags bindFlags, ScriptAccess defaultAccess)
        {
            var methods = type.GetMethods(bindFlags).AsEnumerable();
            if (type.IsInterface)
            {
                methods = methods.Concat(type.GetInterfaces().SelectMany(interfaceType => interfaceType.GetScriptableMethods(bindFlags, defaultAccess)));
                methods = methods.Concat(typeof(object).GetScriptableMethods(bindFlags, defaultAccess));
            }

            return methods.Where(method => method.IsScriptable(defaultAccess));
        }

        public static IEnumerable<MethodInfo> GetScriptableMethods(this Type type, string name, BindingFlags bindFlags, ScriptAccess defaultAccess)
        {
            return type.GetScriptableMethods(bindFlags, defaultAccess).Where(method => method.GetScriptName() == name);
        }


        static ConcurrentDictionary<GetScriptablePropertiesLookup, List<PropertyInfo>> _getScriptablePropertiesCache = new ConcurrentDictionary<GetScriptablePropertiesLookup, List<PropertyInfo>>();

        class GetScriptablePropertiesLookup
        {
            public  readonly Type t;
            public readonly BindingFlags b;
            public readonly ScriptAccess sa;
            public readonly bool ecs;
            int _hc;
            public GetScriptablePropertiesLookup ( Type t, BindingFlags b, ScriptAccess sa , bool ecs)
            {
                this.t = t;
                this.b = b;
                this.sa = sa;
                this.ecs = ecs;
                _hc = t.GetHashCode() + (int)b + (int)sa + (ecs?1:0);
            }


            public override int GetHashCode()
            {
                return _hc;
            }
            public override bool Equals(object obj)
            {
                var compar = (GetScriptablePropertiesLookup)obj;
                return compar.t == this.t &&
                    compar.b == this.b &&
                    compar.sa == this.sa &&
                    compar.ecs == this.ecs;
            }

        }
        class GetScriptablePropertyLookup
        {
            public readonly Type t;
            public readonly string name;
            public readonly BindingFlags b;
            public readonly ScriptAccess sa;
            public readonly bool ecs;
            int _hc;
            public GetScriptablePropertyLookup(Type t, string name, BindingFlags b, ScriptAccess sa, bool ecs)
            {
                this.t = t;
                this.name = name;
                this.b = b;
                this.sa = sa;
                this.ecs = ecs;
                _hc = t.GetHashCode() + (name??string.Empty).GetHashCode () + (int)b + (int)sa + (ecs ? 1 : 0);
            }


            public override int GetHashCode()
            {
                return _hc;
            }
            public override bool Equals(object obj)
            {
                var compar = (GetScriptablePropertyLookup)obj;
                return compar.t == this.t &&
                    compar.name == this.name &&
                    compar.b == this.b &&
                    compar.sa == this.sa &&
                    compar.ecs == this.ecs;
            }

        }

        public static IEnumerable<PropertyInfo> GetScriptableProperties(this Type type, BindingFlags bindFlags, ScriptAccess defaultAccess, bool enableCaseInsensitivity)
        {
            return _getScriptablePropertiesCache.GetOrAdd(new GetScriptablePropertiesLookup(type, bindFlags, defaultAccess, enableCaseInsensitivity), GetScriptableProperties);
        }
        static List<PropertyInfo> GetScriptableProperties(GetScriptablePropertiesLookup lookup)
        {
            var properties = lookup.t.GetProperties(lookup.b).AsEnumerable();

            if (lookup.t.IsInterface)
            {
                properties = properties.Concat(lookup.t.GetInterfaces().SelectMany(interfaceType => interfaceType.GetScriptableProperties(lookup.b, lookup.sa, lookup.ecs)));
            }

            return properties.Where(property => property.IsScriptable(lookup.sa)).ToList();
        }

        public static IEnumerable<PropertyInfo> GetScriptableDefaultProperties(this Type type, BindingFlags bindFlags, ScriptAccess defaultAccess, bool enableCaseInsensitivity)
        {
            var properties = type.GetProperties(bindFlags).AsEnumerable();
            if (type.IsInterface)
            {
                properties = properties.Concat(type.GetInterfaces().SelectMany(interfaceType => interfaceType.GetScriptableProperties(bindFlags, defaultAccess,enableCaseInsensitivity)));
        }

            var defaultMembers = type.GetDefaultMembers();
            return properties.Where(property => property.IsScriptable(defaultAccess) && (defaultMembers.Contains(property) || property.IsDispID(SpecialDispIDs.Default)));
        }

        static ConcurrentDictionary<GetScriptablePropertyLookup, PropertyInfo[]> _getScriptablePropertyCache = new ConcurrentDictionary<GetScriptablePropertyLookup, PropertyInfo[]>();
        public static PropertyInfo[] GetScriptableProperties(this Type type, string name, BindingFlags bindFlags, ScriptAccess defaultAccess, bool enableCaseInsensitivity)
        {
            return _getScriptablePropertyCache.GetOrAdd(new GetScriptablePropertyLookup(type, name, bindFlags, defaultAccess, enableCaseInsensitivity), GetScriptablePropertiesInternal);
        }
        static PropertyInfo[] GetScriptablePropertiesInternal(GetScriptablePropertyLookup lookup)
        {
            if (lookup.ecs)
            {
                return lookup.t.GetScriptableProperties(lookup.b, lookup.sa, lookup.ecs).Where(property => string.Equals(property.GetScriptName(), lookup.name, StringComparison.InvariantCultureIgnoreCase)).Distinct(PropertySignatureComparer.Instance).ToArray();
            }
            return lookup.t .GetScriptableProperties(lookup.b, lookup.sa, lookup.ecs).Where(property => property.GetScriptName() == lookup.name).Distinct(PropertySignatureComparer.Instance).ToArray();
        }

        public static PropertyInfo GetScriptableProperty(this Type type, string name, BindingFlags bindFlags, object[] bindArgs, ScriptAccess defaultAccess, bool enableCaseInsensitivity)
        {
            var candidates = type.GetScriptableProperties(name, bindFlags, defaultAccess, enableCaseInsensitivity);
            return SelectProperty(candidates, bindFlags, bindArgs);
        }
       
        public static PropertyInfo GetScriptableDefaultProperty(this Type type, BindingFlags bindFlags, object[] bindArgs, ScriptAccess defaultAccess, bool enableCaseInsensitivity)
                {
            var candidates = type.GetScriptableDefaultProperties(bindFlags, defaultAccess, enableCaseInsensitivity).Distinct(PropertySignatureComparer.Instance).ToArray();
            return SelectProperty(candidates, bindFlags, bindArgs);
            }

        public static IEnumerable<Type> GetScriptableNestedTypes(this Type type, BindingFlags bindFlags, ScriptAccess defaultAccess)
        {
            return type.GetNestedTypes(bindFlags).Where(nestedType => nestedType.IsScriptable(defaultAccess));
        }

        public static object CreateInstance(this Type type, params object[] args)
        {
            return type.CreateInstance(BindingFlags.Public, args);
        }

        public static object CreateInstance(this Type type, BindingFlags flags, params object[] args)
        {
            return type.InvokeMember(null, BindingFlags.CreateInstance | BindingFlags.Instance | flags, null, null, args, CultureInfo.InvariantCulture);
        }

        public static Type MakeSpecificType(this Type template, params Type[] typeArgs)
        {
            Debug.Assert(template.GetGenericParamCount() <= typeArgs.Length);
            return template.ApplyTypeArguments(typeArgs);
        }

        public static Type ApplyTypeArguments(this Type type, params Type[] typeArgs)
        {
            if (!type.IsSpecific())
            {
                Debug.Assert(typeArgs.All(typeArg => typeArg.IsSpecific()));

                var finalTypeArgs = (Type[])type.GetGenericArguments().Clone();
                for (int finalIndex = 0, index = 0; finalIndex < finalTypeArgs.Length; finalIndex++)
                {
                    if (finalTypeArgs[finalIndex].IsGenericParameter)
                    {
                        finalTypeArgs[finalIndex] = typeArgs[index++];
                        if (index >= typeArgs.Length)
                        {
                            break;
                        }
                    }
                }

                return type.GetGenericTypeDefinition().MakeGenericType(finalTypeArgs);
            }

            return type;
        }

        public static bool IsValidLocator(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.All(IsValidLocatorChar);
        }

        public static HostType ImportType(string typeName, string assemblyName, bool useAssemblyName, object[] hostTypeArgs)
        {
            if (!IsValidLocator(typeName))
            {
                throw new ArgumentException("Invalid type name", "typeName");
            }

            if (useAssemblyName && string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new ArgumentException("Invalid assembly name", "assemblyName");
            }

            if (!hostTypeArgs.All(arg => arg is HostType))
            {
                throw new ArgumentException("Invalid generic type argument");
            }

            var typeArgs = hostTypeArgs.Cast<HostType>().Select(hostType => hostType.GetTypeArg()).ToArray();
            return ImportType(typeName, assemblyName, useAssemblyName, typeArgs);
        }

        public static HostType ImportType(string typeName, string assemblyName, bool useAssemblyName, Type[] typeArgs)
        {
            const int maxTypeArgCount = 16;

            if ((typeArgs != null) && (typeArgs.Length > 0))
            {
                var template = ImportType(typeName, assemblyName, useAssemblyName, typeArgs.Length);
                if (template == null)
                {
                    throw new TypeLoadException(MiscHelpers.FormatInvariant("Could not find a matching generic type definition for '{0}'", typeName));
                }

                return HostType.Wrap(template.MakeSpecificType(typeArgs));
            }

            var type = ImportType(typeName, assemblyName, useAssemblyName, 0);

            // ReSharper disable RedundantEnumerableCastCall

            // the OfType<>() call is not redundant; it filters out null elements
            var counts = Enumerable.Range(1, maxTypeArgCount);
            var templates = counts.Select(count => ImportType(typeName, assemblyName, useAssemblyName, count)).OfType<Type>().ToArray();

            // ReSharper restore RedundantEnumerableCastCall

            if (templates.Length < 1)
            {
                if (type == null)
                {
                    throw new TypeLoadException(MiscHelpers.FormatInvariant("Could not find a specific type or generic type definition for '{0}'", typeName));
                }

                return HostType.Wrap(type);
            }

            if (type == null)
            {
                return HostType.Wrap(templates);
            }

            return HostType.Wrap(new[] { type }.Concat(templates).ToArray());
        }

        private static Type ImportType(string typeName, string assemblyName, bool useAssemblyName, int typeArgCount)
        {
            var assemblyQualifiedName = GetFullTypeName(typeName, assemblyName, useAssemblyName, typeArgCount);
            var type = Type.GetType(assemblyQualifiedName);
            return ((type != null) && useAssemblyName && (type.AssemblyQualifiedName != assemblyQualifiedName)) ? null : type;
        }

        private static string GetFriendlyName(this Type type, Func<Type, string> getBaseName)
        {
            Debug.Assert(type.IsSpecific());
            if (type.IsArray)
            {
                var commas = new string(Enumerable.Repeat(',', type.GetArrayRank() - 1).ToArray());
                return MiscHelpers.FormatInvariant("{0}[{1}]", type.GetElementType().GetFriendlyName(getBaseName), commas);
            }

            var typeArgs = type.GetGenericArguments();
            var parentPrefix = string.Empty;
            if (type.IsNested)
            {
                var parentType = type.DeclaringType.MakeSpecificType(typeArgs);
                parentPrefix = parentType.GetFriendlyName(getBaseName) + ".";
                typeArgs = typeArgs.Skip(parentType.GetGenericArguments().Length).ToArray();
            }

            if (typeArgs.Length < 1)
            {
                return MiscHelpers.FormatInvariant("{0}{1}", parentPrefix, getBaseName(type));
            }

            var name = getBaseName(type.GetGenericTypeDefinition());
            var paramList = String.Join(",", typeArgs.Select(typeArg => typeArg.GetFriendlyName(getBaseName)));
            return MiscHelpers.FormatInvariant("{0}{1}<{2}>", parentPrefix, name, paramList);
        }

        private static string GetFullTypeName(string name, string assemblyName, bool useAssemblyName, int typeArgCount)
        {
            var fullTypeName = name;

            if (typeArgCount > 0)
            {
                fullTypeName += MiscHelpers.FormatInvariant("`{0}", typeArgCount);
            }

            if (useAssemblyName)
            {
                fullTypeName += MiscHelpers.FormatInvariant(", {0}", AssemblyTable.GetFullAssemblyName(assemblyName));
            }

            return fullTypeName;
        }

        private static bool IsValidLocatorChar(char ch)
        {
            return char.IsLetterOrDigit(ch) || (ch == '_') || (ch == '.');
        }

        private static string StripGenericSuffix(string name)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(name));
            var index = name.LastIndexOf('`');
            return (index >= 0) ? name.Substring(0, index) : name;
        }

        private static Type GetPropertyIndexType(object bindArg)
        {
            var hostTarget = bindArg as HostTarget;
            if (hostTarget != null)
            {
                return hostTarget.Type;
            }

            if (bindArg != null)
            {
                return bindArg.GetType();
            }

            throw new InvalidOperationException("Property index value must not be null");
        }

        private static PropertyInfo SelectProperty(PropertyInfo[] candidates, BindingFlags bindFlags, object[] bindArgs)
        {
            if (candidates.Length < 1)
            {
                return null;
            }

            var result = Type.DefaultBinder.SelectProperty(bindFlags, candidates, null, bindArgs.Select(GetPropertyIndexType).ToArray(), null);
            if (result != null)
            {
                return result;
            }

            // the default binder fails to bind to some COM properties because of by-ref parameter types
            if (candidates.Length == 1)
            {
                var parameters = candidates[0].GetIndexParameters();
                if ((bindArgs.Length == parameters.Length) || ((bindArgs.Length > 0) && (parameters.Length >= bindArgs.Length)))
                {
                    return candidates[0];
                }
            }

            return null;
        }

        #region Nested type: PropertySignatureComparer

        private class PropertySignatureComparer : IEqualityComparer<PropertyInfo>
        {
            private static readonly PropertySignatureComparer instance = new PropertySignatureComparer();

            public static PropertySignatureComparer Instance { get { return instance; } }

            #region IEqualityComparer<PropertyInfo> implementation

            public bool Equals(PropertyInfo first, PropertyInfo second)
            {
                var firstParamTypes = first.GetIndexParameters().Select(param => param.ParameterType);
                var secondParamTypes = second.GetIndexParameters().Select(param => param.ParameterType);
                return firstParamTypes.SequenceEqual(secondParamTypes);
            }

            public int GetHashCode(PropertyInfo property)
            {
                var hashCode = 0;

                var parameters = property.GetIndexParameters();
                foreach (var param in parameters)
                {
                    hashCode = unchecked((hashCode * 31) + param.ParameterType.GetHashCode());
                }

                return hashCode;
            }

            #endregion
        }

        #endregion
    }
}
