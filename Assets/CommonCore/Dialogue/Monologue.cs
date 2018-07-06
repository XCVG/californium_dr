﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using CommonCore.State;

namespace CommonCore.Dialogue
{
    public class Monologue
    {
        private List<MonologueNode> Nodes {get; set;}

        internal Monologue()
        {
            Nodes = new List<MonologueNode>();
        }

        public Monologue(string sourceName) : this()
        {
            Nodes = MonologueParser.LoadMonologue(sourceName);
        }

        public string GetLineRandom()
        {
            List<MonologueNode> passedNodes = new List<MonologueNode>(Nodes.Count);
            foreach(MonologueNode mn in Nodes)
            {
                if (mn.CheckConditions())
                    passedNodes.Add(mn);
            }
            if (passedNodes.Count == 0)
                return string.Empty;

            int selected = UnityEngine.Random.Range(0, passedNodes.Count);
            MonologueNode selectedNode = passedNodes[selected];
            selectedNode.ExecuteMicroscripts();
            return selectedNode.Line;
        }
    }

    internal class MonologueNode
    {
        public readonly string Line;
        public readonly string Audio;
        public readonly Conditional[] Conditions;
        public readonly MicroscriptNode[] Microscripts;

        public MonologueNode(string line, string audio, Conditional[] conditions, MicroscriptNode[] microscripts)
        {
            Line = line;
            Audio = audio;
            Conditions = conditions;
            Microscripts = microscripts;
        }

        public bool CheckConditions()
        {
            foreach (Conditional c in Conditions)
            {
                if (!c.Evaluate())
                    return false;
            }
            return true;
        }

        public void ExecuteMicroscripts()
        {
            foreach(MicroscriptNode m in Microscripts)
            {
                m.Execute();
            }
        }
    }

    internal static class MonologueParser
    {
        public static List<MonologueNode> LoadMonologue(string monologueName)
        {
            TextAsset ta = CCBaseUtil.LoadResource<TextAsset>("Monologue/" + monologueName);
            JObject jo = JObject.Parse(ta.text);

            List<MonologueNode> nodes = new List<MonologueNode>();

            //get all monologue entries
            JArray ja = (JArray)jo["monologue"];
            foreach(var jt in ja) //ja das ist gut
            {
                try
                {
                    nodes.Add(ParseNode(jt));
                }
                catch(Exception e)
                {
                    Debug.LogWarning("Failed to parse monologue node!");
                    Debug.LogException(e);
                }
            }

            return nodes;
        }

        public static MonologueNode ParseNode(JToken jt)
        {
            string line = string.Empty;
            string audio = null;

            if (jt["line"] != null)
                line = jt["line"].Value<string>();

            if (jt["audio"] != null)
                audio = jt["audio"].Value<string>();

            List<Conditional> cList = new List<Conditional>();
            if (jt["conditions"] != null)
            {
                JArray conditionJArray = (JArray)jt["conditions"];
                foreach(JToken conditionJT in conditionJArray)
                {
                    try
                    {
                        cList.Add(DialogueParser.ParseSingleCondition(conditionJT));
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }

            List<MicroscriptNode> mList = new List<MicroscriptNode>();
            if(jt["microscripts"] != null)
            {
                JArray microscriptJArray = (JArray)jt["microscripts"];
                foreach(JToken microscriptJT in microscriptJArray)
                {
                    try
                    {
                        mList.Add(DialogueParser.ParseMicroscript(microscriptJT));
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }

            return new MonologueNode(line, audio, cList.ToArray(), mList.ToArray());
        }

    }

}