// vim set: ts=4 sw=4 tw=99 et:
using System;
using System.Collections.Generic;
using System.IO;
using SourcePawn;

namespace Lysis
{
    public enum Opcode
    {
        LoadLocal,
        StoreLocal,
        LoadLocalRef,
        StoreLocalRef,
        Load,
        Constant,
        StackAddress,
        Store,
        IndexAddress,
        Move,
        PushReg,
        PushConstant,
        Pop,
        Stack,
        Return,
        Jump,
        JumpCondition,
        AddConstant,
        MulConstant,
        ZeroGlobal,
        IncGlobal,
        IncLocal,
        DecLocal,
        IncI,
        IncReg,
        DecI,
        DecReg,
        Fill,
        Bounds,
        SysReq,
        Swap,
        PushStackAddress,
        DebugBreak,
        Goto,
        PushLocal,
        Exchange,
        Binary,
        PushGlobal,
        StoreGlobal,
        LoadGlobal,
        Call,
        EqualConstant,
        LoadIndex,
        Unary,
        StoreGlobalConstant,
        StoreLocalConstant,
        ZeroLocal,
        Heap,
        MemCopy,
        Switch
    }

    public abstract class LInstruction
    {
        private uint pc_;

        public LInstruction()
        {
        }

        public abstract Opcode op
        {
            get;
        }

        public abstract void print(TextWriter tw);

        public virtual bool isControl()
        {
            return false;
        }

        public void setPc(uint pc)
        {
            pc_ = pc;
        }

        public uint pc => pc_;

        public static string RegisterName(Register reg)
        {
            return (reg == Register.Pri) ? "pri" : "alt";
        }
    }


    public abstract class LControlInstruction : LInstruction
    {
        protected LBlock[] successors_;

        public LControlInstruction(params LBlock[] blocks)
        {
            successors_ = blocks;
        }

        public virtual void replaceSuccessor(int i, LBlock block)
        {
            successors_[i] = block;
        }

        public virtual int numSuccessors => successors_.Length;

        public virtual LBlock getSuccessor(int i)
        {
            return successors_[i];
        }

        public override bool isControl()
        {
            return true;
        }
    }

    public abstract class LInstructionReg : LInstruction
    {
        private readonly Register reg_;

        public LInstructionReg(Register reg)
        {
            reg_ = reg;
        }

        public Register reg => reg_;
    }

    public abstract class LInstructionStack : LInstruction
    {
        private readonly int offs_;

        public LInstructionStack(int offset)
        {
            offs_ = offset;
        }

        public int offset => offs_;
    }

    public abstract class LInstructionRegStack : LInstruction
    {
        private readonly Register reg_;
        private readonly int offs_;

        public LInstructionRegStack(Register reg, int offset)
        {
            reg_ = reg;
            offs_ = offset;
        }

        public Register reg => reg_;

        public int offset => offs_;
    }

    public abstract class LInstructionJump : LControlInstruction
    {
        private readonly uint target_offs_;

        public LInstructionJump(uint target_offs, params LBlock[] targets)
            : base(targets)
        {
            target_offs_ = target_offs;
        }

        public uint target_offs => target_offs_;
    }

    public class LLoadLocal : LInstructionRegStack
    {
        public LLoadLocal(int offset, Register reg)
            : base(reg, offset)
        {
        }

        public override Opcode op => Opcode.LoadLocal;

        public override void print(TextWriter tw)
        {
            tw.Write("load.s." + RegisterName(reg) + " " + offset);
        }
    }

    public class LLoadLocalRef : LInstructionRegStack
    {
        public LLoadLocalRef(int offset, Register reg)
            : base(reg, offset)
        {
        }

        public override Opcode op => Opcode.LoadLocalRef;

        public override void print(TextWriter tw)
        {
            tw.Write("lref.s." + RegisterName(reg) + " " + offset);
        }
    }

    public class LStoreLocal : LInstructionRegStack
    {
        public LStoreLocal(Register reg, int offset)
            : base(reg, offset)
        {
        }

        public override Opcode op => Opcode.StoreLocal;

        public override void print(TextWriter tw)
        {
            tw.Write("stor.s." + RegisterName(reg) + " " + offset);
        }
    }

    public class LStoreLocalRef : LInstructionRegStack
    {
        public LStoreLocalRef(Register reg, int offset)
            : base(reg, offset)
        {
        }

        public override Opcode op => Opcode.StoreLocalRef;

        public override void print(TextWriter tw)
        {
            tw.Write("sref.s." + RegisterName(reg) + " " + offset);
        }
    }

    public class LLoad : LInstruction
    {
        private readonly int bytes_;

        public LLoad(int bytes)
        {
            bytes_ = bytes;
        }

        private int bytes => bytes_;

        public override Opcode op => Opcode.Load;

        public override void print(TextWriter tw)
        {
            tw.Write("load.i." + bytes + "   ; pri = [pri]");
        }
    }

    public class LConstant : LInstructionReg
    {
        private readonly int val_;

        public LConstant(int val, Register reg) : base(reg)
        {
            val_ = val;
        }

        public int val => val_;

        public override Opcode op => Opcode.Constant;

        public override void print(TextWriter tw)
        {
            tw.Write("const." + RegisterName(reg) + " " + val);
        }
    }


    public class LStackAddress : LInstructionRegStack
    {
        public LStackAddress(int offset, Register reg)
            : base(reg, offset)
        {
        }

        public override Opcode op => Opcode.StackAddress;

        public override void print(TextWriter tw)
        {
            tw.Write("addr." + RegisterName(reg) + " " + offset);
        }
    }

    public class LStore : LInstruction
    {
        private readonly int bytes_;

        public LStore(int bytes)
        {
            bytes_ = bytes;
        }

        public int bytes => bytes_;

        public override Opcode op => Opcode.Store;

        public override void print(TextWriter tw)
        {
            tw.Write("stor.i." + bytes + "   ; [alt] = pri");
        }
    }

    public class LIndexAddress : LInstruction
    {
        private readonly int shift_;

        public LIndexAddress(int shift)
        {
            shift_ = shift;
        }

        public int shift => shift_;

        public override Opcode op => Opcode.IndexAddress;

        public override void print(TextWriter tw)
        {
            tw.Write("idxaddr " + shift + " ; pri=alt+(pri<<" + shift + ")");
        }
    }

    public class LMove : LInstructionReg
    {
        public LMove(Register reg)
            : base(reg)
        {
        }

        public override Opcode op => Opcode.Move;

        public override void print(TextWriter tw)
        {
            tw.Write("move." + RegisterName(reg), ", " +
                (reg == Register.Pri ? RegisterName(Register.Alt) : RegisterName(Register.Pri)));
        }
    }

    public class LPushReg : LInstructionReg
    {
        public LPushReg(Register reg)
            : base(reg)
        {
        }

        public override Opcode op => Opcode.PushReg;

        public override void print(TextWriter tw)
        {
            tw.Write("push." + RegisterName(reg));
        }
    }

    public class LIncReg : LInstructionReg
    {
        public LIncReg(Register reg)
            : base(reg)
        {
        }

        public override Opcode op => Opcode.IncReg;

        public override void print(TextWriter tw)
        {
            tw.Write("inc." + RegisterName(reg));
        }
    }

    public class LDecReg : LInstructionReg
    {
        public LDecReg(Register reg)
            : base(reg)
        {
        }

        public override Opcode op => Opcode.DecReg;

        public override void print(TextWriter tw)
        {
            tw.Write("dec." + RegisterName(reg));
        }
    }

    public class LPushConstant : LInstruction
    {
        private readonly int val_;

        public LPushConstant(int val)
        {
            val_ = val;
        }

        public int val => val_;

        public override Opcode op => Opcode.PushConstant;

        public override void print(TextWriter tw)
        {
            tw.Write("push.c " + val);
        }
    }

    public class LPop : LInstructionReg
    {
        public LPop(Register reg)
            : base(reg)
        {
        }

        public override Opcode op => Opcode.Pop;

        public override void print(TextWriter tw)
        {
            tw.Write("pop." + RegisterName(reg));
        }
    }

    public class LStack : LInstruction
    {
        private readonly int val_;

        public LStack(int val)
        {
            val_ = val;
        }

        public int amount => val_;

        public override Opcode op => Opcode.Stack;

        public override void print(TextWriter tw)
        {
            tw.Write("stack " + amount);
        }
    }

    public class LReturn : LControlInstruction
    {
        public LReturn() : base()
        {
        }

        public override Opcode op => Opcode.Return;

        public override void print(TextWriter tw)
        {
            tw.Write("return");
        }
    }

    public class LGoto : LControlInstruction
    {
        public LGoto(LBlock target)
            : base(target)
        {
        }

        public LBlock target => getSuccessor(0);

        public override Opcode op => Opcode.Goto;

        public override void print(TextWriter tw)
        {
            tw.Write("goto block" + target.id);
        }
    }

    public class LJump : LInstructionJump
    {
        public LJump(LBlock target, uint target_offs)
            : base(target_offs, target)
        {
        }

        public override Opcode op => Opcode.Jump;

        public LBlock target => getSuccessor(0);

        public override void print(TextWriter tw)
        {
            tw.Write("jump block" + target.id);
        }
    }

    public class LJumpCondition : LInstructionJump
    {
        private readonly SPOpcode op_;

        public LJumpCondition(SPOpcode op, LBlock true_target, LBlock false_target, uint target_offs)
            : base(target_offs, true_target, false_target)
        {
            op_ = op;
        }

        public override Opcode op => Opcode.JumpCondition;

        public SPOpcode spop => op_;
        public LBlock trueTarget => getSuccessor(0);
        public LBlock falseTarget => getSuccessor(1);

        public override void print(TextWriter tw)
        {
            switch (op_)
            {
                case SPOpcode.jnz:
                    tw.Write("jnz ");
                    break;
                case SPOpcode.jzer:
                    tw.Write("jzero ");
                    break;
                case SPOpcode.jsgeq:
                    tw.Write("jsgeq ");
                    break;
                case SPOpcode.jsgrtr:
                    tw.Write("jsgrtr ");
                    break;
                case SPOpcode.jsleq:
                    tw.Write("jsleq ");
                    break;
                case SPOpcode.jsless:
                    tw.Write("jsless ");
                    break;
                case SPOpcode.jeq:
                    tw.Write("jeq ");
                    break;
                case SPOpcode.jneq:
                    tw.Write("jneq ");
                    break;
                default:
                    throw new Exception("unrecognized spop");
            }
            tw.Write("block" + trueTarget.id + " (block" + falseTarget.id + ")");
        }
    }

    public class LAddConstant : LInstruction
    {
        private readonly int amount_;

        public LAddConstant(int amount)
        {
            amount_ = amount;
        }

        public int amount => amount_;

        public override Opcode op => Opcode.AddConstant;

        public override void print(TextWriter tw)
        {
            tw.Write("add.pri " + amount);
        }
    }

    public class LMulConstant : LInstruction
    {
        private readonly int amount_;

        public LMulConstant(int amount)
        {
            amount_ = amount;
        }

        public int amount => amount_;

        public override Opcode op => Opcode.MulConstant;

        public override void print(TextWriter tw)
        {
            tw.Write("mul.pri " + amount);
        }
    }

    public class LZeroGlobal : LInstruction
    {
        private readonly int address_;

        public LZeroGlobal(int address)
        {
            address_ = address;
        }

        public int address => address_;

        public override Opcode op => Opcode.ZeroGlobal;
        public override void print(TextWriter tw)
        {
            tw.Write("zero " + address);
        }
    }

    public class LZeroLocal : LInstruction
    {
        private readonly int address_;

        public LZeroLocal(int address)
        {
            address_ = address;
        }

        public int address => address_;

        public override Opcode op => Opcode.ZeroLocal;
        public override void print(TextWriter tw)
        {
            tw.Write("zero.s " + address);
        }
    }

    public class LIncGlobal : LInstruction
    {
        private readonly int address_;

        public LIncGlobal(int address)
        {
            address_ = address;
        }

        public int address => address_;

        public override Opcode op => Opcode.IncGlobal;
        public override void print(TextWriter tw)
        {
            tw.Write("inc " + address);
        }
    }

    public class LIncLocal : LInstructionStack
    {
        public LIncLocal(int offset)
            : base(offset)
        {
        }

        public override Opcode op => Opcode.IncLocal;

        public override void print(TextWriter tw)
        {
            tw.Write("inc.s " + offset);
        }
    }

    public class LDecLocal : LInstructionStack
    {
        public LDecLocal(int offset)
            : base(offset)
        {
        }

        public override Opcode op => Opcode.DecLocal;

        public override void print(TextWriter tw)
        {
            tw.Write("dec.s " + offset);
        }
    }

    public class LIncI : LInstruction
    {
        public LIncI()
        {
        }

        public override Opcode op => Opcode.IncI;

        public override void print(TextWriter tw)
        {
            tw.Write("inc.i    ; [pri] = [pri] + 1");
        }
    }

    public class LDecI : LInstruction
    {
        public LDecI()
        {
        }

        public override Opcode op => Opcode.DecI;

        public override void print(TextWriter tw)
        {
            tw.Write("dec.i    ; [pri] = [pri] + 1");
        }
    }

    public class LFill : LInstruction
    {
        private readonly int amount_;

        public LFill(int amount)
        {
            amount_ = amount;
        }

        public int amount => amount_;

        public override Opcode op => Opcode.Fill;

        public override void print(TextWriter tw)
        {
            tw.Write("fill alt, " + amount_);
        }
    }

    public class LBounds : LInstruction
    {
        private readonly int amount_;

        public LBounds(int amount)
        {
            amount_ = amount;
        }

        public int amount => amount_;

        public override Opcode op => Opcode.Bounds;

        public override void print(TextWriter tw)
        {
            tw.Write("bounds.pri " + amount_);
        }
    }

    public class LSysReq : LInstruction
    {
        private readonly Native native_;

        public LSysReq(Native native)
        {
            native_ = native;
        }

        public override Opcode op => Opcode.SysReq;

        public Native native => native_;

        public override void print(TextWriter tw)
        {
            tw.Write("sysreq " + native.name);
        }
    }

    public class LSwap : LInstructionReg
    {
        public LSwap(Register reg)
            : base(reg)
        {
        }

        public override Opcode op => Opcode.Swap;

        public override void print(TextWriter tw)
        {
            tw.Write("swap." + RegisterName(reg));
        }
    }

    public class LPushStackAddress : LInstructionStack
    {
        public LPushStackAddress(int offset)
            : base(offset)
        {
        }

        public override Opcode op => Opcode.PushStackAddress;

        public override void print(TextWriter tw)
        {
            tw.Write("push.adr " + offset);
        }
    }

    public class LDebugBreak : LInstruction
    {
        public LDebugBreak()
        {
        }

        public override Opcode op => Opcode.DebugBreak;

        public override void print(TextWriter tw)
        {
            tw.Write("break");
        }
    }

    public class LPushLocal : LInstruction
    {
        private readonly int offset_;

        public LPushLocal(int offset)
        {
            offset_ = offset;
        }

        public int offset => offset_;
        public override Opcode op => Opcode.PushLocal;
        public override void print(TextWriter tw)
        {
            tw.Write("push.s " + offset);
        }
    }

    public class LExchange : LInstruction
    {
        public LExchange()
        {
        }

        public override Opcode op => Opcode.Exchange;
        public override void print(TextWriter tw)
        {
            tw.Write("xchg");
        }
    }

    public class LUnary : LInstruction
    {
        private readonly SPOpcode spop_;
        private readonly Register reg_;

        public LUnary(SPOpcode op, Register reg)
        {
            spop_ = op;
            reg_ = reg;
        }

        public SPOpcode spop => spop_;
        public override Opcode op => Opcode.Unary;
        public Register reg => reg_;
        public override void print(TextWriter tw)
        {
            switch (spop)
            {
                case SPOpcode.not:
                    tw.Write("not");
                    break;
                case SPOpcode.invert:
                    tw.Write("invert");
                    break;

                default:
                    throw new Exception("unexpected op");
            }
        }
    }

    public class LBinary : LInstruction
    {
        private readonly SPOpcode spop_;
        private readonly Register lhs_;
        private readonly Register rhs_;

        public LBinary(SPOpcode op, Register lhs, Register rhs)
        {
            spop_ = op;
            lhs_ = lhs;
            rhs_ = rhs;
        }

        public SPOpcode spop => spop_;
        public override Opcode op => Opcode.Binary;
        public Register lhs => lhs_;
        public Register rhs => rhs_;
        public override void print(TextWriter tw)
        {
            switch (spop)
            {
                case SPOpcode.add:
                    tw.Write("add");
                    break;
                case SPOpcode.sub:
                    tw.Write("sub");
                    break;
                case SPOpcode.eq:
                    tw.Write("eq");
                    break;
                case SPOpcode.neq:
                    tw.Write("neq");
                    break;
                case SPOpcode.sleq:
                    tw.Write("sleq");
                    break;
                case SPOpcode.sgeq:
                    tw.Write("sgeq");
                    break;
                case SPOpcode.sgrtr:
                    tw.Write("sgrtr");
                    break;
                case SPOpcode.and:
                    tw.Write("and");
                    break;
                case SPOpcode.or:
                    tw.Write("or");
                    break;
                case SPOpcode.smul:
                    tw.Write("smul");
                    break;
                case SPOpcode.sdiv_alt:
                    tw.Write("sdiv.alt");
                    break;
                case SPOpcode.shr:
                    tw.Write("shr");
                    break;
                case SPOpcode.shl:
                    tw.Write("shl");
                    break;
                case SPOpcode.sub_alt:
                    tw.Write("sub.alt");
                    break;
                case SPOpcode.sless:
                    tw.Write("sless");
                    break;
                case SPOpcode.xor:
                    tw.Write("xor");
                    break;
                case SPOpcode.sshr:
                    tw.Write("sshr");
                    break;

                default:
                    throw new Exception("unexpected op");
            }
        }
    }

    public class LPushGlobal : LInstruction
    {
        private readonly int address_;

        public LPushGlobal(int address)
        {
            address_ = address;
        }

        public int address => address_;
        public override Opcode op => Opcode.PushGlobal;
        public override void print(TextWriter tw)
        {
            tw.Write("push " + address);
        }
    }

    public class LStoreGlobal : LInstructionReg
    {
        private readonly int address_;

        public LStoreGlobal(int address, Register reg)
            : base(reg)
        {
            address_ = address;
        }

        public int address => address_;
        public override Opcode op => Opcode.StoreGlobal;
        public override void print(TextWriter tw)
        {
            tw.Write("stor." + RegisterName(reg) + " " + address);
        }
    }

    public class LLoadGlobal : LInstructionReg
    {
        private readonly int address_;

        public LLoadGlobal(int address, Register reg)
            : base(reg)
        {
            address_ = address;
        }

        public int address => address_;
        public override Opcode op => Opcode.LoadGlobal;
        public override void print(TextWriter tw)
        {
            tw.Write("load." + RegisterName(reg) + " " + address);
        }
    }

    public class LCall : LInstruction
    {
        private readonly int address_;

        public LCall(int address)
        {
            address_ = address;
        }

        public int address => address_;
        public override Opcode op => Opcode.Call;
        public override void print(TextWriter tw)
        {
            tw.Write("call");
        }
    }

    public class LEqualConstant : LInstructionReg
    {
        private readonly int value_;

        public LEqualConstant(Register reg, int value)
            : base(reg)
        {
            value_ = value;
        }

        public int value => value_;
        public override Opcode op => Opcode.EqualConstant;
        public override void print(TextWriter tw)
        {
            tw.Write("eq.c." + RegisterName(reg) + " " + value);
        }
    }

    public class LLoadIndex : LInstruction
    {
        private readonly int shift_;

        public LLoadIndex(int shift)
        {
            shift_ = shift;
        }

        public int shift => shift_;
        public override Opcode op => Opcode.LoadIndex;
        public override void print(TextWriter tw)
        {
            tw.Write("lidx." + shift + " ; [pri=alt+(pri<<" + shift + ")]");
        }
    }

    public class LStoreGlobalConstant : LInstruction
    {
        private readonly int address_;
        private readonly int value_;

        public LStoreGlobalConstant(int address, int value)
        {
            address_ = address;
            value_ = value;
        }

        public int address => address_;
        public int value => value_;
        public override Opcode op => Opcode.StoreGlobalConstant;
        public override void print(TextWriter tw)
        {
            tw.Write("const [" + address + "]" + " = value");
        }
    }

    public class LStoreLocalConstant : LInstruction
    {
        private readonly int address_;
        private readonly int value_;

        public LStoreLocalConstant(int address, int value)
        {
            address_ = address;
            value_ = value;
        }

        public int address => address_;
        public int value => value_;
        public override Opcode op => Opcode.StoreLocalConstant;
        public override void print(TextWriter tw)
        {
            tw.Write("const.s [" + address + "]" + " = value");
        }
    }

    public class LHeap : LInstruction
    {
        private readonly int amount_;

        public LHeap(int amount)
        {
            amount_ = amount;
        }

        public int amount => amount_;
        public override Opcode op => Opcode.Heap;
        public override void print(TextWriter tw)
        {
            tw.Write("heap " + amount);
        }
    }

    public class LMemCopy : LInstruction
    {
        private readonly int bytes_;

        public LMemCopy(int bytes)
        {
            bytes_ = bytes;
        }

        public int bytes => bytes_;
        public override Opcode op => Opcode.MemCopy;
        public override void print(TextWriter tw)
        {
            tw.Write("movs " + bytes);
        }
    }

    public class SwitchCase
    {
        private readonly int value_;
        public LBlock target;

        public SwitchCase(int value, LBlock target)
        {
            value_ = value;
            this.target = target;
        }

        public int value => value_;
    }

    public class LSwitch : LControlInstruction
    {
        private LBlock defaultCase_;
        private readonly List<SwitchCase> cases_;

        public LSwitch(LBlock defaultCase, List<SwitchCase> cases)
        {
            defaultCase_ = defaultCase;
            cases_ = cases;
        }
        public LBlock defaultCase => defaultCase_;
        public override Opcode op => Opcode.Switch;
        public override void replaceSuccessor(int i, LBlock block)
        {
            if (i == 0)
            {
                defaultCase_ = block;
            }

            if (cases_.Count >= i && i > 0)
            {
                cases_[i - 1].target = block;
            }
        }
        public override int numSuccessors => cases_.Count + 1;
        public override LBlock getSuccessor(int i)
        {
            if (i == 0)
            {
                return defaultCase_;
            }

            return cases_[i - 1].target;
        }
        public int numCases => cases_.Count;
        public SwitchCase getCase(int i)
        {
            return cases_[i];
        }
        public override void print(TextWriter tw)
        {
            var text = defaultCase.id + (numCases > 0 ? "," : "");
            for (var i = 0; i < numCases; i++)
            {
                text += getCase(i).target.id;
                if (i != numCases - 1)
                {
                    text += ",";
                }
            }
            tw.Write("switch.pri -> " + text);
        }
    }
}
