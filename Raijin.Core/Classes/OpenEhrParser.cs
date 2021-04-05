using Raijin.Core.CompositePattern;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Raijin.Core.Classes
{
    public static class OpenEhrParser
    {
        /// <summary>
        /// Takes an OpenEHR Flat File and turns it into a hierarchical composite model following the Composite Pattern.
        /// </summary>
        /// <param name="rootName">Desired name of the root node.</param>
        /// <param name="message">The OpenEHR Flat File message.</param>
        /// <returns>Returns the composite model which was generated using the OpenEHR flat file.</returns>
        public static Composite Parse(string rootName, string message)
        {
            // create a root node
            var root = new Composite(rootName + "_rootnode");

            // split the OpenEHR message into lines
            var lines = SplitStringOnNewLines(message);

            // iterate over all lines
            foreach (var line in lines)
            {
                // check that the line has content
                if (line.Length > 1)
                {
                    // split the line into individual parts
                    var parts = line.Split('/');

                    if (parts.Length == 1)
                        continue;

                    // iterate through all parts of the line
                    for (var i = 0; i < parts.Length; i++)
                    {
                        if (Regex.Match(line.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries)[1], @"((http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)", RegexOptions.IgnoreCase).Success)
                        {
                            parts = line.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
                        }

                        // get the node we're working on
                        var part = parts[i];
                        // check if this line is a value (leaf node)
                        if (part.Contains(": "))
                        {
                            // split sub parts
                            var subParts = part.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
                            // branch part
                            var nodeName = subParts[0];
                            // value part
                            var nodeValue = subParts[1];

                            // work out if an identical branch exists
                            var path = string.Join("/", parts.Take(i + 1));
                            var node = root.FindByPath(path);

                            // if the node is null then there it doesn't exist already
                            if (node == null)
                            {
                                // create a branch
                                var branch = new Composite(nodeName);

                                // create a leaf
                                var leaf = new Leaf(nodeValue);

                                // add leaf to branch
                                branch.Add(leaf);

                                // check if the branch is attached to the root
                                if (i == 0)
                                {
                                    // add branch to root
                                    root.Add(branch);
                                }
                                else
                                {
                                    // work out the parent of this branch
                                    var parentPath = string.Join("/", parts.Take(i));
                                    // find the parent branch using the generated path
                                    var parentBranch = root.FindByPath(parentPath);
                                    // add branch to parent branch
                                    parentBranch.Add(branch);
                                }
                            }
                        }
                        else
                        {
                            // work out if an identical branch exists
                            var path = string.Join("/", parts.Take(i + 1));
                            var node = root.FindByPath(path);

                            // if the node is null then there it doesn't exist already
                            if (node == null)
                            {
                                // create a branch
                                var branch = new Composite(part);

                                // check if the branch is attached to the root
                                if (i == 0)
                                {
                                    // add branch to root
                                    root.Add(branch);
                                }
                                else
                                {
                                    // work out the parent of this branch
                                    var parentPath = string.Join("/", parts.Take(i));
                                    // find the parent branch using the generated path
                                    var parentBranch = root.FindByPath(parentPath);
                                    // add branch to parent branch
                                    parentBranch.Add(branch);
                                }
                            }
                        }
                    }
                }
            }

            // return the parsed message
            return root;
        }

        public static string[] SplitStringOnNewLines(string input)
        {
            return input.Replace("\"", "") // remove \" from record
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None) // split on each newline
                .Select(
                    x => x.Trim()[x.Trim().Length - 1] == ',' // check if line ends with comma
                        ? x.Trim().Remove(x.Trim().Length - 1) // if line ends with comma then remove comma and trim
                        : x.Trim()) // if line doesn't end with comma then just return trimmed line
                .ToArray(); // convert to array
        }
    }

}
