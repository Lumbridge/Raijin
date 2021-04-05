using Hl7.Fhir.Model;
using Raijin.Core.Adapters.OpenEhrToFhir;
using Raijin.Core.CompositePattern;
using Raijin.Core.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Raijin.Core.MappingLogic.OpenEhrToFhir
{
    /// <summary>
    /// List of mapping types which can be selected.
    /// </summary>
    public enum MappingType
    {
        Attribute,
        Conditional,
        Extension,
        List,
        Resource
    }

    /// <summary>
    /// Abstract class containing fields used by inherited mapping types.
    /// </summary>
    public abstract class BaseMapping
    {
        /// <summary>
        /// Log4Net Logging Object.
        /// </summary>
        //public static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// This constructor populates the main aspects of mapping OpenEHR to FHIR.
        /// </summary>
        /// <param name="openEhrFieldPath">The name of the OpenEHR attribute.</param>
        /// <param name="fhirFieldPath">The path of the FHIR field to map the OpenEHR value to.</param>
        /// <param name="regexPattern">An optional regex pattern to search for, will be appended to the openEhrFieldPath.</param>
        protected BaseMapping(string openEhrFieldPath, string fhirFieldPath = "", string regexPattern = "")
        {
            OpenEhrFieldPath = openEhrFieldPath;
            FhirFieldPath = fhirFieldPath;
            RegexPattern = regexPattern;
        }

        /// <summary>
        /// The type of mapping this adapter is.
        /// </summary>
        public MappingType MappingType;

        /// <summary>
        /// The name of the OpenEHR attribute.
        /// </summary>
        public string OpenEhrFieldPath;

        /// <summary>
        /// The path of the FHIR field to map the OpenEHR value to.
        /// </summary>
        public string FhirFieldPath;

        /// <summary>
        /// Regex pattern to use when performing node searches.
        /// </summary>
        public string RegexPattern;

        /// <summary>
        /// Execute the processing of this mapping type.
        /// </summary>
        /// <param name="adapter">The adapter adapter which contains the mappings you want to execute.</param>
        /// <returns>An updated version of the adapter containing the results of the executed mappings.</returns>
        public abstract OpenEhrToFhirAdapterBase ProcessMappings(OpenEhrToFhirAdapterBase adapter);

        /// <summary>
        /// Constructs the object which will be placed at the target FHIR location according the mapping rules and openEHR message values.
        /// </summary>
        /// <param name="adapter">The current adapter being processed.</param>
        /// <param name="node">The current node being processed.</param>
        /// <returns>Returns the constructed value to be placed at the target FHIR location.</returns>
        public object ConstructNewTargetValue(OpenEhrToFhirAdapterBase adapter, Component node, BaseMapping mapping, object oldValue = null)
        {
            object newTargetValue = null;

            // fhir resource destination
            var destination = node.NetPath(adapter.Mappings);

            // list + CodeableConcept combined logic i.e. substance:0|code or substance|code
            if (Regex.Match(node.Name, ":\\d+\\|+").Success ||
                Regex.Match(node.Name, $"{mapping.OpenEhrFieldPath}\\|+").Success ||
                Regex.Match(node.Name, $"{mapping.OpenEhrFieldPath}:\\d+").Success)
            {
                // Get all components of this codeable concept
                var siblingNodes = ((Composite)node.Parent).FindMany(mapping, new List<Component>()).Where(x => x.NetPath(adapter.Mappings) == destination).ToList();

                var system = ((Composite)siblingNodes.FirstOrDefault(x => x.Name.Contains("terminology")))?.Children[0].Name;
                var code = ((Composite)siblingNodes.FirstOrDefault(x => x.Name.Contains("code")))?.Children[0].Name;
                var text = ((Composite)siblingNodes.FirstOrDefault(x => x.Name.Contains("value")))?.Children[0].Name;

                if (system == null && code == null && text == null)
                {
                    newTargetValue = StringHelper.ParseOpenEhrCcString(((Composite)node).Children.First().Name);
                }
                else
                {
                    newTargetValue = new CodeableConcept(system, code, text);
                }

                if (GetType() == typeof(ConditionalMapping))
                {
                    // the type of codeable concept type to search for i.e. Terminology/Code/Value
                    var conditionalSearchType = ((ConditionalMapping)mapping).Conditions.First().Item1;

                    // the actual value to search for in order to match the conditional mapping
                    var conditionalSearchValue = string.Empty;

                    switch (conditionalSearchType)
                    {
                        case "code":
                            {
                                conditionalSearchValue = code;
                                break;
                            }
                        case "terminology":
                            {
                                conditionalSearchValue = system;
                                break;
                            }
                        case "value":
                            {
                                conditionalSearchValue = text;
                                break;
                            }
                    }

                    var metConditions = ((ConditionalMapping)mapping).Conditions.Where(x =>
                           string.Equals(x.Item2, conditionalSearchValue, StringComparison.InvariantCultureIgnoreCase))
                        .ToList();

                    var destinationIsCollection = oldValue is IEnumerable;
                    if (destinationIsCollection)
                    {
                        if (((IList)oldValue).Count == 0)
                        {
                            var listGenericType = oldValue.GetType().GenericTypeArguments[0];
                            var codeGenericType = metConditions[0].Item3.GetType();
                            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listGenericType), null);
                            foreach (var metCondition in metConditions)
                            {
                                var obj = Activator.CreateInstance(typeof(Code<>).MakeGenericType(codeGenericType), metCondition.Item3);
                                list.Add(obj);
                            }

                            newTargetValue = list;
                        }
                    }
                    else
                    {
                        newTargetValue = metConditions[0].Item3;
                    }
                }
                else if (GetType() == typeof(ExtensionMapping))
                {
                    if (oldValue != null)
                    {
                        ((List<Extension>)oldValue).Add(new Extension(
                            ((ExtensionMapping)mapping).ExtensionFhirStructureDefinitionUrl,
                            new CodeableConcept(system, code, text)));
                        newTargetValue = oldValue;
                    }
                }

                // mark these nodes as processed so skip duplicate processing
                adapter.ProcessedNodes.AddRange(siblingNodes);
            }
            else
            {
                var siblingNodes = ((Composite)node.Parent).FindMany(mapping, new List<Component>()).Where(x => x.NetPath(adapter.Mappings) == destination).ToList();

                if (GetType() == typeof(ExtensionMapping))
                {
                    var childLeafValue = ((Composite)node).Children[0] as Composite;
                    ((List<Extension>)oldValue).Add(new Extension(((ExtensionMapping)mapping).ExtensionFhirStructureDefinitionUrl, new FhirString(childLeafValue?.Children[0].Name)));
                    newTargetValue = oldValue;
                }
                else
                {
                    if (siblingNodes.Count == 1)
                    {
                        newTargetValue = StringHelper.ParseOpenEhrCcString(((Composite)siblingNodes[0]).Children[0].Name);
                    }
                    else
                    {
                        var index = node.ParentResourceIndex(adapter.Mappings);
                        var t_node = (Composite)siblingNodes.Where(x => x.NetPath(adapter.Mappings) == destination).ToList()[index];
                        newTargetValue = StringHelper.ParseOpenEhrCcString(t_node.Children[0].Name);
                    }
                }

                adapter.ProcessedNodes.AddRange(siblingNodes);
            }

            return newTargetValue;
        }

        /// <summary>
        /// gets the reference to a fhir field and also returns the index of the field if the field is a list item
        /// if oldValue is not null here then it means the field has been instantiated by a list node mapping operation and must be part of a list
        /// if oldValue is null then it means the field has not been instantiated previously.
        /// </summary>
        /// <param name="resourceRoot">The root resource to be searched i.e. AllergyIntolerance, Patient etc.</param>
        /// <param name="adapter">The adapter currently being used.</param>
        /// <param name="node">The current node being used in the process mapping method.</param>
        /// <param name="index">If the value of the field is inside a list then it's index in the list will be returned.</param>
        /// <returns>Returns the object found at the search location, also returns the index of the field if the field is a list item.</returns>
        public object GetFhirObject(Resource resourceRoot, OpenEhrToFhirAdapterBase adapter, Component node, out int index)
        {
            // get the path inside the resource where this property should be set
            var finalRoute = node.InnerNetPath(adapter.Mappings);

            if (string.IsNullOrEmpty(finalRoute))
                finalRoute = node.NetPath(adapter.Mappings);

            // get the index of the parent list to put the property inside
            var start = finalRoute.LastIndexOf('[') + 1;
            var end = finalRoute.LastIndexOf(']');
            index = int.MinValue;
            if (start != -1 && end != -1)
            {
                var indexStr = finalRoute.Substring(start, end - start);
                if (!int.TryParse(indexStr, out index))
                    throw new Exception("Failed to parse index.");
            }

            if (finalRoute.Split('.').Last().EndsWith("]"))
            {
                finalRoute = finalRoute.Remove(finalRoute.LastIndexOf('['));
            }

            var value = resourceRoot.GetPropertyValue(finalRoute);

            if (value == null)
            {
                // recursively check through all properties for one matching the end node of the mapping
            }

            return value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resourceRoot"></param>
        /// <param name="adapter"></param>
        /// <param name="node"></param>
        /// <param name="newValue"></param>
        /// <param name="oldValue"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public OpenEhrToFhirAdapterBase SetFhirValue(Resource resourceRoot, OpenEhrToFhirAdapterBase adapter, Component node, object newValue, object oldValue = null, int index = int.MinValue)
        {
            var points = 1;

            // setting a not null property which is inside a list
            if (oldValue != null && index != int.MinValue)
            {
                var setListItemMethod = oldValue.GetType().GetMethod("set_Item");

                if (setListItemMethod != null)
                {
                    setListItemMethod.Invoke(oldValue, new[] { index, newValue });

                    adapter.SuccessfulMappingCount += points;

                    //Log.Info($"Mapped {GetType().Name} from {OpenEhrFieldPath} to {node.InnerNetPath(adapter.Mappings)} & yielded +{points} for total of {adapter.SuccessfulMappingCount} successes for this adapter.");
                }
            }
            // setting a null property which is not in a list
            else
            {
                var childPathFromParent = node.InnerNetPath(adapter.Mappings);

                object parentObject;

                if (childPathFromParent.LastIndexOf('.') < 0)
                {
                    parentObject = resourceRoot;
                }
                else
                {
                    var parentObjectPath = childPathFromParent.Remove(childPathFromParent.LastIndexOf('.'));
                    parentObject = resourceRoot.GetPropertyValue(parentObjectPath);
                }

                // get the property name that we want to set
                var childProperty = childPathFromParent.Split('.').Last();

                // set codeable concept in parent object which isn't at the root resource i.e. Resource.Property[0].AnotherProperty[1]
                if (ReflectionHelper.SetValue(parentObject, childProperty, newValue))
                {
                    adapter.SuccessfulMappingCount += points;
                }
                // set primitive data type in parent object which isn't at the root resource i.e. Resource.Property[0].AnotherProperty
                else if (ReflectionHelper.SetValue(parentObject, childProperty, ((Composite)node).Children[0].Name))
                {
                    adapter.SuccessfulMappingCount += points;
                }
                // set primitive data type in parent object which is at the root resource i.e. Resource.Property
                else
                {
                    if (ReflectionHelper.SetValue(resourceRoot, childProperty, ((Composite)node).Children[0].Name))
                    {
                        adapter.SuccessfulMappingCount += points;
                    }
                    else
                    {
                        //Log.Error($"Failed to map attribute with mapping {OpenEhrFieldPath} to {FhirFieldPath}.");
                    }
                }

                //Log.Info($"Mapped {GetType().Name} from {OpenEhrFieldPath} to {childPathFromParent} & yielded +{points} for total of {adapter.SuccessfulMappingCount} successes for this adapter.");
            }

            return adapter;
        }
    }
}
