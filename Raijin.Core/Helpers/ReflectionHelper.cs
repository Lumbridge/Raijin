using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Raijin.Core.CompositePattern;

namespace Raijin.Core.Helpers
{
    public static class ReflectionHelper
    {
        public static string GetObjectFlatFile(object obj, int indent = 0)
        {
            var result = "";

            if (obj == null) return result;

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
                            result += GetObjectFlatFile(val, indent);
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
                            result += $"{indentString}{property.Name}: (ObjectType={propValue})\n";
                        }
                        result += GetObjectFlatFile(propValue, indent + 2);
                    }
                    // list property with child objects/properties
                    else if (IsList(propValue))
                    {
                        if (propValue != null && !string.IsNullOrWhiteSpace(propValue.ToString()))
                        {
                            var subCount = ((IList)propValue).Count;
                            if (subCount > 0)
                            {
                                result += $"{indentString}{property.Name} (List, Count={subCount})\n";
                            }
                        }
                        result += GetObjectFlatFile(propValue, indent + 2);
                    }
                    // bottom level property with value
                    else
                    {
                        if (propValue != null && !string.IsNullOrWhiteSpace(propValue.ToString())
                                              && !propValue.ToString().Contains("d__")
                                              && !propValue.ToString().Contains("WhereSelectList")
                                              && !propValue.ToString().Contains("Dictionary"))
                        {
                            result += $"{indentString}{property.Name}: {propValue}\n";
                        }
                    }
                }
            }

            return result;
        }

        public static Composite GetObjectComposite(object obj, Composite result, Dictionary<string, string> mappedTermsDictionary = null, List<string> ignoredPropertiesList = null)
        {
            if (obj == null) return result;
            if (result == null) return result;
            if (mappedTermsDictionary == null) mappedTermsDictionary = new Dictionary<string, string>();
            if (ignoredPropertiesList == null) ignoredPropertiesList = new List<string>();

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
                            result = GetObjectComposite(val, result, mappedTermsDictionary, ignoredPropertiesList);
                        }
                    }
                }
            }
            else
            {
                foreach (var property in properties)
                {
                    var propValue = property.GetValue(obj, null);

                    if (ignoredPropertiesList.Contains(property.Name))
                        continue;
                    if (propValue != null && ignoredPropertiesList.Contains(propValue.ToString()))
                        continue;

                    // object property with child properties
                    if (property.PropertyType.Assembly == objType.Assembly && !property.PropertyType.IsEnum)
                    {
                        if (propValue != null && !string.IsNullOrWhiteSpace(propValue.ToString()))
                        {
                            var branch = new Composite(mappedTermsDictionary.TryGetValue(property.Name, out var term) ? term : property.Name);
                            result.Add(GetObjectComposite(propValue, branch, mappedTermsDictionary, ignoredPropertiesList));
                        }
                    }
                    // list property with child objects/properties
                    else if (IsList(propValue))
                    {
                        if (propValue != null && !string.IsNullOrWhiteSpace(propValue.ToString()))
                        {
                            var listChildCount = ((IList)propValue).Count;
                            if (listChildCount > 0)
                            {
                                string valueToFind;
                                valueToFind = mappedTermsDictionary.TryGetValue(property.Name, out valueToFind)
                                ? valueToFind
                                : property.Name;
                                var iterationCount = result.FindMany(valueToFind, new List<Component>()).Count;
                                iterationCount = iterationCount == 0 ? 0 : iterationCount - 1;
                                var branch = new Composite(valueToFind + $":{iterationCount}");
                                result.Add(GetObjectComposite(propValue, branch, mappedTermsDictionary, ignoredPropertiesList));
                            }
                        }
                    }
                    // bottom level property with value
                    else
                    {
                        if (propValue != null && !string.IsNullOrWhiteSpace(propValue.ToString())
                                              && !propValue.ToString().Contains("d__")
                                              && !propValue.ToString().Contains("WhereSelectList")
                                              && !propValue.ToString().Contains("Dictionary"))
                        {
                            var branch = new Composite(mappedTermsDictionary.TryGetValue(property.Name, out var term) ? term : property.Name);
                            var leaf = new Leaf(mappedTermsDictionary.TryGetValue(propValue.ToString(), out var leafTerm) ? leafTerm : propValue.ToString(), branch);
                            branch.Add(leaf);
                            result.Add(branch);
                        }
                    }
                }
            }

            return result;
        }

        public static IEnumerable<T> GetEnumerableOfType<T>(params object[] constructorArgs) where T : class/*, IComparable<T>*/
        {
            List<T> objects = new List<T>();
            foreach (Type type in
                Assembly.GetAssembly(typeof(T)).GetTypes()
                    .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T))))
            {
                objects.Add((T)Activator.CreateInstance(type, constructorArgs));
            }
            //objects.Sort();
            return objects;
        }

        public static PropertyInfo GetProperty(object t, string propName)
        {
            if (t.GetType().GetProperties().Count(p => p.Name == propName.Split('.')[0]) == 0)
                throw new ArgumentNullException($"Property {propName}, is not exists in object {t}");
            if (propName.Split('.').Length == 1)
                return t.GetType().GetProperty(propName);
            else
                return GetProperty(t.GetType().GetProperty(propName.Split('.')[0])?.GetValue(t, null), propName.Split('.')[1]);
        }

        public static PropertyInfo GetPropertyInfo(this object sourceObject, string propertyName)
        {
            if (sourceObject == null)
                return null;

            PropertyInfo pi = null;
            var obj = sourceObject;

            // Split property name to parts (propertyName could be hierarchical, like obj.subobj.subobj.property
            var propertyNameParts = propertyName.Split('.');

            var lastPropPartName = propertyNameParts.Last();
            if (lastPropPartName.Contains("["))
            {
                lastPropPartName = lastPropPartName.Split('[')[0];
            }

            foreach (var propertyNamePart in propertyNameParts)
            {
                if (obj == null) return null;

                // propertyNamePart could contain reference to specific 
                // element (by index) inside a collection
                if (!propertyNamePart.Contains("["))
                {
                    pi = obj.GetType().GetProperty(propertyNamePart);
                    if (pi == null)
                        return null;
                    obj = pi.GetValue(obj, null);
                }
                else
                {   // propertyNamePart is a reference to specific element 
                    // (by index) inside a collection
                    // like AggregatedCollection[123]
                    // get collection name and element index
                    var indexStart = propertyNamePart.IndexOf("[", StringComparison.Ordinal) + 1;
                    var collectionPropertyName = propertyNamePart.Substring(0, indexStart - 1);
                    var collectionElementIndex = int.Parse(propertyNamePart.Substring(indexStart, propertyNamePart.Length - indexStart - 1));

                    // get collection object
                    pi = obj.GetType().GetProperty(collectionPropertyName);
                    if (pi == null)
                        return null;
                    if (pi.Name == lastPropPartName && !propertyNameParts.Last().Contains("["))
                        return pi;
                    var unknownCollection = pi.GetValue(obj, null);

                    // try to process the collection as array
                    if (unknownCollection.GetType().IsArray)
                    {
                        var collectionAsArray = unknownCollection as object[];
                        obj = collectionAsArray[collectionElementIndex];
                    }
                    else
                    {
                        //   try to process the collection as IList
                        if (unknownCollection is IList collectionAsList)
                        {
                            if (collectionAsList.Count > 0)
                            {
                                obj = collectionAsList[collectionElementIndex];
                            }
                        }
                    }
                }
            }

            return pi;
        }

        public static object GetPropertyValue(this object sourceObject, string propertyName)
        {
            if (sourceObject == null)
                return null;

            var obj = sourceObject;

            // Split property name to parts (propertyName could be hierarchical, like obj.subobj.subobj.property
            var propertyNameParts = propertyName.Split('.');

            foreach (var propertyNamePart in propertyNameParts)
            {
                if (obj == null) return null;

                // propertyNamePart could contain reference to specific 
                // element (by index) inside a collection
                if (!propertyNamePart.Contains("["))
                {
                    var pi = obj.GetType().GetProperty(propertyNamePart);
                    if (pi == null) return null;
                    obj = pi.GetValue(obj, null);
                }
                else
                {   // propertyNamePart is a reference to specific element 
                    // (by index) inside a collection
                    // like AggregatedCollection[123]
                    // get collection name and element index
                    var indexStart = propertyNamePart.IndexOf("[", StringComparison.Ordinal) + 1;
                    var collectionPropertyName = propertyNamePart.Substring(0, indexStart - 1);
                    var collectionElementIndex = int.Parse(propertyNamePart.Substring(indexStart, propertyNamePart.Length - indexStart - 1));

                    // get collection object
                    var pi = obj.GetType().GetProperty(collectionPropertyName);
                    if (pi == null)
                        return null;
                    var unknownCollection = pi.GetValue(obj, null);

                    // try to process the collection as array
                    if (unknownCollection.GetType().IsArray)
                    {
                        var collectionAsArray = unknownCollection as object[];
                        obj = collectionAsArray[collectionElementIndex];
                    }
                    else
                    {
                        //   try to process the collection as IList
                        if (unknownCollection is IList collectionAsList)
                        {
                            if (collectionAsList.Count > 0 && collectionAsList.Count > collectionElementIndex)
                            {
                                obj = collectionAsList[collectionElementIndex];
                            }
                        }
                        else
                        {
                            // unknown collection type
                        }
                    }
                }
            }

            return obj;
        }

        public static bool SetValue(object inputObject, string propertyName, object propertyVal)
        {
            //get the property information based on the type
            var propertyInfo = inputObject.GetPropertyInfo(propertyName); //type.GetProperty(propertyName);

            if (propertyInfo == null)
                return false;

            //ProcessMappings.ChangeType does not handle conversion to nullable types
            //if the property type is nullable, we need to get the underlying type of the property
            var targetType = IsNullableType(propertyInfo.PropertyType) ? Nullable.GetUnderlyingType(propertyInfo.PropertyType) : propertyInfo.PropertyType;

            //Returns an System.Object with the specified System.Type and whose value is
            //equivalent to the specified object.
            try
            {
                propertyVal = Convert.ChangeType(propertyVal, targetType);
            }
            catch
            {
                return false;
            }

            if (propertyVal == null)
                return false;

            //Set the value of the property
            propertyInfo.SetValue(inputObject, propertyVal, null);

            return true;
        }

        public static Type GetNullableType(Type type)
        {
            // Use Nullable.GetUnderlyingType() to remove the Nullable<T> wrapper if type is already nullable.
            type = Nullable.GetUnderlyingType(type) ?? type; // avoid type becoming null
            if (type.IsValueType)
                return typeof(Nullable<>).MakeGenericType(type);
            else
                return type;
        }

        public static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool IsList(object value)
        {
            if (value == null) return false;
            return value is IList || IsGenericList(value);
        }

        public static bool IsGenericList(object value)
        {
            var type = value.GetType();
            return type.IsGenericType
                   && typeof(List<>) == type.GetGenericTypeDefinition();
        }
    }
}
