using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Raijin.Core.MappingLogic.OpenEhrToFhir;

namespace Raijin.Core.CompositePattern
{
    public abstract class Component
    {
        public Guid Id;
        public string Name;
        public int ListIndex = int.MinValue;
        public Component Parent;

        protected Component(string name, Component parent = null)
        {
            Id = Guid.NewGuid();
            Name = name;
            Parent = parent;
            if (name.Contains(":") && !name.Contains("::") && !name.Contains(": "))
            {
                int.TryParse(name.Split(':').Last(), out ListIndex);
            }

            if (Regex.Match(name, ":\\d+\\|+").Success)
            {
                var startIndex = name.IndexOf(":") + 1;
                var endIndex = name.IndexOf("|");
                var index = name.Substring(startIndex, endIndex - startIndex);
                int.TryParse(index, out ListIndex);
            }
        }

        public abstract Component Find(string name);

        public abstract List<Component> All(List<Component> nodes);
        public abstract List<Component> FindMany(BaseMapping searchTerm, List<Component> nodes);
        public abstract List<Component> FindMany(string searchTerm, List<Component> nodes);
        public abstract Component FindById(Guid id);
        public abstract Component FindByPath(string pathToRoot);
        public abstract void Add(Component c);
        public abstract void Remove(Component c);
        public abstract void Display(int depth = 1);
        public abstract void DisplayFlatFile();
        public abstract string GetFlatFile();
        public abstract int ParentResourceIndex(List<BaseMapping> mappings);
        public abstract string NetPath(List<BaseMapping> mappings);
        public abstract string InnerNetPath(List<BaseMapping> mappings);

        public string PathToRoot
        {
            get
            {
                var pos = this;
                var parts = new List<string>();
                while (pos.Parent != null)
                {
                    parts.Add(pos.Name);
                    pos = pos.Parent;
                }
                if (pos.Parent == null)
                {
                    parts.Add(pos.Name);
                }
                parts.Reverse();
                return string.Join("/", parts.Skip(1));
            }
        }
    }
}
