using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using D3DLab.ECS;

namespace D3DLab.Std.Engine.Core.MeshFormats {
    public class ObjGroupInfo {
        public string Name;
        public int StartIndex;
        public int Count;
    }
    public class ObjGroup {
        public ObjGroupInfo PosGroupInfo;
        public ObjGroupInfo IndxGroupInfo;
        public readonly string Name;
        public ObjGroup(string name) {
            Name = name;
        }
    }
    public class OrderedObjGroups {
        public readonly List<ObjGroup> Groups;
        public readonly string Name;
        public OrderedObjGroups(string name, List<ObjGroup> groups) {
            Name = name;
            Groups = groups;
        }
    }

    public class ObjGroupsComponent : GraphicComponent {
        public readonly List<OrderedObjGroups> OrderedGroups;

        public ObjGroupsComponent() {
            OrderedGroups = new List<OrderedObjGroups>();
        }
    }

}
