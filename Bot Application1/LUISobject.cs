using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OutlookBot
{

    public class LUISobject
    {
        public string luis_schema_version { get; set; }
        public string versionId { get; set; }
        public string name { get; set; }
        public string desc { get; set; }
        public string culture { get; set; }
        public Intent[] intents { get; set; }
        public Entity[] entities { get; set; }
        public Composite[] composites { get; set; }
        public object[] closedLists { get; set; }
        public string[] bing_entities { get; set; }
        public Action[] actions { get; set; }
        public Model_Features[] model_features { get; set; }
        public object[] regex_features { get; set; }
        public Utterance[] utterances { get; set; }
        public Dialog dialog { get; set; }
    }

    public class Intent
    {
        public string intent { get; set; }
        public string score { get; set; }
    }

    public class Entity
    {
        public string entity { get; set; }
        public string type { get; set; }
    }

    public class Dialog
    {
        public string prompt { get; set; }
        public string parameterName { get; set; }
        public string parameterType { get; set; }
    }

    public class Composite
    {
        public string name { get; set; }
        public string[] children { get; set; }
    }

    public class Action
    {
        public string actionName { get; set; }
        public string intentName { get; set; }
        public Channel channel { get; set; }
        public Actionparameter[] actionParameters { get; set; }
    }

    public class Channel
    {
        public string Name { get; set; }
        public string Method { get; set; }
        public Setting[] Settings { get; set; }
    }

    public class Setting
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class Actionparameter
    {
        public string parameterName { get; set; }
        public string entityName { get; set; }
        public bool required { get; set; }
        public string question { get; set; }
        public string phraseListFeatureName { get; set; }
    }

    public class Model_Features
    {
        public string name { get; set; }
        public bool mode { get; set; }
        public string words { get; set; }
        public bool activated { get; set; }
    }

    public class Utterance
    {
        public string text { get; set; }
        public string intent { get; set; }
        public Entity1[] entities { get; set; }
    }

    public class Entity1
    {
        public string entity { get; set; }
        public int startPos { get; set; }
        public int endPos { get; set; }
    }

}