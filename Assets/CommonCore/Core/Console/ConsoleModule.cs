﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonCore.Messaging;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CommonCore.Console
{

    /// <summary>
    /// CommonCore Console Integration Module
    /// Provides integration of third-party console components
    /// </summary>
    [CCExplicitModule]
    public class ConsoleModule : CCModule
    {
        //TODO change this over to provide actual abstraction

        private static ConsoleModule Instance;

        private IConsole Console;
        private ConsoleMessagingIntegrationComponent ConsoleMessagingThing;

        /// <summary>
        /// Initialize the Console module
        /// </summary>
        public ConsoleModule()
        {
            Instance = this;

            CreateConsole();

            //any hooking into the console could be done here
            AddCommands();
            ConsoleMessagingThing = new ConsoleMessagingIntegrationComponent();

            Debug.Log("Console module loaded!");
        }

        /// <summary>
        /// Find a console implemntation and instantiate it
        /// </summary>
        private void CreateConsole()
        {
            Type[] possibleConsoles = CCBase.BaseGameTypes
                .Where((type) => type.GetInterfaces().Contains(typeof(IConsole)))
                .ToArray();

            Debug.Log(possibleConsoles.ToNiceString());

            //get our preferred console implmentation...
            Type preferredConsole = Array.Find(possibleConsoles, (t) => t.Name == CoreParams.PreferredCommandConsole);

            //...fall back to whatever we can find
            if (preferredConsole == null)
            {
                if (possibleConsoles.Length > 0)
                    preferredConsole = possibleConsoles[0];
            }

            if (preferredConsole != null)
            {
                Console = (IConsole)Activator.CreateInstance(preferredConsole);

                Debug.Log($"Using {preferredConsole.Name} console implementation");
            }                      

        }

        /// <summary>
        /// Clean up the console module
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            ConsoleMessagingThing = null;
            (Console as IDisposable)?.Dispose();
            Debug.Log("Console module unloaded!");
        }

        /// <summary>
        /// Register a command with the command parser
        /// </summary>
        /// <param name="command">The method the command will execute</param>
        /// <param name="useClassName">Whether to prepend the class name or not</param>
        /// <param name="alias">Override for command name (optional)</param>
        /// <param name="className">Override for the class name (optional)</param>
        /// <param name="description">Description of the command (optional)</param>
        public static void RegisterCommand(Delegate command, bool useClassName, string alias, string className, string description)
        {
            Instance.Console.AddCommand(command, useClassName, alias, className, description);
        }

        /// <summary>
        /// Write a line of text to the active command console
        /// </summary>
        /// <param name="text">The text to write</param>
        public static void WriteLine(string text)
        {
            Instance.Console.WriteLine(text);
        }

        private void AddCommands()
        {
            //DevConsole.singleton.AddCommand(new ActionCommand(Quit) { useClassName = false });
            //DevConsole.singleton.AddCommand(new FuncCommand<string>(GetVersion) { className = "Core" });

            //this is pretty tightly coupled still but we'll fix that soon enough

            var allCommands = CCBase.BaseGameTypes
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0)
                .ToArray();

            Debug.Log($"Registering {allCommands.Length} console commands!");

            foreach(var command in allCommands)
            {
                //Debug.Log(command.Name);

                try
                {
                    CommandAttribute commandAttr = (CommandAttribute)command.GetCustomAttributes(typeof(CommandAttribute), false)[0];
                    Console.AddCommand(CreateDelegate(command), commandAttr.useClassName, commandAttr.alias, commandAttr.className, commandAttr.description);
                }
                catch(Exception e)
                {
                    Debug.LogError("Failed to add command " + command.Name);
                    Debug.LogException(e);
                }
            }
        }               

        private static Delegate CreateDelegate(MethodInfo methodInfo)
        {
            Func<Type[], Type> getType;
            var isAction = methodInfo.ReturnType.Equals((typeof(void)));
            var types = methodInfo.GetParameters().Select(p => p.ParameterType);

            if (isAction)
            {
                getType = Expression.GetActionType;
            }
            else
            {
                getType = Expression.GetFuncType;
                types = types.Concat(new[] { methodInfo.ReturnType });
            }

            if (methodInfo.IsStatic)
            {
                return Delegate.CreateDelegate(getType(types.ToArray()), methodInfo);
            }

            throw new ArgumentException("Method must be static!", "methodInfo");
        }

        /// <summary>
        /// Provides integration between the console system and messaging system
        /// </summary>
        private class ConsoleMessagingIntegrationComponent : IQdmsMessageReceiver
        {
            public ConsoleMessagingIntegrationComponent()
            {
                QdmsMessageBus.Instance.RegisterReceiver(this);
            }

            ~ConsoleMessagingIntegrationComponent()
            {
                QdmsMessageBus.Instance.UnregisterReceiver(this);
            }

            bool IQdmsMessageReceiver.IsValid //AFAIK this is only used for destroying components
            {
                get
                {
                    return true;
                }

                set
                {
                    //do nothing
                }
            }

            void IQdmsMessageReceiver.ReceiveMessage(QdmsMessage msg)
            {
                if(msg is HUDPushMessage)
                {
                    ConsoleModule.WriteLine(string.Format("{0} : {1}", "*HUD PUSH MESSAGE*", ((HUDPushMessage)msg).Contents));
                }
            }
        }

    }

    /// <summary>
    /// Test commands for the command system
    /// </summary>
    public class TestCommands
    {
        [Command]
        public static void CCTestCommand()
        {
            Debug.Log("hello world");
        }

        [Command]
        public static void CCTestArgCommand(string mystring)
        {
            Debug.Log(mystring);
        }

        [Command]
        private static string GetVersion()
        {
            return string.Format("{0} {1} (Unity {2})", CoreParams.VersionCode, CoreParams.VersionName, CoreParams.UnityVersion);
        }

        [Command]
        private static void Quit()
        {
            Application.Quit();
        }
    }
}