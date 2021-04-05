using Hl7.Fhir.Model;
using Raijin.Core.Adapters.OpenEhrToFhir;
using Raijin.Core.CompositePattern;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Raijin.Core.MappingLogic.OpenEhrToFhir
{
    public class ResourceMapping : BaseMapping
    {
        public ResourceMapping(string openEhrFieldPath, ResourceType resourceType) : base(openEhrFieldPath)
        {
            ResourceTargetType = resourceType;
            MappingType = MappingType.Resource;
        }

        // Targeted FHIR Type of result e.g. OEHR Record -> AllergyIntolerance Resource
        public ResourceType ResourceTargetType { get; set; }

        public override OpenEhrToFhirAdapterBase ProcessMappings(OpenEhrToFhirAdapterBase adapter)
        {
            foreach (var mapping in adapter.Mappings.Where(x => x.MappingType == MappingType.Resource))
            {
                // calculate number of resources of this type are required
                var number = adapter.OpenEhrRecord.FindMany(mapping, new List<Component>()).Count;

                for (var i = 0; i < number; i++)
                {
                    var resource = (Resource)Activator.CreateInstance("Hl7.Fhir.STU3.Core", "Hl7.Fhir.Model." + ((ResourceMapping)mapping).ResourceTargetType).Unwrap();
                    adapter.ResourceList.Add(resource);
                    //Log.Info($"Added resource of type {resource.GetType().Name} to  ResourceList[{adapter.ResourceList.Count - 1}].");
                }
            }
            return adapter;
        }
    }
}
