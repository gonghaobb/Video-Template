//
//      __  __       _______ _____  _______   __
//      |  \/  |   /\|__   __|  __ \|_   _\ \ / /
//      | \  / |  /  \  | |  | |__) | | |  \ V / 
//      | |\/| | / /\ \ | |  |  _  /  | |   > <  
//      | |  | |/ ____ \| |  | | \ \ _| |_ / . \ 
//      |_|  |_/_/    \_\_|  |_|  \_\_____/_/ \_\                        
//									   (ByteDance)
//
//      Created by Matrix team.
//      Procedural LOGO:https://www.shadertoy.com/view/ftKBRW
//
//      The team was set up on September 4, 2019.
//

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;

namespace Matrix.EcosystemSimulate
{
    using IProvider = FilterWindow.IProvider;
    using Element = FilterWindow.Element;
    using GroupElement = FilterWindow.GroupElement;

    public class SubEcosystemProvider : IProvider
    {
        private class SubEcosystemElement : Element
        {
            public Type type;
            public SubEcosystemElement(int level, string label, Type type)
            {
                this.level = level;
                this.type = type;
                content = new GUIContent(label);
            }
        }

        private class PathNode : IComparable<PathNode>
        {
            public Type type;
            public string name;
            public readonly List<PathNode> nodeList = new List<PathNode>();
            public int CompareTo(PathNode other)
            {
                return string.Compare(name, other.name, StringComparison.Ordinal);
            }
        }

        public Vector2 position { get; set; }

        private EcosystemManager m_Target;
        private EcosystemManagerEditor m_TargetManagerEditor;

        public SubEcosystemProvider(EcosystemManager target, EcosystemManagerEditor managerEditor)
        {
            m_Target = target;
            m_TargetManagerEditor = managerEditor;
        }

        public void CreateComponentTree(List<Element> tree)
        {
            tree.Add(new GroupElement(0, "选择子模块"));

            IEnumerable<Type> types = CoreUtils.GetAllTypesDerivedFrom<SubEcosystem>().Where(t => !t.IsAbstract);
            PathNode rootNode = new PathNode();

            foreach (Type t in types)
            {
                string strAll = t.ToString();
                int lastPos = strAll.LastIndexOf('.');
                string strType = strAll.Substring(lastPos + 1, strAll.Length - lastPos - 1);
                EcosystemType eType = (EcosystemType)Enum.Parse(typeof(EcosystemType), strType, true);
                if(m_Target.subEcosystemDict.Contains(eType))
                {
                    continue;
                }
                string path = m_TargetManagerEditor.SUB_ECOSYSTEM_NAME_DICT[eType];
                AddNode(rootNode, path, t);
            }

            Traverse(rootNode, 1, tree);
        }

        public bool GoToChild(Element element, bool addIfComponent)
        {
            if (element is SubEcosystemElement ecosystemElement)
            {
                m_Target.CreateSubManager(ecosystemElement.type);
                return true;
            }

            return false;
        }

        private static void AddNode(PathNode root, string path, Type type)
        {
            PathNode current = root;
            string[] parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                PathNode child = current.nodeList.Find(x => x.name == part);

                if (child == null)
                {
                    child = new PathNode { name = part, type = type };
                    current.nodeList.Add(child);
                }

                current = child;
            }
        }

        private static void Traverse(PathNode node, int depth, List<Element> tree)
        {
            node.nodeList.Sort();

            foreach (PathNode n in node.nodeList)
            {
                if (n.nodeList.Count > 0) // Group
                {
                    tree.Add(new GroupElement(depth, n.name));
                    Traverse(n, depth + 1, tree);
                }
                else // Element
                {
                    tree.Add(new SubEcosystemElement(depth, n.name, n.type));
                }
            }
        }
    }
}
