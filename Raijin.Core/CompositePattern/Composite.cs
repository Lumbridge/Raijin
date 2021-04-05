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
    public class Composite : Component
    {
        public List<Component> Children = new List<Component>();

        public Composite(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Adds a branch to another branch.
        /// </summary>
        /// <param name="component">The component to append to the selected component.</param>
        public override void Add(Component component)
        {
            component.Parent = this;
            Children.Add(component);
        }

        /// <summary>
        /// Removes a component from the selected component.
        /// </summary>
        /// <param name="component">The component to remove from the selected component.</param>
        public override void Remove(Component component)
        {
            Children.Remove(component);
        }

        /// <summary>
        /// Displays the component and all child components to the console.
        /// </summary>
        /// <param name="depth">The starting depth to display the root branch.</param>
        public override void Display(int depth = 1)
        {
            Console.WriteLine(new string('-', depth) + Name);

            // Recursively display child nodes
            foreach (var component in Children)
            {
                component.Display(depth + 2);
            }
        }

        /// <summary>
        /// Display as OpenEHR flat file.
        /// </summary>
        public override void DisplayFlatFile()
        {
            foreach (var component in Children)
            {
                component.DisplayFlatFile();
            }
        }

        /// <summary>
        /// Get OpenEHR flat file as a string.
        /// </summary>
        /// <returns>OpenEHR flat file as a string.</returns>
        public override string GetFlatFile()
        {
            var flatFile = "";
            foreach (var component in Children)
            {
                flatFile += component.GetFlatFile();
            }
            return flatFile;
        }

        /// <summary>
        /// Searches for a node by name under the selected node.
        /// </summary>
        /// <param name="name">The name of the node to find.</param>
        /// <returns>The matching node or NULL if no node is found.</returns>
        public override Component Find(string name)
        {
            Component node = null;

            if (Name == name)
            {
                node = this;
            }

            foreach (var component in Children)
            {
                if (node == null)
                    node = component.Find(name);
                else
                    break;
            }

            return node;
        }

        /// <summary>
        /// Searches for a node by it's unique guid.
        /// </summary>
        /// <param name="id">The guid of the node to find.</param>
        /// <returns>A single component of the hierarchy.</returns>
        public override Component FindById(Guid id)
        {
            Component node = null;

            if (id == Id)
            {
                node = this;
            }

            foreach (var component in Children)
            {
                if (node == null)
                    node = component.FindById(id);
                else
                    break;
            }

            return node;
        }

        /// <summary>
        /// Returns all nodes from the hierarchy as a flat node collection.
        /// </summary>
        /// <param name="nodes">The list of nodes where you want the result to be put.</param>
        /// <returns>Returns a list of component nodes.</returns>
        public override List<Component> All(List<Component> nodes)
        {
            nodes.Add(this);
            foreach (var child in Children)
            {
                nodes = child.All(nodes);
            }
            return nodes;
        }

        /// <summary>
        /// Searches the node hierarchy for a specified search term and places the result in the nodes parameter.
        /// </summary>
        /// <param name="searchTerm">The mapping to search for.</param>
        /// <param name="nodes">This collection is returned with the matching nodes.</param>
        /// <returns>List of nodes matching the search criteria.</returns>
        public override List<Component> FindMany(BaseMapping searchTerm, List<Component> nodes)
        {
            // check if search contains regex
            if (!string.IsNullOrEmpty(searchTerm.RegexPattern))
            {
                var regexMatch = Regex.Match(Name, searchTerm.RegexPattern);

                if (regexMatch.Success)
                {
                    nodes.Add(this);
                }
            }
            else
            {
                if (searchTerm.OpenEhrFieldPath == Name || Name.Split(':')[0] == searchTerm.OpenEhrFieldPath)
                {
                    nodes.Add(this);
                }
            }

            foreach (var component in Children)
            {
                nodes = component.FindMany(searchTerm, nodes);
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
            if (searchTerm == Name || Name.Split(':')[0] == searchTerm)
            {
                nodes.Add(this);
            }

            foreach (var component in Children)
            {
                nodes = component.FindMany(searchTerm, nodes);
            }

            return nodes;
        }

        /// <summary>
        /// Searches for a node by OpenEHR flat file path.
        /// </summary>
        /// <param name="pathToRoot">OpenEHR flat file path.</param>
        /// <returns>Returns the node which matches the search parameter.</returns>
        public override Component FindByPath(string pathToRoot)
        {
            Component node = null;

            if (PathToRoot == pathToRoot)
            {
                node = this;
            }

            foreach (var component in Children)
            {
                if (node == null)
                    node = component.FindByPath(pathToRoot);
                else
                    break;
            }

            return node;
        }

        public override string NetPath(List<BaseMapping> mappings)
        {
            var path = "";

            var parts = PathToRoot.Split('/');

            foreach (var part in parts)
            {
                var listParts = part.Split(':');
                if (listParts.Length > 1)
                {
                    var field = listParts[0];
                    var index = listParts[1];

                    var mapping = mappings.FirstOrDefault(x => field == x.OpenEhrFieldPath);
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

                        if (index.Contains("|"))
                        {
                            var indexParts = index.Split('|');
                            path += $"{field}[{indexParts[0]}]";
                        }
                        else
                        {
                            path += $"{field}[{index}]";
                        }

                        if (part != parts.Last())
                            path += ".";
                    }
                }
                else if (part == parts.Last())
                {
                    var mapping = mappings.FirstOrDefault(x =>
                        part == x.OpenEhrFieldPath || !string.IsNullOrEmpty(x.RegexPattern) &&
                        Regex.Match(part, x.RegexPattern).Success);
                    if (mapping != null)
                    {
                        var mappingType = mapping.MappingType;

                        string field;
                        switch (mappingType)
                        {
                            case MappingType.Resource:
                                field = "ResourceList";
                                break;
                            case MappingType.Extension:
                                field = "Extension";
                                break;
                            default:
                                {
                                    field = mapping.FhirFieldPath;
                                    if (field.Contains("[]"))
                                    {
                                        var emptyIndexCount = field.GetSubStringCount();
                                        for (var i = 0; i < emptyIndexCount; i++)
                                        {
                                            var parent = this;
                                            for (var j = 0; j < i; j++)
                                            {
                                                parent = (Composite)parent.Parent;
                                            }

                                            while (parent.ListIndex == int.MinValue && parent.Parent != null)
                                            {
                                                parent = (Composite)parent.Parent;
                                            }

                                            var insideIndexLocation = field.GetIndexOfNthOccurence("[]", i + 1) + 1;

                                            var indexNumber =
                                                parent.Parent.NetPath(mappings) != $"ResourceList[{parent.ParentResourceIndex(mappings)}]"
                                                    ? parent.Parent.ListIndex
                                                    : 0;

                                            field = field.Insert(insideIndexLocation, indexNumber.ToString());

                                            if (path.EndsWith(field + "."))
                                                field = string.Empty;
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

            if (Children == null && ListIndex != int.MinValue)
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
                if (listParts.Length > 1)
                {
                    var field = listParts[0];
                    var index = listParts[1];
                    var mapping = mappings.FirstOrDefault(x => field == x.OpenEhrFieldPath);
                    if (mapping != null)
                    {
                        var mappingType = mapping.MappingType;

                        switch (mappingType)
                        {
                            case MappingType.Resource:
                                continue;
                            default:
                                if (!mapping.FhirFieldPath.Contains("[]"))
                                    field = mapping.FhirFieldPath;
                                break;
                        }

                        if (index.Contains("|"))
                        {
                            var indexParts = index.Split('|');
                            path += $"{field}[{indexParts[0]}]";
                        }
                        else
                        {
                            path += $"{field}[{index}]";
                        }

                        if (part != parts.Last())
                            path += ".";
                    }
                }
                else if (part == parts.Last())
                {
                    var mapping = mappings.FirstOrDefault(x =>
                        part == x.OpenEhrFieldPath || !string.IsNullOrEmpty(x.RegexPattern) &&
                        Regex.Match(part, x.RegexPattern).Success);
                    if (mapping != null)
                    {
                        var mappingType = mapping.MappingType;

                        string field;
                        switch (mappingType)
                        {
                            case MappingType.Resource:
                                field = "ResourceList";
                                break;
                            case MappingType.Extension:
                                field = "Extension";
                                break;
                            default:
                                {
                                    field = mapping.FhirFieldPath;
                                    if (field.Contains("[]"))
                                    {
                                        var emptyIndexCount = field.GetSubStringCount();
                                        for (var i = 0; i < emptyIndexCount; i++)
                                        {
                                            var parent = this;
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

            if (Children == null && ListIndex != int.MinValue)
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
                    var mapping = mappings.FirstOrDefault(x => field == x.OpenEhrFieldPath);
                    if (mapping != null)
                    {
                        if (mapping.MappingType == MappingType.Resource)
                            return int.Parse(index);
                    }
                }
            }

            return int.MinValue;
        }
    }
}
