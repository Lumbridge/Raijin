using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raijin.Core.Adapters.OpenEhrToFhir;
using Raijin.Core.CompositePattern;

namespace Raijin.Core.MappingLogic.OpenEhrToFhir
{
    public class ExtensionMapping : BaseMapping
    {
        public ExtensionMapping(string openEhrFieldPath, string regexPattern = "", string fhirFieldPath = "", string extensionFhirValueType = "", string extensionFhirStructureDefinitionUrlEnder = "")
            : base(openEhrFieldPath, fhirFieldPath, regexPattern)
        {
            MappingType = MappingType.Extension;
            if (string.IsNullOrEmpty(extensionFhirStructureDefinitionUrlEnder)) extensionFhirStructureDefinitionUrlEnder = openEhrFieldPath;
            extensionFhirStructureDefinitionUrlEnder = extensionFhirStructureDefinitionUrlEnder.Replace("|", "");
            extensionFhirStructureDefinitionUrlEnder = extensionFhirStructureDefinitionUrlEnder.Replace(":", "");
            ExtensionFhirStructureDefinitionUrl = $"https://nwis.structure-definitions.nhs.uk/StructureDefinition/{extensionFhirStructureDefinitionUrlEnder}";
            ExtensionFhirValueType = extensionFhirValueType;
        }

        public string ExtensionFhirValueType { get; set; }
        public string ExtensionFhirStructureDefinitionUrl { get; set; }

        public override OpenEhrToFhirAdapterBase ProcessMappings(OpenEhrToFhirAdapterBase adapter)
        {
            foreach (var mapping in adapter.Mappings.Where(x => x.MappingType == MappingType.Extension))
            {
                var nodes = adapter.OpenEhrRecord.FindMany(mapping, new List<Component>());

                // 2.2 Work out destination of this mapping item
                foreach (var node in nodes)
                {
                    // check if the current node has been processed already
                    if (adapter.ProcessedNodes.Contains(node))
                        continue;

                    // get the resource that this node should be mapped to
                    var rootResourceIndex = node.ParentResourceIndex(adapter.Mappings);

                    // if this node has no parent resource index then we skip it
                    if (rootResourceIndex == int.MinValue)
                        continue;

                    // Get parent object of attribute
                    var resourceRoot = adapter.ResourceList[rootResourceIndex];

                    // gets the reference to a fhir field and also returns the index of the field if the field is a list item
                    // if val is not null here then it means the field has been instantiated by a list node mapping operation and must be part of a list
                    // if val is null then it means the field has not been instantiated previously.
                    var oldValue = GetFhirObject(resourceRoot, adapter, node, out _);

                    // constructs the value to place at the target field location
                    var newValue = ConstructNewTargetValue(adapter, node, mapping, oldValue);

                    // updates the target field in the fhir resource and returns the updated adapter
                    adapter = SetFhirValue(resourceRoot, adapter, node, newValue, oldValue);
                }
            }

            return adapter;
        }
    }
}
