﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using XamarinUniversity.Infrastructure;

[assembly:InternalsVisibleTo("XamU.Infrastructure.Tests")]

namespace XamarinUniversity.Services
{
    /// <summary>
    /// Identifies which services you want to register during the Init call.
    /// </summary>
    [Flags]
    public enum RegisterBehavior
    {
        /// <summary>
        /// Register the default (Forms) navigation service
        /// </summary>
        Navigation = 1,
        /// <summary>
        /// Register the default (Forms) message visualizer
        /// </summary>
        MessageVisualizer = 2
    }

    /// <summary>
    /// Static class to initialize the library
    /// </summary>
    public static class XamUInfrastructure
    {
        private static bool initialized;
        private static IDependencyService serviceLocator;

        /// <summary>
        /// Used to reset for unit tests.
        /// </summary>
        internal static void Reset()
        {
            initialized = false;
            serviceLocator = null;
        }

        /// <summary>
        /// This allows you to retrieve and customize the global dependency service
        /// used by the library (and app).
        /// </summary>
        /// <value>The service locator.</value>
        public static IDependencyService ServiceLocator => serviceLocator ?? (serviceLocator = new DependencyServiceWrapper ());

        /// <summary>
        /// Registers the known services with the ServiceLocator type.
        /// </summary>
        /// <returns>IDependencyService</returns>
        public static IDependencyService Init ()
        {
            return Init(null, RegisterBehavior.MessageVisualizer | RegisterBehavior.Navigation);
        }

        /// <summary>
        /// Register the known services with the ServiceLocator type.
        /// </summary>
        /// <param name="defaultLocator">Service locator</param>
        /// <returns>IDependencyService</returns>
        public static IDependencyService Init(IDependencyService defaultLocator)
        {
            return Init(defaultLocator, 
                RegisterBehavior.MessageVisualizer | RegisterBehavior.Navigation);
        }

        /// <summary>
        /// Register the known services with the ServiceLocator type.
        /// </summary>
        /// <param name="registerBehavior">Services to register</param>
        /// <returns>IDependencyService</returns>
        public static IDependencyService Init(RegisterBehavior registerBehavior)
        {
            return Init(null, registerBehavior);
        }

        /// <summary>
        /// Registers the known services with the ServiceLocator type.
        /// </summary>
        /// <param name="defaultLocator">ServiceLocator, if null, DependencyService is used.</param>
        /// <param name="registerBehavior">Registration behavior</param>
        /// <returns>IDependencyService</returns>
        public static IDependencyService Init(IDependencyService defaultLocator, RegisterBehavior registerBehavior)
        {
            // If the ServiceLocator has already been set, then something used it before
            // Init was called. This is not allowed if they are going to change the locator.
            if (defaultLocator != null 
                && serviceLocator != null)
            {
                throw new InvalidOperationException(
                   $"Must call {nameof(XamUInfrastructure.Init)} before using any library features; " +
                   "ServiceLocator has already been set.");
            }

            // Can call Init multiple times as long as you don't change the locator.
            if (initialized)
            {
                Debug.Assert(serviceLocator != null);
                return ServiceLocator;
            }

            // Only do the remaining logic once or we get duplicate key exceptions.
            initialized = true;

            // Assign the locator; either use the supplied one, or the default
            // DependencyService version if not supplied.
            if (defaultLocator == null)
            {
                defaultLocator = ServiceLocator;
            }
            else
            {
                Debug.Assert (serviceLocator == null);
                serviceLocator = defaultLocator;
            }

            // Register the services
            if ((registerBehavior & RegisterBehavior.MessageVisualizer) == RegisterBehavior.MessageVisualizer)
            {
                try
                {
                    defaultLocator.Register<IMessageVisualizerService, FormsMessageVisualizerService>();
                }
                catch (ArgumentException)
                {
                }
            }

            if ((registerBehavior & RegisterBehavior.Navigation) == RegisterBehavior.Navigation)
            {
                // Use a single instance for the navigation service and
                // register both interfaces against it.
                var navService = new FormsNavigationPageService();

                try
                {
                    defaultLocator.Register<INavigationPageService>(navService);
                    defaultLocator.Register<INavigationService>(navService);
                }
                catch (ArgumentException)
                {
                }
            }

            try
            {
                defaultLocator.Register<IDependencyService>(defaultLocator);
            }
            catch (ArgumentException)
            {
            }

            return defaultLocator;
        }
    }
}

