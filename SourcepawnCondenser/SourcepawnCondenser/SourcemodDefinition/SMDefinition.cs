using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMDefinition
    {
        public List<SMConstant> Constants = new List<SMConstant>();

        public string[]
            ConstantsStrings = new string[0]; //ATTENTION: THIS IS NOT THE LIST OF ALL CONSTANTS - IT INCLUDES MUCH MORE

        public List<string> currentVariables = new List<string>();

        //public string[] EnumStrings = new string[0]; NOT NEEDED
        //public string[] StructStrings = new string[0]; NOT NEEDED
        //public string[] DefinesStrings = new string[0]; NOT NEEDED

        public List<SMDefine> Defines = new List<SMDefine>();
        public List<SMEnum> Enums = new List<SMEnum>();
        public List<SMEnumStruct> EnumStructs = new List<SMEnumStruct>();

        /* Enum structs */
        public string[] EnumStructStrings = new string[0];
        public string[] FieldStrings = new string[0];
        public List<SMFunction> Functions = new List<SMFunction>();

        /* Other */
        public string[] FunctionStrings = new string[0];
        public List<SMMethodmap> Methodmaps = new List<SMMethodmap>();


        /* Methodmaps */
        public string[] MethodmapsStrings = new string[0];
        public string[] MethodsStrings = new string[0];
        public string[] StructFieldStrings = new string[0];
        public string[] StructMethodStrings = new string[0];
        public List<SMStruct> Structs = new List<SMStruct>();
        public List<SMTypedef> Typedefs = new List<SMTypedef>();
        public string[] TypeStrings = new string[0];
        public List<SMVariable> Variables = new List<SMVariable>();
        public SMFunction currentFunction;

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
                Constants = Constants.Distinct(new SMConstantComparer()).ToList();
                Constants.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            }
            catch (Exception)
            {
                // ignored
            } //racing condition on save when the thread closes first or not..
        }

        public void AppendFiles(IEnumerable<string> paths)
        {
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
                    catch (UnauthorizedAccessException)
                    {
                        // ignored
                    }
                }
            }

            // var editor = Program.MainWindow.GetCurrentEditorElement();

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
            foreach (var e in Enums)
            {
                constantNames.AddRange(e.Entries);
            }

            constantNames.AddRange(Defines.Select(i => i.Name));
            constantNames.AddRange(Variables.Select(v => v.Name));

            if (caret != -1 && currentFunctions != null)
            {
                // TODO: This somewhat works, but somethings when in the end of a function it's buggy and doesnt find
                // the correct function or it finds nothing at all. The addition is a small hack that sometimes works 
                var currentFunc = currentFunctions.FirstOrDefault(e =>
                    e.Index < caret && caret <= e.EndPos);
                if (currentFunc != null)
                {
                    constantNames.AddRange(currentFunc.FuncVariables.Select(v => v.Name));
                    var stringParams = currentFunc.Parameters.Select(e => e.Split('=').First().Trim())
                        .Select(e => e.Split(' ').Last()).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
                    constantNames.AddRange(stringParams);
                    currentVariables.AddRange(stringParams);
                }
                else
                {
                    currentVariables.Clear();
                }
            }

            constantNames.Sort(string.Compare);
            ConstantsStrings = constantNames.ToArray();
            var typeNames = new List<string>
            {
                Capacity = Enums.Count + Structs.Count + Methodmaps.Count + EnumStructs.Count
            };
            typeNames.AddRange(Enums.Select(i => i.Name));
            typeNames.AddRange(Structs.Select(i => i.Name));
            typeNames.AddRange(Methodmaps.Select(i => i.Name));
            typeNames.AddRange(Typedefs.Select(i => i.Name));
            typeNames.AddRange(EnumStructs.Select(i => i.Name));
            typeNames.Sort(string.Compare);
            TypeStrings = typeNames.Where(e => !string.IsNullOrWhiteSpace(e)).ToArray();
        }

        public ACNode[] ProduceACNodes()
        {
            var nodes = new List<ACNode>
            {
                Capacity = Enums.Count + Structs.Count + Constants.Count + Functions.Count + EnumStructs.Count
            };
            nodes.AddRange(ACNode.ConvertFromStringArray(FunctionStrings, true, "▲ "));
            nodes.AddRange(ACNode.ConvertFromStringArray(TypeStrings, false, "♦ "));
            nodes.AddRange(ACNode.ConvertFromStringArray(ConstantsStrings, false, "• "));
            nodes.AddRange(ACNode.ConvertFromStringArray(MethodmapsStrings, false, "↨ "));
            nodes.AddRange(ACNode.ConvertFromStringArray(EnumStructStrings, false, "↩ "));
            //nodes = nodes.Distinct(new ACNodeEqualityComparer()).ToList(); Methodmaps and Functions can and will be the same.
            nodes.Sort((a, b) => string.CompareOrdinal(a.EntryName, b.EntryName));

            return nodes.ToArray();
        }

        public ISNode[] ProduceISNodes()
        {
            var nodes = new List<ISNode>();
            nodes.AddRange(ISNode.ConvertFromStringArray(MethodsStrings, true, "▲ "));
            nodes.AddRange(ISNode.ConvertFromStringArray(StructMethodStrings, true, "▲ "));

            nodes.AddRange(ISNode.ConvertFromStringArray(FieldStrings, false, "• "));
            nodes.AddRange(ISNode.ConvertFromStringArray(StructFieldStrings, false, "• "));

            // nodes.AddRange(ISNode.ConvertFromStringArray(VariableStrings, false, "v "));

            nodes = nodes.Distinct(new ISNodeEqualityComparer()).ToList();
            nodes.Sort((a, b) => string.CompareOrdinal(a.EntryName, b.EntryName));

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
                Variables.AddRange(def.Variables);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public SMDefinition ProduceTemporaryExpandedDefinition(SMDefinition[] definitions, int caret,
            List<SMFunction> currentFunctions)
        {
            var def = new SMDefinition();
            def.MergeDefinitions(this);
            foreach (var definition in definitions)
            {
                if (definition != null)
                {
                    def.MergeDefinitions(definition);
                }
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
        public bool IsExecuteable;
        public string Name;

        public static List<ACNode> ConvertFromStringArray(string[] strings, bool Executable, string prefix = "")
        {
            var nodeList = new List<ACNode>();
            var length = strings.Length;
            for (var i = 0; i < length; ++i)
            {
                nodeList.Add(
                    new ACNode { Name = prefix + strings[i], EntryName = strings[i], IsExecuteable = Executable });
            }

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
            {
                nodeList.Add(
                    new ISNode { Name = prefix + strings[i], EntryName = strings[i], IsExecuteable = Executable });
            }

            return nodeList;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}