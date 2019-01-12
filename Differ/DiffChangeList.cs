﻿using System.Collections.Generic;
using System.Linq;

namespace Roblox.Reflection
{
    public class DiffChangeList : List<object>
    {
        public string Name { get; private set; }

        public DiffChangeList(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            string[] elements = this.Select(elem => elem.ToString()).ToArray();
            return string.Join(" ", elements);
        }

        public void WriteHtml(ReflectionDumper buffer, bool multiline = false)
        {
            int numTabs;

            if (multiline)
            {
                buffer.OpenClassTag(Name, 1, "div");
                buffer.NextLine();

                buffer.OpenClassTag("ChangeList", 2);
                numTabs = 3;
            }
            else
            {
                buffer.OpenClassTag(Name, 1);
                numTabs = 2;
            }

            buffer.NextLine();

            foreach (object change in this)
            {
                if (change is Parameters)
                {
                    var parameters = change as Parameters;
                    parameters.WriteHtml(buffer, numTabs, true);
                }
                else if (change is LuaType)
                {
                    var type = change as LuaType;
                    type.WriteHtml(buffer, numTabs);
                }
                else
                {
                    string value;

                    if (change is Security)
                    {
                        var security = change as Security;
                        value = security.Describe(true);
                    }
                    else
                    {
                        value = change.ToString();
                    }

                    string tagClass;

                    if (value.StartsWith("{") && value.EndsWith("}"))
                        tagClass = "Security";
                    else if (value.StartsWith("[") && value.EndsWith("]"))
                        tagClass = "Serialization";
                    else if (value.StartsWith("\"") && value.EndsWith("\""))
                        tagClass = "String";
                    else
                        tagClass = change.GetType().Name;

                    buffer.OpenClassTag(tagClass, numTabs);
                    buffer.Write(value);
                    buffer.CloseClassTag();
                }
            }

            buffer.CloseClassTag(numTabs - 1);

            if (multiline)
            {
                buffer.CloseClassTag(1, "div");
            }
        }
    }
}