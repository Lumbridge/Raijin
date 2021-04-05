using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Raijin.Core.Classes;
using Raijin.Core.MappingLogic.OpenEhrToFhir;

namespace Raijin.Core.Adapters.OpenEhrToFhir
{
    public class NWISAdverseReactionList : OpenEhrToFhirAdapterBase
    {
        public NWISAdverseReactionList(string openEhrFlatFile)
        {
            // ========
            // METADATA
            // ========
            OpenEhrRecord = OpenEhrParser.Parse(GetType().Name, openEhrFlatFile);

            // ========================
            // OpenEHR -> FHIR Mappings
            // ========================

            // =========
            // Resources
            // =========
            Mappings.Add(new ResourceMapping("adverse_reaction_risk", ResourceType.AllergyIntolerance));

            // =====
            // Lists
            // =====
            Mappings.Add(new ListNodeMapping("reaction_event", "Reaction", "reaction_event:\\d"));
            Mappings.Add(new ListNodeMapping("manifestation", "Manifestation", "manifestation:\\d"));

            // ==========
            // Attributes
            // ==========
            Mappings.Add(new AttributeMapping("manifestation", "Manifestation", "manifestation:\\d+\\|+"));
            Mappings.Add(new AttributeMapping("manifestation", "Manifestation", "manifestation:\\d+"));
            Mappings.Add(new AttributeMapping("specific_substance", "Substance", "specific_substance\\|+"));
            Mappings.Add(new AttributeMapping("substance", "Reaction[].Substance", "substance\\|+"));
            Mappings.Add(new AttributeMapping("route_of_exposure", "ExposureRoute"));
            Mappings.Add(new AttributeMapping("onset_of_last_reaction", "LastOccurrence"));
            Mappings.Add(new AttributeMapping("onset_of_reaction", "LastOccurrence"));
            Mappings.Add(new AttributeMapping("reaction_comment", "Description"));

            // ====================
            // Conditional Mappings
            // ====================
            Mappings.Add(new ConditionalMapping("criticality", "Criticality",
                new List<Tuple<string, string, object>>
                {
                    new Tuple<string, string, object>("code", "at0102", AllergyIntolerance.AllergyIntoleranceCriticality.Low),
                    new Tuple<string, string, object>("code", "at0103", AllergyIntolerance.AllergyIntoleranceCriticality.High),
                    new Tuple<string, string, object>("code", "at0124", AllergyIntolerance.AllergyIntoleranceCriticality.UnableToAssess)
                }, "criticality\\|+"));

            Mappings.Add(new ConditionalMapping("reaction_mechanism", "Type",
                new List<Tuple<string, string, object>>
                {
                    new Tuple<string, string, object>("code", "at0059", AllergyIntolerance.AllergyIntoleranceType.Allergy),
                    new Tuple<string, string, object>("code", "at0060", AllergyIntolerance.AllergyIntoleranceType.Intolerance)
                }, "reaction_mechanism\\|+"));

            Mappings.Add(new ConditionalMapping("category", "CategoryElement",
                new List<Tuple<string, string, object>>
                {
                    new Tuple<string, string, object>("value", "biologic", AllergyIntolerance.AllergyIntoleranceCategory.Biologic),
                    new Tuple<string, string, object>("value", "medication", AllergyIntolerance.AllergyIntoleranceCategory.Medication),
                    new Tuple<string, string, object>("value", "other", AllergyIntolerance.AllergyIntoleranceCategory.Environment),
                    new Tuple<string, string, object>("value", "food", AllergyIntolerance.AllergyIntoleranceCategory.Food)
                }, "category\\|+"));

            // ==================
            // Extension Mappings
            // ==================
            Mappings.Add(new ExtensionMapping("encoding", "encoding\\|+"));
            Mappings.Add(new ExtensionMapping("last_updated"));
            Mappings.Add(new ExtensionMapping("language", "language\\|+"));
            Mappings.Add(new ExtensionMapping("witnessed_by_clinician", fhirFieldPath: "Reaction[]"));

            // ==================================================================
            // Process all mappings to convert the OpenEHR model to FHIR Resource
            // ==================================================================
            ExecuteProcessMappings();
        }
    }
}
