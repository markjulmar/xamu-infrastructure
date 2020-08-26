#define DEBUG // so we can use Debug.WriteLine
//
// DependencyServiceWrapper.cs
//
// Author:
//       Mark Smith <smmark@microsoft.com>
//
// Copyright (c) 2016-2018 Xamarin, Microsoft.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using XamarinUniversity.Infrastructure;

namespace XamarinUniversity.Services
{
    /// <summary>
    /// Wrapper around static Xamarin.Forms DependencyService to allow it to
    /// be turned into a mockable interface for unit testing.
    /// </summary>
    public class DependencyServiceWrapper : IDependencyService
    {
        readonly MethodInfo genericGetMethod;
        static readonly Dictionary<Type, object> DependencyInstances = new Dictionary<Type, object>();

        /// <summary>
        /// Constructor for the DS wrapper.
        /// </summary>
        public DependencyServiceWrapper()
        {
            genericGetMethod = GetType().GetTypeInfo().GetDeclaredMethods("Get").Single(m => m.GetParameters().Length == 1);
        }

        /// <summary>
        /// Retrieve a dependency based on the abstraction <typeparamref name="T"/>.
        /// This extends the default DependencyService capability by allowing this method
        /// to create types which are not registered but have a public constructor.
        /// </summary>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public T Get<T>() where T : class
        {
            return Get<T>(DependencyScope.Global);
        }

        /// <summary>
        /// Retrieve a dependency based on the abstraction <typeparamref name="T"/>.
        /// This extends the default DependencyService capability by allowing this method
        /// to create types which are not registered but have a public constructor.
        /// </summary>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        /// <param name="scope">Scope of the returning object (global or new)</param>
        public T Get<T>(DependencyScope scope) where T : class
        {
            Type targetType = typeof(T);
            if (DependencyInstances.ContainsKey(targetType))
                return DependencyInstances[targetType] as T;

            // Try the underlying DependencyService.
            T value = DependencyService.Get<T>(scope == DependencyScope.Global 
                            ? DependencyFetchTarget.GlobalInstance : DependencyFetchTarget.NewInstance);
            if (value != null)
                return value;

            try
            {
                // Try to create it ourselves.
                TypeInfo typeInfo = targetType.GetTypeInfo();
                if (typeInfo.IsInterface || typeInfo.IsAbstract)
                    return null;

                // Look for a public, default constructor first.
                var ctors = typeInfo.DeclaredConstructors.Where(c => c.IsPublic).ToArray();
                if (ctors.Length == 0)
                    return null;

                var ctor = Array.Find(ctors, c => c.GetParameters().Length == 0);
                if (ctor != null)
                    return Activator.CreateInstance(targetType) as T;

                // Pick the first public constructor found and create any parameters.
                // Note we use the same scope as passed to create all the parameters.
                return Activator.CreateInstance(targetType, ctors[0].GetParameters()
                    .Select(p => genericGetMethod.MakeGenericMethod(p.ParameterType)
                    .Invoke(this, new object[] { scope }))
                    .ToArray()) as T;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DependencyServiceWrapper failed to create {targetType.Name}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Register a specific type as an abstraction
        /// </summary>
        /// <typeparam name="T">The type to register.</typeparam>
        public void Register<T> () where T : class, new()
        {
            DependencyService.Register<T> ();
        }

        /// <summary>
        /// Register a type along with an abstraction type.
        /// </summary>
        /// <typeparam name="T">Abstraction type</typeparam>
        /// <typeparam name="TImpl">Type to create</typeparam>
        public void Register<T, TImpl> () 
            where T : class
            where TImpl : class, T, new()
        {
            DependencyService.Register<T, TImpl> ();
        }

        /// <summary>
        /// Register a specific instance with a type. This extends the
        /// built-in DependencyService by allowing a specific instance to be registered.
        /// </summary>
        /// <typeparam name="T">Type to register</typeparam>
        /// <param name="impl">Implementation</param>
        public void Register<T>(T impl) where T : class
        {
            DependencyInstances.Add(typeof(T), impl);
        }
    }
}