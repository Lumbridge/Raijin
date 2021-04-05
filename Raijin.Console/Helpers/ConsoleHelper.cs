using System;
using System.Collections;
using static Raijin.Core.Helpers.ReflectionHelper;

namespace Raijin.Console.Helpers
{
    public static class ConsoleHelper
    {
        public static void Log(string message, ConsoleColor colour, bool newline = false)
        {
            if (newline)
                System.Console.Write(message + "\n", System.Console.ForegroundColor = colour);
            else
                System.Console.Write(message, System.Console.ForegroundColor = colour);
            System.Console.ResetColor();
        }

        public static void SetConsoleColour(int indentLevel)
        {
            switch (indentLevel)
            {
                case 0:
                    System.Console.ForegroundColor = ConsoleColor.White;
                    break;
                case 2:
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case 4:
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case 6:
                    System.Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case 8:
                    System.Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case 10:
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case 12:
                    System.Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case 14:
                    System.Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
        }

        public static void PrintPropertiesRecursive(object obj, int indent = 0)
        {
            if (obj == null) return;

            var indentString = new string('-', indent);
            var objType = obj.GetType();
            var properties = objType.GetProperties();

            // check if this object is a list
            if (IsList(obj))
            {
                var t = obj.GetType();
                if (t.GetProperty("Item") != null)
                {
                    var p = t.GetProperty("Item");
                    var count = -1;
                    if (t.GetProperty("Count") != null && t.GetProperty("Count").PropertyType == typeof(int))
                    {
                        count = (int)t.GetProperty("Count").GetValue(obj, null);
                    }
                    if (count > 0)
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var val = p.GetValue(obj, new object[] { i });
                            PrintPropertiesRecursive(val, indent);
                        }
                    }
                }
            }
            else
            {
                foreach (var property in properties)
                {
                    var propValue = property.GetValue(obj, null);

                    // object property with child properties
                    if (property.PropertyType.Assembly == objType.Assembly && !property.PropertyType.IsEnum)
                    {
                        if (propValue != null && !string.IsNullOrWhiteSpace(propValue.ToString()))
                        {
                            ConsoleHelper.SetConsoleColour(indent);
                            System.Console.WriteLine("{0}{1}:", indentString, property.Name + $"(Level {indent})");
                        }
                        PrintPropertiesRecursive(propValue, indent + 2);
                    }
                    // list property with child objects/properties
                    else if (IsList(propValue))
                    {
                        if (propValue != null && !string.IsNullOrWhiteSpace(propValue.ToString()))
                        {
                            var subCount = ((IList)propValue).Count;
                            if (subCount > 0)
                            {
                                ConsoleHelper.SetConsoleColour(indent);
                                System.Console.WriteLine("{0}{1}:", indentString, property.Name + $"(Level {indent})" + $" (List, Count: {((IList)propValue).Count})");
                            }
                        }
                        PrintPropertiesRecursive(propValue, indent + 2);
                    }
                    // bottom level property with value
                    else
                    {
                        if (propValue != null && !string.IsNullOrWhiteSpace(propValue.ToString())
                                              && !propValue.ToString().Contains("d__")
                                              && !propValue.ToString().Contains("WhereSelectList")
                                              && !propValue.ToString().Contains("Dictionary"))
                        {
                            ConsoleHelper.SetConsoleColour(indent);
                            System.Console.WriteLine("{0}{1}: {2}", indentString, property.Name + $"(Level {indent})", propValue);
                        }
                    }
                }
            }
        }
    }
}
