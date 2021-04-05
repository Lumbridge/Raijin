using Hl7.Fhir.Model;
using Raijin.Core.CompositePattern;
using System.Collections.Generic;
using System.Linq;

namespace Raijin.Core.Adapters.FhirToOpenEhr
{
    public class AllergyIntoleranceAdapter : FhirToOpenEhrAdapterBase
    {
        public override Dictionary<string, string> MappedTermsDictionary =>
            new Dictionary<string, string>
            {
                { "Entry", "allergies_and_adverse_reactions" },
                { "Reaction", "reaction_event" },
                { "AllergyIntolerance", "adverse_reaction_risk" }
            };

        public override List<string> SkipNodeNames =>
            new List<string>
            {
                "TypeName",
                "Text",
                "ObjectValue",
                "SystemElement",
                "CodeElement"
            };

        public override List<string> NodePruneList =>
            new List<string>
            {
                "TextElement/Value",
                "Value"
            };

        public override Composite Convert(Bundle.EntryComponent entry, int resourceIndex)
        {
            // create the resource root using the type of the resource and iteration of the resource
            var resourceRoot = new Composite(MappedTermsDictionary[entry.Resource.TypeName] + $":{resourceIndex}");

            resourceRoot = Helpers.ReflectionHelper.GetObjectComposite(entry.Resource, resourceRoot, MappedTermsDictionary, SkipNodeNames);

            // prune nodes
            var matchingNodes = resourceRoot.All(new List<Component>()).Where(x => x.GetType() == typeof(Leaf) && NodePruneList.Any(t => x.PathToRoot.Contains(t))).ToList();

            foreach (var node in matchingNodes)
            {
                // get the parent node of the node at the start of the path
                var currentNode = node.Parent;

                // calculate how many nodes we need to traverse backwards
                var iterationCount = NodePruneList.OrderByDescending(x => x.Length)
                    .First(t => node.PathToRoot.Contains(t))?.Split('/').Length;

                // traverse nodes to get to the sub root, removing the old ones as we go
                for (var i = 0; i < iterationCount; i++)
                {
                    var oldNode = currentNode;
                    currentNode = currentNode.Parent;
                    currentNode.Remove(oldNode);
                }

                // add the leaf from the end of the path to the start of the path
                resourceRoot.FindById(currentNode.Id).Add(new Leaf(node.Name));
            }

            // done, return resource root
            return resourceRoot;
        }
    }

}
