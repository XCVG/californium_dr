﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonCore.Console;
using SickDev.CommandSystem;

/// <summary>
/// Console interface that uses the third-party SickDev console system
/// </summary>
public class SickDevConsoleImplementation : IConsole, IDisposable
{
    private GameObject ConsoleObject;

    public SickDevConsoleImplementation()
    {
        GameObject ConsolePrefab = Resources.Load<GameObject>("DevConsole");
        ConsoleObject = GameObject.Instantiate(ConsolePrefab);
    }

    public void Dispose()
    {
        if (ConsoleObject != null)
            UnityEngine.Object.Destroy(ConsoleObject);
    }

    public void AddCommand(Delegate command, bool useClassName, string alias, string className, string description)
    {
        Command sdCommand = new Command(command);

        sdCommand.useClassName = useClassName;

        if (!string.IsNullOrEmpty(alias))
            sdCommand.alias = alias;
        if (!string.IsNullOrEmpty(className))
        {
            sdCommand.className = className;
            sdCommand.useClassName = true;
        }
        if (!string.IsNullOrEmpty(description))
            sdCommand.description = description;


        DevConsole.singleton.AddCommand(sdCommand);
    }    

    public void WriteLine(string line)
    {
        DevConsole.singleton.Log(line);
    }
}