using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMDefinition
    {
        public List<SMConstant> Constants = new List<SMConstant>();

        //public string[] EnumStrings = new string[0]; NOT NEEDED
        //public string[] StructStrings = new string[0]; NOT NEEDED
        //public string[] DefinesStrings = new string[0]; NOT NEEDED

        public List<SMDefine> Defines = new List<SMDefine>();
        public List<SMEnum> Enums = new List<SMEnum>();
        public List<SMFunction> Functions = new List<SMFunction>();
        public List<SMMethodmap> Methodmaps = new List<SMMethodmap>();
        public List<SMStruct> Structs = new List<SMStruct>();
        public List<SMTypedef> Typedefs = new List<SMTypedef>();
        public List<SMEnumStruct> EnumStructs = new List<SMEnumStruct>();


        /* Methodmaps */
        public string[] MethodmapsStrings = new string[0];
        public string[] MethodsStrings = new string[0];
        public string[] FieldStrings = new string[0];

        /* Enum structs */
        public string[] EnumStructStrings = new string[0];
        public string[] StructFieldStrings = new string[0];
        public string[] StructMethodStrings = new string[0];
        
        /* Other */
        public string[] FunctionStrings = new string[0];
        public string[] TypeStrings = new string[0];
        public string[]
            ConstantsStrings = new string[0]; //ATTENTION: THIS IS NOT THE LIST OF ALL CONSTANTS - IT INCLUDES MUCH MORE


        public void Sort()
        {
            try
            {
                Functions = Functions.Distinct(new SMFunctionComparer()).ToList();
                Functions.Sort((a, b) => { return string.Compare(a.Name, b.Name); });
                //Enums = Enums.Distinct(new SMEnumComparer()).ToList(); //enums can have the same name but not be the same...
                Enums.Sort((a, b) => { return string.Compare(a.Name, b.Name); });
                Structs = Structs.Distinct(new SMStructComparer()).ToList();
                Structs.Sort((a, b) => { return string.Compare(a.Name, b.Name); });
                Defines = Defines.Distinct(new SMDefineComparer()).ToList();
                Defines.Sort((a, b) => { return string.Compare(a.Name, b.Name); });
                Constants = Constants.Distinct(new SMConstantComparer()).ToList();
                Constants.Sort((a, b) => { return string.Compare(a.Name, b.Name); });
            }
            catch (Exception)
            {
            } //racing condition on save when the thread closes first or not..
        }

        public void AppendFiles(string[] paths)
        {
            for (var i = 0; i < paths.Length; ++i)
                if (Directory.Exists(paths[i]))
                {
                    var files = Directory.GetFiles(paths[i], "*.inc", SearchOption.AllDirectories);
                    for (var j = 0; j < files.Length; ++j)
                    {
                        var fInfo = new FileInfo(files[j]);
                        var subCondenser = new Condenser(File.ReadAllText(fInfo.FullName), fInfo.Name);
                        var subDefinition = subCondenser.Condense();
                        Functions.AddRange(subDefinition.Functions);
                        Enums.AddRange(subDefinition.Enums);
                        Structs.AddRange(subDefinition.Structs);
                        Defines.AddRange(subDefinition.Defines);
                        Constants.AddRange(subDefinition.Constants);
                        Methodmaps.AddRange(subDefinition.Methodmaps);
                        Typedefs.AddRange(subDefinition.Typedefs);
                        EnumStructs.AddRange(subDefinition.EnumStructs);
                    }
                }

            Sort();
            ProduceStringArrays();
        }

        public void ProduceStringArrays()
        {
            FunctionStrings = new string[Functions.Count];
            for (var i = 0; i < Functions.Count; ++i) FunctionStrings[i] = Functions[i].Name;
            var methodNames = new List<string>();
            var fieldNames = new List<string>();
            var methodmapNames = new List<string>();
            var enumStructNames = new List<string>();
            var structMethodNames = new List<string>();
            var structFieldNames = new List<string>();
            foreach (var mm in Methodmaps)
            {
                methodmapNames.Add(mm.Name);
                methodNames.AddRange(mm.Methods.Select(m => m.Name));
                fieldNames.AddRange(mm.Fields.Select(f => f.Name));
            }

            foreach (var sm in EnumStructs)
            {
                enumStructNames.Add(sm.Name);
                structMethodNames.AddRange(sm.Methods.Select(m => m.Name));
                structFieldNames.AddRange(sm.Fields.Select(f => f.Name));
            }

            MethodsStrings = methodNames.ToArray();
            FieldStrings = fieldNames.ToArray(); 
            StructFieldStrings = structFieldNames.ToArray();
            StructMethodStrings = structMethodNames.ToArray();
            MethodmapsStrings = methodmapNames.ToArray();
            EnumStructStrings = enumStructNames.ToArray();

            var constantNames = Constants.Select(i => i.Name).ToList();
            foreach (var e in Enums) constantNames.AddRange(e.Entries);

            constantNames.AddRange(Defines.Select(i => i.Name));
            constantNames.Sort((a, b) => string.Compare(a, b));
            ConstantsStrings = constantNames.ToArray();
            var typeNames = new List<string>();
            typeNames.Capacity = Enums.Count + Structs.Count + Methodmaps.Count;
            typeNames.AddRange(Enums.Select(i => i.Name));
            typeNames.AddRange(Structs.Select(i => i.Name));
            typeNames.AddRange(Methodmaps.Select(i => i.Name));
            typeNames.AddRange(Typedefs.Select(i => i.Name));
            typeNames.Sort((a, b) => string.Compare(a, b));
            TypeStrings = typeNames.ToArray();
        }

        public ACNode[] ProduceACNodes()
        {
            var nodes = new List<ACNode>();
            try
            {
                nodes.Capacity = Enums.Count + Structs.Count + Constants.Count + Functions.Count;
                nodes.AddRange(ACNode.ConvertFromStringArray(FunctionStrings, true, "▲ "));
                nodes.AddRange(ACNode.ConvertFromStringArray(TypeStrings, false, "♦ "));
                nodes.AddRange(ACNode.ConvertFromStringArray(ConstantsStrings, false, "• "));
                nodes.AddRange(ACNode.ConvertFromStringArray(MethodmapsStrings, false, "↨ "));
                nodes.AddRange(ACNode.ConvertFromStringArray(EnumStructStrings, false, "↩ "));
                //nodes = nodes.Distinct(new ACNodeEqualityComparer()).ToList(); Methodmaps and Functions can and will be the same.
                nodes.Sort((a, b) => { return string.Compare(a.EntryName, b.EntryName); });
            }
            catch (Exception)
            {
            }

            return nodes.ToArray();
        }

        public ISNode[] ProduceISNodes()
        {
            var nodes = new List<ISNode>();
            try
            {
                nodes.AddRange(ISNode.ConvertFromStringArray(MethodsStrings, true, "▲ "));
                nodes.AddRange(ISNode.ConvertFromStringArray(StructMethodStrings, true, "▲ "));
                
                nodes.AddRange(ISNode.ConvertFromStringArray(FieldStrings, false, "• "));
                nodes.AddRange(ISNode.ConvertFromStringArray(StructFieldStrings, false, "• "));

                nodes = nodes.Distinct(new ISNodeEqualityComparer()).ToList();
                nodes.Sort((a, b) => { return string.Compare(a.EntryName, b.EntryName); });
            }
            catch (Exception)
            {
            }

            return nodes.ToArray();
        }

        public void MergeDefinitions(SMDefinition def)
        {
            try
            {
                Functions.AddRange(def.Functions);
                Enums.AddRange(def.Enums);
                Structs.AddRange(def.Structs);
                Defines.AddRange(def.Defines);
                Constants.AddRange(def.Constants);
                Methodmaps.AddRange(def.Methodmaps);
                EnumStructs.AddRange(def.EnumStructs);
            }
            catch (Exception)
            {
            }
        }

        public SMDefinition ProduceTemporaryExpandedDefinition(SMDefinition[] definitions)
        {
            var def = new SMDefinition();
            try
            {
                def.MergeDefinitions(this);
                for (var i = 0; i < definitions.Length; ++i)
                    if (definitions[i] != null)
                        def.MergeDefinitions(definitions[i]);
                def.Sort();
                def.ProduceStringArrays();
            }
            catch (Exception)
            {
            }

            return def;
        }

        private class SMFunctionComparer : IEqualityComparer<SMFunction>
        {
            public bool Equals(SMFunction left, SMFunction right)
            {
                return left.Name == right.Name;
            }

            public int GetHashCode(SMFunction sm)
            {
                return sm.Name.GetHashCode();
            }
        }

        private class SMEnumComparer : IEqualityComparer<SMEnum>
        {
            public bool Equals(SMEnum left, SMEnum right)
            {
                return left.Name == right.Name;
            }

            public int GetHashCode(SMEnum sm)
            {
                return sm.Name.GetHashCode();
            }
        }

        private class SMStructComparer : IEqualityComparer<SMStruct>
        {
            public bool Equals(SMStruct left, SMStruct right)
            {
                return left.Name == right.Name;
            }

            public int GetHashCode(SMStruct sm)
            {
                return sm.Name.GetHashCode();
            }
        }

        private class SMDefineComparer : IEqualityComparer<SMDefine>
        {
            public bool Equals(SMDefine left, SMDefine right)
            {
                return left.Name == right.Name;
            }

            public int GetHashCode(SMDefine sm)
            {
                return sm.Name.GetHashCode();
            }
        }

        private class SMConstantComparer : IEqualityComparer<SMConstant>
        {
            public bool Equals(SMConstant left, SMConstant right)
            {
                return left.Name == right.Name;
            }

            public int GetHashCode(SMConstant sm)
            {
                return sm.Name.GetHashCode();
            }
        }

        private class SMMethodmapsComparer : IEqualityComparer<SMMethodmap>
        {
            public bool Equals(SMMethodmap left, SMMethodmap right)
            {
                return left.Name == right.Name;
            }

            public int GetHashCode(SMMethodmap sm)
            {
                return sm.Name.GetHashCode();
            }
        }

        /*public class ACNodeEqualityComparer : IEqualityComparer<ACNode>
        {
            public bool Equals(ACNode nodeA, ACNode nodeB)
            { return nodeA.EntryName == nodeB.EntryName; }

            public int GetHashCode(ACNode node)
            { return node.EntryName.GetHashCode(); }
        }*/
        public class ISNodeEqualityComparer : IEqualityComparer<ISNode>
        {
            public bool Equals(ISNode nodeA, ISNode nodeB)
            {
                return nodeA.EntryName == nodeB.EntryName;
            }

            public int GetHashCode(ISNode node)
            {
                return node.EntryName.GetHashCode();
            }
        }
    }

    public class ACNode
    {
        public string EntryName;
        public bool IsExecuteable;
        public string Name;

        public static List<ACNode> ConvertFromStringArray(string[] strings, bool Executable, string prefix = "")
        {
            var nodeList = new List<ACNode>();
            var length = strings.Length;
            for (var i = 0; i < length; ++i)
                nodeList.Add(
                    new ACNode {Name = prefix + strings[i], EntryName = strings[i], IsExecuteable = Executable});
            return nodeList;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class ISNode
    {
        public string EntryName;
        public bool IsExecuteable;
        public string Name;

        public static List<ISNode> ConvertFromStringArray(string[] strings, bool Executable, string prefix = "")
        {
            var nodeList = new List<ISNode>();
            var length = strings.Length;
            for (var i = 0; i < length; ++i)
                nodeList.Add(
                    new ISNode {Name = prefix + strings[i], EntryName = strings[i], IsExecuteable = Executable});
            return nodeList;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}