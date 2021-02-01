using System.Collections.Generic;
using System.Linq;
using SourcePawn;

namespace Lysis
{
    public class AbstractStack
    {
        private class StackEntry
        {
            public DDeclareLocal declaration;
            public DNode assignment;

            public StackEntry(DDeclareLocal decl, DNode assn)
            {
                declaration = decl;
                assignment = assn;
            }
        }

        private readonly List<StackEntry> stack_;
        private readonly StackEntry[] args_;
        private DNode pri_;
        private DNode alt_;

        public AbstractStack(int nargs)
        {
            stack_ = new List<StackEntry>();
            args_ = new StackEntry[nargs];
            for (var i = 0; i < args_.Length; i++)
            {
                args_[i] = new StackEntry(null, null);
            }
        }

        public AbstractStack(AbstractStack other)
        {
            stack_ = new List<StackEntry>();
            for (var i = 0; i < other.stack_.Count; i++)
            {
                stack_.Add(new StackEntry(other.stack_[i].declaration, other.stack_[i].assignment));
            }

            args_ = new StackEntry[other.args_.Length];
            for (var i = 0; i < args_.Length; i++)
            {
                args_[i] = new StackEntry(other.args_[i].declaration, other.args_[i].assignment);
            }

            pri_ = other.pri_;
            alt_ = other.alt_;
        }

        public void push(DDeclareLocal local)
        {
            stack_.Add(new StackEntry(local, local.value));
            local.setOffset(depth);
        }
        private StackEntry popEntry()
        {
            var e = stack_[stack_.Count - 1];
            stack_.RemoveRange(stack_.Count - 1, 1);
            return e;
        }
        public void pop()
        {
            popEntry();
        }
        public DNode popAsTemp()
        {
            var entry = popEntry();
            if (entry.declaration.uses.Count == 0)
            {
                return entry.assignment;
            }
            //Debug.Assert(false, "not yet handled");
            return null;
        }
        public DNode popName()
        {
            DNode value = stack_[stack_.Count - 1].declaration;
            pop();
            return value;
        }
        public DNode popValue()
        {
            var value = stack_[stack_.Count - 1].assignment;
            pop();
            return value;
        }

        private StackEntry entry(int offset)
        {
            if (offset < 0)
            {
                return stack_.ElementAt((-offset / 4) - 1);
            }

            return args_[(offset - 12) / 4];
        }
        public DDeclareLocal getName(int offset)
        {
            return entry(offset).declaration;
        }

        public int nargs => args_.Length;
        public int depth => -(stack_.Count * 4);
        public DNode pri => pri_;
        public DNode alt => alt_;
        public DNode reg(Register reg)
        {
            return (reg == Register.Pri) ? pri_ : alt_;
        }
        public void set(Register reg, DNode node)
        {
            if (reg == Register.Pri)
            {
                pri_ = node;
            }
            else
            {
                alt_ = node;
            }
        }
        public void set(int offset, DNode value)
        {
            entry(offset).assignment = value;
        }
        public void init(int offset, DDeclareLocal local)
        {
            entry(offset).declaration = local;
            entry(offset).assignment = null;
        }
    }

    public class NodeBlock
    {
        private readonly LBlock lir_;
        private AbstractStack stack_;
        private readonly NodeList nodes_;

        public NodeBlock(LBlock lir)
        {
            lir_ = lir;
            nodes_ = new NodeList();
        }

        private void joinRegs(Register reg, DNode value)
        {
            if (value == null || stack_.reg(reg) == value)
            {
                return;
            }

            if (stack_.reg(reg) == null)
            {
                stack_.set(reg, value);
                return;
            }

            DPhi phi;
            var node = stack_.reg(reg);
            if (node.type != NodeType.Phi || node.block != this)
            {
                phi = new DPhi(node);
                stack_.set(reg, phi);
                add(phi);
            }
            else
            {
                phi = (DPhi)node;
            }
            phi.addInput(value);
        }

        public void inherit(LGraph graph, NodeBlock other)
        {
            if (other == null)
            {
                stack_ = new AbstractStack(graph.nargs);
                for (var i = 0; i < graph.nargs; i++)
                {
                    var local = new DDeclareLocal(lir_.pc, null);
                    local.setOffset((i * 4) + 12);
                    add(local);
                    stack_.init((i * 4) + 12, local);
                }
            }
            else if (stack_ == null)
            {
                //Debug.Assert(other.stack_ != null);
                stack_ = new AbstractStack(other.stack_);
            }
            else
            {
                // Right now we only create phis for pri/alt.
                joinRegs(Register.Pri, other.stack_.pri);
                joinRegs(Register.Alt, other.stack_.alt);
            }
        }

        public void add(DNode node)
        {
            node.setBlock(this);
            nodes_.add(node);
        }
        public void prepend(DNode node)
        {
            node.setBlock(this);
            nodes_.insertBefore(nodes_.last, node);
        }
        public void replace(NodeList.iterator_base where, DNode with)
        {
            with.setBlock(this);
            nodes_.replace(where, with);
        }
        public void replace(DNode where, DNode with)
        {
            with.setBlock(this);
            nodes_.replace(where, with);
        }

        public LBlock lir => lir_;
        public AbstractStack stack => stack_;
        public NodeList nodes => nodes_;
    }

    public class NodeGraph
    {
        private readonly SourcePawnFile file_;
        private readonly NodeBlock[] blocks_;
        private int nameCounter_;
        private readonly Function function_;

        public NodeGraph(SourcePawnFile file, NodeBlock[] blocks)
        {
            file_ = file;
            blocks_ = blocks;
            nameCounter_ = 0;
            function_ = file_.lookupFunction(blocks[0].lir.pc);
        }
        public NodeBlock this[int i] => blocks_[i];
        public SourcePawnFile file => file_;
        public Function function => function_;
        public int numBlocks => blocks_.Length;
        public string tempName()
        {
            return "var" + ++nameCounter_;
        }
    }
}
