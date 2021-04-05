using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Raijin.Core.Helpers;
using Raijin.Core.MappingLogic.OpenEhrToFhir;

namespace Raijin.Core.CompositePattern
{
    public class Leaf : Component
    {
        public Leaf(string name, Component parent = null)
            : base(name, parent)
        {
        }

        public override void Add(Component c)
        {
            Console.WriteLine("Cannot add to a leaf");
        }

        public override void Remove(Component c)
        {
            Console.WriteLine("Cannot remove from a leaf");
        }

        /// <summary>
        /// Displays a leaf node to console.
        /// </summary>
        /// <param name="depth">Depth within tree.</param>
        public override void Display(int depth = 1)
        {
            Console.WriteLine(new string('-', depth) + Name, Console.ForegroundColor = ConsoleColor.Green);
            Console.ResetColor();
        }

        /// <summary>
        /// Displays a leaf node to console as it was before parsing.
        /// </summary>
        public override void DisplayFlatFile()
        {
            var path = "\"" + string.Join("/", PathToRoot.Split('/').Take(PathToRoot.Split('/').Length - 1)) + "\"";
            var value = "\"" + PathToRoot.Split('/').Last() + "\"";
            Console.Write(path + ": ");
            Console.Write(value, Console.ForegroundColor = ConsoleColor.Green);
            Console.ResetColor();
            Console.Write(",\n");
        }

        public override string GetFlatFile()
        {
            var path = "\"" + string.Join("/", PathToRoot.Split('/').Take(PathToRoot.Split('/').Length - 1)) + "\"";
            var value = "\"" + PathToRoot.Split('/').Last() + "\"";
            return "    " + path + ": " + value + ",\n";
        }

        /// <summary>
        /// Checks if leaf matches search parameters.
        /// </summary>
        /// <param name="name">Leaf node to search for.</param>
        /// <returns>Returns this leaf node if matches search parameter.</returns>
        public override Component Find(string name)
        {
            return name == Name ? this : null;
        }

        /// <summary>
        /// Searches for a node by OpenEHR flat file path.
        /// </summary>
        /// <param name="pathToRoot">OpenEHR flat file path.</param>
        /// <returns>Returns the node which matches the search parameter.</returns>
        public override Component FindByPath(string pathToRoot)
        {
            return PathToRoot == pathToRoot ? this : null;
        }

        /// <summary>
        /// Searches for a node by it's unique guid.
        /// </summary>
        /// <param name="id">The guid of the node to find.</param>
        /// <returns>A single component of the hierarchy.</returns>
        public override Component FindById(Guid id)
        {
            return Id == id ? this : null;
        }

        /// <summary>
        /// Searches the node hierarchy for a specified search term and places the result in the nodes parameter.
        /// </summary>
        /// <param name="searchTerm">The mapping to search for.</param>
        /// <param name="nodes">This collection is returned with the matching nodes.</param>
        /// <returns>List of nodes matching the search criteria.</returns>
        public override List<Component> FindMany(BaseMapping searchTerm, List<Component> nodes)
        {
            if (!string.IsNullOrEmpty(searchTerm.RegexPattern))
            {
                if (Regex.Match(Name, searchTerm.RegexPattern).Success)
                {
                    nodes.Add(this);
                }
            }
            else
            {
                if (searchTerm.OpenEhrFieldPath == Name)
                {
                    nodes.Add(this);
                }
            }

            return nodes;
        }

        /// <summary>
        /// Searches the node hierarchy for a specified search term and places the result in the nodes parameter.
        /// </summary>
        /// <param name="searchTerm">The string to search for.</param>
        /// <param name="nodes">This collection is returned with the matching nodes.</param>
        /// <returns>List of nodes matching the search criteria.</returns>
        public override List<Component> FindMany(string searchTerm, List<Component> nodes)
        {
            if (searchTerm == Name)
            {
                nodes.Add(this);
            }

            return nodes;
        }

        public override string NetPath(List<BaseMapping> mappings)
        {
            var path = "";

            var parts = PathToRoot.Split('/');

            foreach (var part in parts)
            {
                var listParts = part.Split(':');
                var codeableConceptParts = part.Split('|');
                if (listParts.Length > 1)
                {
                    var field = listParts[0];
                    var index = listParts[1];
                    var mapping = mappings.FirstOrDefault(x => field + ":" == x.OpenEhrFieldPath);
                    if (mapping != null)
                    {
                        var mappingType = mapping.MappingType;

                        switch (mappingType)
                        {
                            case MappingType.Resource:
                                field = "ResourceList";
                                break;
                            default:
                                field = mapping.FhirFieldPath;
                                break;
                        }

                        path += $"{field}[{index}]";
                        if (part != parts.Last())
                            path += ".";
                    }
                }
                else if (codeableConceptParts.Length > 1)
                {
                    var field = codeableConceptParts[0];
                    var subField = codeableConceptParts[1];
                    var mapping = mappings.FirstOrDefault(x => field + "|" == x.OpenEhrFieldPath);
                    if (mapping != null)
                    {
                        var mappingType = mapping.MappingType;

                        switch (mappingType)
                        {
                            case MappingType.Resource:
                                field = "ResourceList";
                                break;
                            default:
                                {
                                    field = mapping.FhirFieldPath;
                                    if (field.Contains("[]"))
                                    {
                                        var emptyIndexCount = field.GetSubStringCount();
                                        for (var i = 0; i < emptyIndexCount; i++)
                                        {
                                            var parent = Parent; // TODO may need to rework this
                                            for (var j = 0; j < i; j++)
                                            {
                                                parent = (Composite)parent.Parent;
                                            }

                                            var insideIndexLocation = field.GetIndexOfNthOccurence("[]", i + 1) + 1;
                                            var indexNumber =
                                                parent.Parent.NetPath(mappings) !=
                                                $"ResourceList[{parent.ParentResourceIndex(mappings)}]"
                                                    ? parent.Parent.ListIndex
                                                    : 0;
                                            field = field.Insert(insideIndexLocation, indexNumber.ToString());
                                        }
                                    }
                                    break;
                                }
                        }

                        path += $"{field}";

                        if (part != parts.Last())
                            path += ".";
                    }
                }
                else
                {
                    var mapping = mappings.FirstOrDefault(x => parts.Last() == x.OpenEhrFieldPath);
                    if (mapping != null)
                    {
                        var mappingType = mapping.MappingType;

                        string field;
                        switch (mappingType)
                        {
                            case MappingType.Resource:
                                field = "ResourceList";
                                break;
                            default:
                                {
                                    field = mapping.FhirFieldPath;
                                    if (field.Contains("[]"))
                                    {
                                        var emptyIndexCount = field.GetSubStringCount();
                                        for (var i = 0; i < emptyIndexCount; i++)
                                        {
                                            var parent = Parent; // TODO may need to rework this
                                            for (var j = 0; j < i; j++)
                                            {
                                                parent = (Composite)parent.Parent;
                                            }

                                            var insideIndexLocation = field.GetIndexOfNthOccurence("[]", i + 1) + 1;
                                            var indexNumber =
                                                parent.Parent.NetPath(mappings) !=
                                                $"ResourceList[{parent.ParentResourceIndex(mappings)}]"
                                                    ? parent.Parent.ListIndex
                                                    : 0;
                                            field = field.Insert(insideIndexLocation, indexNumber.ToString());
                                        }
                                    }
                                    break;
                                }
                        }

                        path += $"{field}";
                        break;
                    }
                }
            }

            if (ListIndex != int.MinValue)
            {
                path = path.Remove(path.Length - 3);
            }

            if (path.Length > 0 && path[path.Length - 1] == '.')
                path = path.Remove(path.Length - 1);

            return path;
        }

        public override string InnerNetPath(List<BaseMapping> mappings)
        {
            var path = "";

            var parts = PathToRoot.Split('/');

            foreach (var part in parts)
            {
                var listParts = part.Split(':');
                var codeableConceptParts = part.Split('|');
                if (listParts.Length > 1)
                {
                    var field = listParts[0];
                    var index = listParts[1];
                    var mapping = mappings.FirstOrDefault(x => field + ":" == x.OpenEhrFieldPath);
                    if (mapping != null)
                    {
                        var mappingType = mapping.MappingType;

                        switch (mappingType)
                        {
                            case MappingType.Resource:
                                continue;
                            default:
                                field = mapping.FhirFieldPath;
                                break;
                        }

                        path += $"{field}[{index}]";
                        if (part != parts.Last())
                            path += ".";
                    }
                }
                else if (codeableConceptParts.Length > 1)
                {
                    var field = codeableConceptParts[0];
                    var subField = codeableConceptParts[1];
                    var mapping = mappings.FirstOrDefault(x => field + "|" == x.OpenEhrFieldPath);
                    if (mapping != null)
                    {
                        var mappingType = mapping.MappingType;

                        switch (mappingType)
                        {
                            case MappingType.Resource:
                                field = "ResourceList";
                                break;
                            default:
                                {
                                    field = mapping.FhirFieldPath;
                                    if (field.Contains("[]"))
                                    {
                                        var emptyIndexCount = field.GetSubStringCount();
                                        for (var i = 0; i < emptyIndexCount; i++)
                                        {
                                            var parent = Parent; // TODO may need to rework this
                                            for (var j = 0; j < i; j++)
                                            {
                                                parent = (Composite)parent.Parent;
                                            }

                                            var insideIndexLocation = field.GetIndexOfNthOccurence("[]", i + 1) + 1;
                                            var indexNumber =
                                                parent.Parent.NetPath(mappings) !=
                                                $"ResourceList[{parent.ParentResourceIndex(mappings)}]"
                                                    ? parent.Parent.ListIndex
                                                    : 0;
                                            field = field.Insert(insideIndexLocation, indexNumber.ToString());
                                        }
                                    }
                                    break;
                                }
                        }

                        path += $"{field}";

                        if (part != parts.Last())
                            path += ".";
                    }
                }
                else
                {
                    var mapping = mappings.FirstOrDefault(x => parts.Last() == x.OpenEhrFieldPath);
                    if (mapping != null)
                    {
                        var mappingType = mapping.MappingType;

                        string field;
                        switch (mappingType)
                        {
                            case MappingType.Resource:
                                field = "ResourceList";
                                break;
                            default:
                                {
                                    field = mapping.FhirFieldPath;
                                    if (field.Contains("[]"))
                                    {
                                        var emptyIndexCount = field.GetSubStringCount();
                                        for (var i = 0; i < emptyIndexCount; i++)
                                        {
                                            var parent = Parent; // TODO may need to rework this
                                            for (var j = 0; j < i; j++)
                                            {
                                                parent = (Composite)parent.Parent;
                                            }

                                            var insideIndexLocation = field.GetIndexOfNthOccurence("[]", i + 1) + 1;
                                            var indexNumber =
                                                parent.Parent.NetPath(mappings) !=
                                                $"ResourceList[{parent.ParentResourceIndex(mappings)}]"
                                                    ? parent.Parent.ListIndex
                                                    : 0;
                                            field = field.Insert(insideIndexLocation, indexNumber.ToString());
                                        }
                                    }
                                    break;
                                }
                        }

                        path += $"{field}";

                        break;
                    }
                }
            }

            if (ListIndex != int.MinValue)
            {
                path = path.Remove(path.Length - 3);
            }

            return path;
        }

        public override int ParentResourceIndex(List<BaseMapping> mappings)
        {
            var parts = PathToRoot.Split('/');
            foreach (var part in parts)
            {
                var subParts = part.Split(':');
                if (subParts.Length > 1)
                {
                    var field = subParts[0];
                    var index = subParts[1];
                    var mapping = mappings.FirstOrDefault(x => field + ":" == x.OpenEhrFieldPath);
                    if (mapping != null)
                    {
                        if (mapping.MappingType == MappingType.Resource)
                            return int.Parse(index);
                    }
                }
            }

            return int.MinValue;
        }

        public override List<Component> All(List<Component> nodes)
        {
            nodes.Add(this);
            return nodes;
        }
    }
}
