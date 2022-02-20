using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
// ReSharper disable NonReadonlyMemberInGetHashCode

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMDefinition
    {
        // This contains Enum values, Constant variables, Defines.
        public List<string> Constants;

        public List<SMConstant> ConstVariables = new();

        // The function variables (where the cursor is in)
        private readonly List<string> FunctionVariableStrings = new();

        // TODO: Typedefs are not added correctly.
        // This contains Enum, Structs, Methodmaps, Typedefs, Enum structs' names.
        public List<string> TypeStrings = new();

        // Top-level variables
        public readonly List<SMVariable> Variables = new();
        public readonly List<SMEnum> Enums = new();
        public readonly List<SMEnumStruct> EnumStructs = new();
        public readonly List<SMMethodmap> Methodmaps = new();

        public string[] FunctionStrings = new string[0];


        // This contains method map and enum structs' methods.
        public List<string> ObjectMethods;
        // This contains method map and enum structs' fields.
        public List<string> ObjectFields;

        public List<SMFunction> Functions = new();
        public List<SMDefine> Defines = new();
        public List<SMStruct> Structs = new();
        public List<SMTypedef> Typedefs = new();

        public SMFunction CurrentFunction;

        public void Sort()
        {
            try
            {
                Functions = Functions.Distinct(new SMFunctionComparer()).ToList();
                Functions.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
                //Enums = Enums.Distinct(new SMEnumComparer()).ToList(); //enums can have the same name but not be the same...
                Enums.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
                Structs = Structs.Distinct(new SMStructComparer()).ToList();
                Structs.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
                Defines = Defines.Distinct(new SMDefineComparer()).ToList();
                Defines.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
                ConstVariables = ConstVariables.Distinct(new SMConstantComparer()).ToList();
                ConstVariables.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            }
            catch (Exception)
            {
                // ignored
            } //racing condition on save when the thread closes first or not..
        }

        public void AppendFiles(IEnumerable<string> paths, out List<string> rejectedPaths)
        {
            rejectedPaths = new();
            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        var files = Directory.GetFiles(path, "*.inc", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            var fInfo = new FileInfo(file);
                            var subCondenser = new Condenser(File.ReadAllText(fInfo.FullName), fInfo.FullName);
                            var subDefinition = subCondenser.Condense();
                            Functions.AddRange(subDefinition.Functions);
                            Enums.AddRange(subDefinition.Enums);
                            Structs.AddRange(subDefinition.Structs);
                            Defines.AddRange(subDefinition.Defines);
                            ConstVariables.AddRange(subDefinition.ConstVariables);
                            Methodmaps.AddRange(subDefinition.Methodmaps);
                            Typedefs.AddRange(subDefinition.Typedefs);
                            EnumStructs.AddRange(subDefinition.EnumStructs);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        rejectedPaths.Add(path);
                    }
                }
            }

            Sort();
            ProduceStringArrays();
        }

        private void ProduceStringArrays(int caret = -1, List<SMFunction> currentFunctions = null)
        {
            FunctionStrings = new string[Functions.Count];
            for (var i = 0; i < Functions.Count; ++i)
            {
                FunctionStrings[i] = Functions[i].Name;
            }


            ObjectMethods = new List<string>();
            ObjectFields = new List<string>();

            foreach (var mm in Methodmaps)
            {
                ObjectMethods.AddRange(mm.Methods.Select(m => m.Name));
                ObjectFields.AddRange(mm.Fields.Select(f => f.Name));
            }

            foreach (var sm in EnumStructs)
            {
                ObjectMethods.AddRange(sm.Methods.Select(m => m.Name));
                ObjectFields.AddRange(sm.Fields.Select(f => f.Name));
            }

            Constants = new List<string>();

            // Add Enum values
            foreach (var e in Enums)
            {
                Constants.AddRange(e.Entries);
            }

            Constants.AddRange(ConstVariables.Select(i => i.Name).ToList());
            Constants.AddRange(Defines.Select(i => i.Name).ToList());

            Constants.Sort(string.Compare);

            TypeStrings = Enums.Select(e => e.Name).Concat(Structs.Select(e => e.Name))
                .Concat(Methodmaps.Select(e => e.Name)).Concat(Typedefs.Select(e => e.Name))
                .Concat(EnumStructs.Select(e => e.Name)).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            TypeStrings.Sort(string.Compare);


            // Parse local function variables.
            if (caret != -1 && currentFunctions != null)
            {
                if (CurrentFunction != null)
                {
                    FunctionVariableStrings.AddRange(CurrentFunction.FuncVariables.Select(v => v.Name));
                    var stringParams = CurrentFunction.Parameters.Select(e => e.Split('=').First().Trim())
                        .Select(e => e.Split(' ').Last()).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
                    FunctionVariableStrings.AddRange(stringParams);
                }
                else
                {
                    FunctionVariableStrings.Clear();
                }
            }
        }

        public List<ACNode> ProduceACNodes()
        {
            var nodes = new List<ACNode>
            {
                Capacity = Enums.Count + Structs.Count + ConstVariables.Count + Functions.Count + EnumStructs.Count
            };
            nodes.AddRange(ACNode.ConvertFromStringArray(FunctionStrings, true, "▲ "));
            nodes.AddRange(ACNode.ConvertFromStringList(TypeStrings, false, "♦ "));
            nodes.AddRange(ACNode.ConvertFromStringList(Constants, false, "• "));
            nodes.AddRange(ACNode.ConvertFromStringList(Methodmaps.Select(e => e.Name), false, "↨ "));
            nodes.AddRange(ACNode.ConvertFromStringList(EnumStructs.Select(e => e.Name), false, "↩ "));

            nodes.AddRange(ACNode.ConvertFromStringList(Variables.Select(e => e.Name), false, "• "));
            nodes.AddRange(ACNode.ConvertFromStringList(FunctionVariableStrings, false, "• "));
            
            //nodes = nodes.Distinct(new ACNodeEqualityComparer()).ToList(); Methodmaps and Functions can and will be the same.
            nodes.Sort((a, b) => string.CompareOrdinal(a.EntryName, b.EntryName));

            return nodes;
        }

        public List<ISNode> ProduceISNodes()
        {
            var nodes = new List<ISNode>();
            nodes.AddRange(ISNode.ConvertFromStringList(ObjectMethods, true, "▲ "));
            nodes.AddRange(ISNode.ConvertFromStringList(ObjectFields, false, "• "));

            // nodes.AddRange(ISNode.ConvertFromStringArray(VariableStrings, false, "v "));

            nodes = nodes.Distinct(new ISNodeEqualityComparer()).ToList();
            nodes.Sort((a, b) => string.CompareOrdinal(a.EntryName, b.EntryName));

            return nodes;
        }

        public void MergeDefinitions(SMDefinition def)
        {
            try
            {
                Functions.AddRange(def.Functions);
                Typedefs.AddRange(def.Typedefs);
                Enums.AddRange(def.Enums);
                Structs.AddRange(def.Structs);
                Defines.AddRange(def.Defines);
                ConstVariables.AddRange(def.ConstVariables);
                Methodmaps.AddRange(def.Methodmaps);
                EnumStructs.AddRange(def.EnumStructs);
                Variables.AddRange(def.Variables);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public SMDefinition ProduceTemporaryExpandedDefinition(IEnumerable<SMDefinition> definitions, int caret,
            List<SMFunction> currentFunctions)
        {
            var def = new SMDefinition();
            def.MergeDefinitions(this);
            foreach (var definition in definitions)
            {
                if (definition == null)
                    continue;

                def.MergeDefinitions(definition);
                def.CurrentFunction ??= definition.CurrentFunction;
            }

            def.Sort();
            def.ProduceStringArrays(caret, currentFunctions);
            return def;
        }

        private class SMFunctionComparer : IEqualityComparer<SMFunction>
        {
            public bool Equals(SMFunction left, SMFunction right)
            {
                return left?.Name == right?.Name;
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
                return left?.Name == right?.Name;
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
                return left?.Name == right?.Name;
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
                return left?.Name == right?.Name;
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
                return left?.Name == right?.Name;
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
                return left?.Name == right?.Name;
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
                return nodeA?.EntryName == nodeB?.EntryName;
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
        public bool IsExecutable;
        public string Name;
        // Used for pre-processor statments.
        public bool addSpace;

        public static List<ACNode> ConvertFromStringArray(string[] strings, bool executable, string prefix = "", bool addSpace = false)
        {
            var nodeList = new List<ACNode>();
            var length = strings.Length;
            for (var i = 0; i < length; ++i)
            {
                nodeList.Add(
                    new ACNode {Name = prefix + strings[i], EntryName = strings[i], IsExecutable = executable});
            }

            return nodeList;
        }

        public static IEnumerable<ACNode> ConvertFromStringList(IEnumerable<string> strings, bool executable,
            string prefix = "", bool addSpace = false)
        {
            return strings.Select(e => new ACNode {Name = prefix + e, EntryName = e, IsExecutable = executable, addSpace = addSpace});
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

        public static IEnumerable<ISNode> ConvertFromStringArray(string[] strings, bool Executable, string prefix = "")
        {
            var nodeList = new List<ISNode>();
            var length = strings.Length;
            for (var i = 0; i < length; ++i)
            {
                nodeList.Add(
                    new ISNode {Name = prefix + strings[i], EntryName = strings[i], IsExecuteable = Executable});
            }

            return nodeList;
        }

        public static IEnumerable<ISNode> ConvertFromStringList(IEnumerable<string> strings, bool executable,
            string prefix = "")
        {
            return strings.Select(t => new ISNode {Name = prefix + t, EntryName = t, IsExecuteable = executable})
                .ToList();
        }

        public override string ToString()
        {
            return Name;
        }
    }
    
    
    class ISNodeComparer : IEqualityComparer<ISNode>
    {
        // Products are equal if their names and product numbers are equal.
        public bool Equals(ISNode x, ISNode y)
        {

            //Check whether the compared objects reference the same data.
            if (ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                return false;

            //Check whether the products' properties are equal.
            return x.EntryName == y.EntryName && x.IsExecuteable == y.IsExecuteable;
        }
        

        public int GetHashCode(ISNode isNode)
        {
            //Get hash code for the Name field if it is not null.
            int nameHash = isNode.Name == null ? 0 : isNode.Name.GetHashCode();
            int isExecutableHash = isNode.IsExecuteable.GetHashCode();

            //Calculate the hash code for the product.
            return nameHash ^ isExecutableHash;
        }
    }
}