using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using Simple;

namespace Simple.Reflection
{
    /// <summary>
    /// This is a static class aimed to help with tasks related 
    /// to reflection such getting the calling method, type-
    /// reflection help methods, etc.
    /// </summary>
    /// <remarks>
    ///     <why>Code reuse</why>
    ///     <when created="23/05/2006" updated="23/05/2006"/>
    ///     <where>All .Net (desktop) based apps</where>
    ///     <who>Yair Cohen</who>
    /// </remarks>
    public static class ReflectionHelper
    {
        /// <summary>
        /// Set a value to property in the given instance.
        /// if property set is not found, it will regard it.
        /// </summary>
        /// <param name="instance">
        ///     instance object
        /// </param>
        /// <param name="propertyName">
        ///     name of property to set
        /// </param>
        /// <param name="value">
        ///     value to set
        /// </param>
        public static void SetPropertyValue(object instance, string propertyName, object value)
        {
            SetPropertyValue(instance, propertyName, value, false);
        }

        /// <summary>
        /// Set a value to property in the given instance
        /// </summary>
        /// <param name="instance">
        ///     instance object
        /// </param>
        /// <param name="propertyName">
        ///     name of property to set
        /// </param>
        /// <param name="value">
        ///     value to set
        /// </param>
        /// <param name="forceSet">
        ///     Force set the property, throw exception if not exists
        /// </param>
        public static void SetPropertyValue(object instance, string propertyName, object value, bool forceSet)
        {
            if (instance == null)
            {
                return;
            }

            PropertyInfo propInfo = instance.GetType().GetProperty(propertyName);
            if (propInfo != null && propInfo.CanWrite)
            {
                propInfo.SetValue(instance, value, null);
            }
            else
            {
                if (propInfo == null || forceSet)
                {
                    throw new IndexOutOfRangeException(string.Format("Property '{0}' not found or has no set method, in type '{1}'.", propertyName, instance.GetType()));
                }
            }
        }

        /// <summary>
        /// Invoke generic method on an instance
        /// </summary>
        /// <param name="instance">instnace to play on</param>
        /// <param name="type">type of the instance</param>
        /// <param name="name">name of method</param>
        /// <param name="genericParams">type parameters for the method</param>
        /// <param name="parameters">parameters to pass to the method</param>
        /// <returns>the return value of the method</returns>
        /// <exception cref="MissingMethodException">method not exists</exception>
        /// <exception cref="InvalidOperationException">The method is not a generic one</exception>
        public static object InvokeGenericMethod(
                            object instance,
                            Type type, 
                            string name ,
                            Type[] genericParams, 
                            params object[] parameters)
        {

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            // get the  method
            MethodInfo method = type.GetMethod(name);

            if (method == null)
            {
                throw new MissingMethodException(type.Name, name);
            }

            // Get the generic method and invoke it with  parameters
            var generic = method.MakeGenericMethod(genericParams);
            
            return generic.Invoke(instance, parameters);

        }

        public static Type UnderlyingType(this Type nullableType)
        {
            if (nullableType.IsNullable())
            {
                return nullableType.GetGenericArguments()[0];
            }
            else
            {
                return nullableType;
            }
        }

        public static bool IsNullable(this Type theType)
        {
            return (theType.IsGenericType && theType.
              GetGenericTypeDefinition().Equals
              (typeof(Nullable<>)));
        }


        /// <summary>
        /// Invoke generic method on an instance
        /// </summary>
        /// <param name="instance">instnace to play on</param>
        /// <param name="name">name of method</param>
        /// <param name="genericParams">type parameters for the method</param>
        /// <param name="parameters">parameters to pass to the method</param>
        /// <returns>the return value of the method</returns>
        /// <exception cref="MissingMethodException">method not exists</exception>
        /// <exception cref="InvalidOperationException">The method is not a generic one</exception>
        public static object InvokeGenericMethod(
                    object instance,
                    string name,
                    Type[] genericParams,
                    params object[] parameters)
        {

            return InvokeGenericMethod(instance,
                                        instance.GetType(),
                                        name,
                                        genericParams,
                                        parameters);
        }

        /// <summary>
        /// Identifies the name of a calling method that couldn't be extracted
        /// </summary>
        public const string CALLING_UNAVAILIBLE = "CallingMethodIsUnavailible";

        /// <summary>
        /// Get the calling method name of the caller 
        /// in the format of [Type.Name].[MethodName] 
        /// (without the brackets)
        /// </summary>
        /// <returns>
        ///     The calling method name if found, 
        ///     else return <see cref="ReflectionHelper.CALLING_UNAVAILIBLE"/>
        /// </returns>
        public static string GetCallingMethodFullName()
        {
            return GetCallingMethodFullName(4);
        }


        /// <summary>
        /// Get the calling method name of the caller 
        /// in the format of [Type.Name].[MethodName] 
        /// (without the brackets)
        /// </summary>
        /// <returns>
        ///     The calling method name if found, 
        ///     else return <see cref="ReflectionHelper.CALLING_UNAVAILIBLE"/>
        /// </returns>
        public static string GetCallingMethodFullName(int skip)
        {
            MethodBase callingMethod = GetCallingMethod(skip);
            if (callingMethod != null)
            {
                return string.Format("{0}.{1}", callingMethod.ReflectedType.Name, callingMethod.Name);
            }
            else
            {
                return CALLING_UNAVAILIBLE;
            }
        }

        /// <summary>
        /// Get the calling method of the caller 
        /// NOTE: if we got only the top of stacktrace we would end
        /// in returning the calling method for this method...
        /// </summary>
        /// <returns></returns>
        public static MethodBase GetCallingMethod()
        {
            return GetCallingMethod(2);
        }


        /// <summary>
        /// Get the calling method of the caller 
        /// NOTE: if we got only the top of stacktrace we would end
        /// in returning the calling method for this method...
        /// </summary>
        /// <param name="skipOnStack">how much steps we are skipping in the stacktrace
        /// this parameter is used by the <see cref="GetCallingMethodFullName"/> function
        /// so it can skip on itself
        /// </param>
        /// <returns>The calling method, if found, else returns null</returns>
        public static MethodBase GetCallingMethod(int skipOnStack)
        {
            
            StackTrace stack = new StackTrace(skipOnStack);
            if (stack.FrameCount > 0)
            {
                StackFrame frame = stack.GetFrame(0);
                MethodBase current = frame.GetMethod();

                return current;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Finds an attibute on a type. 
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <typeparam name="TAttr"></typeparam>
        /// 
        /// <returns>
        ///     if Attributes of <typeparamref name="TAttr"/> are defined, return 
        ///     them, else, return null
        /// </returns>
        public static TAttr[] FindAttributes<TType, TAttr>() where TAttr : System.Attribute
        {
            return FindAttributes<TType, TAttr>(false);
        }

        /// <summary>
        /// Finds an attibute on a type. 
        /// </summary>
        /// <typeparam name="TType">type to inspect</typeparam>
        /// <typeparam name="TAttr">type of attribute to search</typeparam>
        /// <param name="inherited">Specifies wether to search this member's 
        /// inheritance chain to find the attributes</param>
        /// <returns>
        ///     if Attributes of <typeparamref name="TAttr"/> are defined, return 
        ///     them, else, return null
        /// </returns>
        public static TAttr[] FindAttributes<TType, TAttr>(bool inherited) where TAttr : System.Attribute
        {
            Type    reflected = typeof(TType),
                    attribute = typeof(TAttr);

            if (reflected.IsDefined(attribute, inherited))
            {
                object[] results = reflected.GetCustomAttributes(attribute, true);
                if (results != null && results.Length > 0)
	            {
                    TAttr[] attrs = new TAttr[results.Length];
                    Array.Copy(results, attrs, results.Length);
                    return attrs;
	            }
            }

            // not defined ? return null
            return null;
        }

        /// <summary>
        /// Finds an attibute on a method. 
        /// </summary>
        /// <typeparam name="TAttr">type of attribute to search</typeparam>
        /// inheritance chain to find the attributes</param>
        /// <returns>
        ///     if Attributes of <typeparamref name="TAttr"/> are defined, return 
        ///     them, else, return null
        /// </returns>
        public static TAttr[] FindAttributes<TAttr>(MethodBase method)
        {
            return FindAttributes<TAttr>(method, false);
        }

        /// <summary>
        /// Finds an attibute on a method. 
        /// </summary>
        /// <typeparam name="TAttr">type of attribute to search</typeparam>
        /// <param name="inherited">Specifies wether to search this member's 
        /// inheritance chain to find the attributes</param>
        /// <returns>
        ///     if Attributes of <typeparamref name="TAttr"/> are defined, return 
        ///     them, else, return null
        /// </returns>
        public static TAttr[] FindAttributes<TAttr>(MethodBase method, bool inherited)
        {
            Type attribute = typeof(TAttr);

            if (method.IsDefined(attribute, inherited))
            {
                object[] results = method.GetCustomAttributes(attribute, true);
                if (results != null && results.Length > 0)
                {
                    TAttr[] attrs = new TAttr[results.Length];
                    Array.Copy(results, attrs, results.Length);
                    return attrs;
                }
            }

            // not defined ? return null
            return null;
        }


        /// <summary>
        /// Create instance of the specified type or derived, the first found, if it 
        /// marked with one (first) of the attributes specified
        /// </summary>
        /// <typeparam name="TType">type to search</typeparam>
        /// <param name="assembly">Assembly to inspect</param>
        /// <param name="attributes">attributes that the type should be marked with</param>
        /// <returns>instance of the first found type meeting the criterias</returns>
        public static TType GetInstanceOfTypeOrDerivedFromAssemblyPublicTypes<TType>(Assembly assembly, params Type[] attributes)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }
            return GetInstanceOfTypeOrDerivedFromTypes<TType>(assembly.GetExportedTypes(), attributes);
        }

        /// <summary>
        /// Create instance of the specified type or derived, the first found, if it 
        /// marked with one (first) of the attributes specified
        /// </summary>
        /// <typeparam name="TType">type to search</typeparam>
        /// <param name="assembly">Assembly to inspect</param>
        /// <param name="attributes">attributes that the type should be marked with</param>
        /// <returns>instance of the first found type meeting the criterias</returns>
        public static TType GetInstanceOfTypeOrDerivedFromAssembly<TType>(Assembly assembly, params Type[] attributes)
        {
            
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }
            return GetInstanceOfTypeOrDerivedFromTypes<TType>(assembly.GetTypes(), attributes);
        }

        /// <summary>
        /// Create instance of the specified type or derived, the first found, if it 
        /// marked with one (first) of the attributes specified
        /// </summary>
        /// <typeparam name="TType">type to search</typeparam>
        /// <param name="types">types to search in</param>
        /// <param name="attributes">attributes that the type should be marked with</param>
        /// <returns>instance of the first found type meeting the criterias, if not found, return default(TType)</returns>
        public static TType GetInstanceOfTypeOrDerivedFromTypes<TType>(Type[] types, params Type[] attributes) 
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }
            foreach (Type type in  types)
            {
                if (!type.IsAbstract && 
                    typeof(TType).IsAssignableFrom(type))
                {
                    if (attributes.Length > 0)
                    {
                        foreach (Type attribute in attributes)
                        {
                            if (type.IsDefined(attribute, false))
                            {
                                return (TType)Activator.CreateInstance(type);
                            }
                        }
                    }
                    else
                    {
                        return (TType)Activator.CreateInstance(type);
                    }
                }
            }

            // got here ? not found any type, return null
            return default(TType);

        }


        /// <summary>
        /// Get the public properties of a type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static PropertyInfo[] GetPublicProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }


        public static T GetDefinedAttribute<T>(this MemberInfo member)
            where T : System.Attribute
        {
            return member.GetDefinedAttribute<T>(true);
        }

        public static T GetDefinedAttribute<T>(this MemberInfo member, bool inherit)
            where T : System.Attribute
        {
            return member.GetCustomAttributes(typeof (T), inherit)[0] as T;
        }

        /// <summary>
        /// Run an action against all public properties of a type
        /// </summary>
        /// <param name="type">contextual Type</param>
        /// <param name="action">action to perform</param>
        /// <param name="filterForAttribute">type of attribute to filter</param>
        public static void ForEachPublicProperty(this Type type, Action<PropertyInfo> action, Type filterForAttribute)
        {
            var properties = GetPublicProperties(type);

            var index = 0;
            while (index < properties.Length)
            {
                if (filterForAttribute == null ||
                    properties[index].IsDefined(filterForAttribute, true))
                {
                    // run the action on each property matching the attribute
                    action(properties[index]);

                }
                index++;
            }
        }

        public static object GetPropertyValue(object instance, string name)
        {

            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            var type = instance.GetType();
            var property = type.GetProperty(name);
            
            if (property == null)
            {
                throw new ArgumentException($"Type '{type.Name}' does not contain property '{name}'.");
            }

            return property.GetValue(instance, null);
        }


    }
}
