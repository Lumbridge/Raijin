using Raijin.Core.Adapters.OpenEhrToFhir;
using Raijin.Core.CompositePattern;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Raijin.Core.MappingLogic.OpenEhrToFhir
{
    public class ConditionalMapping : BaseMapping
    {
        //private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ConditionalMapping(string openEhrFieldPath, string fhirFieldPath, List<Tuple<string, string, object>> conditions, string regexPattern = "")
            : base(openEhrFieldPath, fhirFieldPath, regexPattern)
        {
            Conditions = conditions;
            MappingType = MappingType.Conditional;
        }

        // List of conditions
        // string 1 = DX_TEXT/CodeableConcept field name to check
        // string 2 = Value of that field for this condition to take effect
        // object to assign to the FHIR object if the condition is met
        // Example using the following criticality lines:
        // criticality|code": "at0103",
        // criticality|value": "High",
        // criticality|terminology": "local"
        // The FHIR mapping would be:
        // at0102::Low ⇒ low
        // at0103::High ⇒ high
        // at0124::Indeterminate ⇒ unable-to-assess
        // So the mapping object would be
        // new ConditionalMapping("criticality|", "criticality",
        // new List<Tuple<string, string, object>>
        // {
        //     new Tuple<string, string, object>("code", "at0102", AllergyIntolerance.AllergyIntoleranceCriticality.Low),
        //     new Tuple<string, string, object>("code", "at0103", AllergyIntolerance.AllergyIntoleranceCriticality.High),
        //     new Tuple<string, string, object>("code", "at0124", AllergyIntolerance.AllergyIntoleranceCriticality.UnableToAssess)
        // }));
        public List<Tuple<string, string, object>> Conditions { get; set; }

        public override OpenEhrToFhirAdapterBase ProcessMappings(OpenEhrToFhirAdapterBase adapter)
        {
            foreach (var mapping in adapter.Mappings.Where(x => x.MappingType == MappingType.Conditional))
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

                    var oldValue = GetFhirObject(resourceRoot, adapter, node, out var index);

                    // constructs the value to place at the target field location
                    var newValue = ConstructNewTargetValue(adapter, node, mapping, oldValue);

                    adapter = SetFhirValue(resourceRoot, adapter, node, newValue, oldValue, index);
                }
            }

            return adapter;
        }
    }
}
