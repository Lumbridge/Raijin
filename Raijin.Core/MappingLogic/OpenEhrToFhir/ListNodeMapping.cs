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
    public class ListNodeMapping : BaseMapping
    {
        public ListNodeMapping(string openEhrFieldPath, string fhirFieldPath, string regexPattern = "")
            : base(openEhrFieldPath, fhirFieldPath, regexPattern)
        {
            MappingType = MappingType.List;
        }

        public override OpenEhrToFhirAdapterBase ProcessMappings(OpenEhrToFhirAdapterBase adapter)
        {
            foreach (var mapping in adapter.Mappings.Where(x => x.MappingType == MappingType.List))
            {
                var listNodes = adapter.OpenEhrRecord.FindMany(mapping, new List<Component>());

                foreach (var node in listNodes)
                {
                    var netPath = node.NetPath(adapter.Mappings);
                    var innerNetPath = node.InnerNetPath(adapter.Mappings);

                    var pi = adapter.GetPropertyInfo(netPath);

                    var componentType = pi.PropertyType.GetGenericArguments()[0];
                    var constructedListType = typeof(List<>).MakeGenericType(componentType);
                    var listInstance = (IList)Activator.CreateInstance(constructedListType);

                    var subNodes = ((Composite)node.Parent).FindMany(mapping, new List<Component>()).ToList();

                    var counter = 0;
                    while (counter < subNodes.Count)
                    {
                        var subNode = subNodes[counter];

                        if (Regex.Match(subNode.Name, ":\\d+\\|+").Success)
                        {
                            var regex = $"{subNode.Name.Split(':')[0]}:{subNode.ListIndex}+\\|+";

                            var siblings = subNodes.Where(x =>
                                Regex.Match(x.Name, regex).Success
                                && x.ParentResourceIndex(adapter.Mappings) == subNode.ParentResourceIndex(adapter.Mappings)).ToList();

                            var passes = siblings.DistinctBy(x => x.Parent.NetPath(adapter.Mappings)).Count();

                            for (var i = 0; i < passes; i++)
                            {
                                var system = (Composite)siblings.Where(x => x.Name.Contains("terminology")).ToList()[i];
                                var code = (Composite)siblings.Where(x => x.Name.Contains("code")).ToList()[i];
                                var text = (Composite)siblings.Where(x => x.Name.Contains("value")).ToList()[i];

                                if (system != null) counter++;
                                if (code != null) counter++;
                                if (text != null) counter++;

                                listInstance.Add(Activator.CreateInstance(componentType, null));
                            }
                        }
                        else
                        {
                            counter++;
                            listInstance.Add(Activator.CreateInstance(componentType, null));
                        }
                    }

                    var obj = adapter.GetPropertyValue(netPath);
                    if (!ReflectionHelper.SetValue(obj, innerNetPath, listInstance))
                    {
                        var innerPi = adapter.GetPropertyInfo(netPath);
                        obj = adapter.GetPropertyValue(netPath.Remove(netPath.LastIndexOf('.')));
                        try
                        {
                            innerPi.SetValue(obj, listInstance);
                        }
                        catch
                        {
                            //Log.Error($"Failed to map list {mapping.OpenEhrFieldPath} to {mapping.FhirFieldPath}.");
                        }
                    }
                }
            }

            return adapter;
        }
    }
}
