using System;
using System.Collections.Generic;
using SourcePawn;

namespace Lysis
{
    public class NodeBuilder
    {
        private readonly SourcePawnFile file_;
        private readonly LGraph graph_;
        private readonly NodeBlock[] blocks_;

        public NodeBuilder(SourcePawnFile file, LGraph graph)
        {
            file_ = file;
            graph_ = graph;
            blocks_ = new NodeBlock[graph_.blocks.Length];
            for (var i = 0; i < graph_.blocks.Length; i++)
            {
                blocks_[i] = new NodeBlock(graph_.blocks[i]);
            }
        }

        public void traverse(NodeBlock block)
        {
            for (var i = 0; i < block.lir.numPredecessors; i++)
            {
                var pred = blocks_[block.lir.getPredecessor(i).id];

                // Don't bother with backedges yet.
                if (pred.lir.id >= block.lir.id)
                {
                    continue;
                }

                block.inherit(graph_, pred);
            }

            foreach (var uins in block.lir.instructions)
            {
                // Attempt to find static declarations. This is really
                // expensive - we could cheapen it by creating per-method
                // lists of statics.
                {
                    var i = -1;
                    do
                    {
                        var var = file_.lookupDeclarations(uins.pc, ref i, Scope.Static);
                        if (var == null)
                        {
                            break;
                        }

                        block.add(new DDeclareStatic(var));
                    } while (true);
                }

                switch (uins.op)
                {
                    case Opcode.DebugBreak:
                        break;

                    case Opcode.Stack:
                        {
                            var ins = (LStack)uins;
                            if (ins.amount < 0)
                            {
                                for (var i = 0; i < -ins.amount / 4; i++)
                                {
                                    var local = new DDeclareLocal(ins.pc, null);
                                    block.stack.push(local);
                                    block.add(local);
                                }
                            }
                            else
                            {
                                for (var i = 0; i < ins.amount / 4; i++)
                                {
                                    block.stack.pop();
                                }
                            }
                            break;
                        }

                    case Opcode.Fill:
                        {
                            var ins = (LFill)uins;
                            var node = block.stack.alt;
                            var local = (DDeclareLocal)node;
                            //Debug.Assert(block.stack.pri.type == NodeType.Constant);
                            for (var i = 0; i < ins.amount; i += 4)
                            {
                                block.stack.set(local.offset + i, block.stack.pri);
                            }

                            break;
                        }

                    case Opcode.Constant:
                        {
                            var ins = (LConstant)uins;
                            var v = new DConstant(ins.val, ins.pc);
                            block.stack.set(ins.reg, v);
                            block.add(v);
                            break;
                        }

                    case Opcode.PushConstant:
                        {
                            var ins = (LPushConstant)uins;
                            var v = new DConstant(ins.val);
                            var local = new DDeclareLocal(ins.pc, v);
                            block.stack.push(local);
                            block.add(v);
                            block.add(local);
                            break;
                        }

                    case Opcode.PushReg:
                        {
                            var ins = (LPushReg)uins;
                            var local = new DDeclareLocal(ins.pc, block.stack.reg(ins.reg));
                            block.stack.push(local);
                            block.add(local);
                            break;
                        }

                    case Opcode.Pop:
                        {
                            var ins = (LPop)uins;
                            var node = block.stack.popAsTemp();
                            block.stack.set(ins.reg, node);
                            break;
                        }

                    case Opcode.StackAddress:
                        {
                            var ins = (LStackAddress)uins;
                            var local = block.stack.getName(ins.offset);
                            block.stack.set(ins.reg, local);
                            break;
                        }

                    case Opcode.PushStackAddress:
                        {
                            var ins = (LPushStackAddress)uins;
                            var lref = new DLocalRef(block.stack.getName(ins.offset));
                            var local = new DDeclareLocal(ins.pc, lref);
                            block.stack.push(local);
                            block.add(lref);
                            block.add(local);
                            break;
                        }

                    case Opcode.Goto:
                        {
                            var ins = (LGoto)uins;
                            var node = new DJump(blocks_[ins.target.id]);
                            block.add(node);
                            break;
                        }

                    case Opcode.Jump:
                        {
                            var ins = (LJump)uins;
                            var node = new DJump(blocks_[ins.target.id]);
                            block.add(node);
                            break;
                        }

                    case Opcode.JumpCondition:
                        {
                            var ins = (LJumpCondition)uins;
                            var lhtarget = blocks_[ins.trueTarget.id];
                            var rhtarget = blocks_[ins.falseTarget.id];
                            var cmp = block.stack.pri;
                            var jmp = ins.spop;
                            if (jmp != SPOpcode.jzer && jmp != SPOpcode.jnz)
                            {
                                SPOpcode newop;
                                switch (ins.spop)
                                {
                                    case SPOpcode.jeq:
                                        newop = SPOpcode.neq;
                                        jmp = SPOpcode.jzer;
                                        break;
                                    case SPOpcode.jneq:
                                        newop = SPOpcode.eq;
                                        jmp = SPOpcode.jzer;
                                        break;
                                    case SPOpcode.jsgeq:
                                        newop = SPOpcode.sless;
                                        jmp = SPOpcode.jzer;
                                        break;
                                    case SPOpcode.jsgrtr:
                                        newop = SPOpcode.sleq;
                                        jmp = SPOpcode.jzer;
                                        break;
                                    case SPOpcode.jsleq:
                                        newop = SPOpcode.sgrtr;
                                        jmp = SPOpcode.jzer;
                                        break;
                                    case SPOpcode.jsless:
                                        newop = SPOpcode.sgeq;
                                        jmp = SPOpcode.jzer;
                                        break;
                                    default:
                                        //Debug.Assert(false);
                                        return;
                                }
                                cmp = new DBinary(newop, block.stack.pri, block.stack.alt);
                                block.add(cmp);
                            }
                            var jcc = new DJumpCondition(jmp, cmp, lhtarget, rhtarget);
                            block.add(jcc);
                            break;
                        }

                    case Opcode.LoadLocal:
                        {
                            var ins = (LLoadLocal)uins;
                            var load = new DLoad(block.stack.getName(ins.offset));
                            block.stack.set(ins.reg, load);
                            block.add(load);
                            break;
                        }

                    case Opcode.LoadLocalRef:
                        {
                            var ins = (LLoadLocalRef)uins;
                            var load = new DLoad(block.stack.getName(ins.offset));
                            load = new DLoad(load);
                            block.stack.set(ins.reg, load);
                            block.add(load);
                            break;
                        }

                    case Opcode.StoreLocal:
                        {
                            var ins = (LStoreLocal)uins;
                            var store = new DStore(block.stack.getName(ins.offset), block.stack.reg(ins.reg));
                            block.add(store);
                            break;
                        }

                    case Opcode.StoreLocalRef:
                        {
                            var ins = (LStoreLocalRef)uins;
                            var load = new DLoad(block.stack.getName(ins.offset));
                            var store = new DStore(load, block.stack.reg(ins.reg));
                            block.add(store);
                            break;
                        }

                    case Opcode.SysReq:
                        {
                            var sysreq = (LSysReq)uins;
                            var ins = (DConstant)block.stack.popValue();
                            var arguments = new List<DNode>();
                            for (var i = 0; i < ins.value; i++)
                            {
                                arguments.Add(block.stack.popName());
                            }

                            var call = new DSysReq(sysreq.native, arguments.ToArray());
                            block.stack.set(Register.Pri, call);
                            block.add(call);
                            break;
                        }

                    case Opcode.AddConstant:
                        {
                            var ins = (LAddConstant)uins;
                            var val = new DConstant(ins.amount);
                            var node = new DBinary(SPOpcode.add, block.stack.pri, val);
                            block.stack.set(Register.Pri, node);
                            block.add(val);
                            block.add(node);
                            break;
                        }

                    case Opcode.MulConstant:
                        {
                            var ins = (LMulConstant)uins;
                            var val = new DConstant(ins.amount);
                            var node = new DBinary(SPOpcode.smul, block.stack.pri, val);
                            block.stack.set(Register.Pri, node);
                            block.add(val);
                            block.add(node);
                            break;
                        }

                    case Opcode.Bounds:
                        {
                            var ins = (LBounds)uins;
                            var node = new DBoundsCheck(block.stack.pri);
                            block.add(node);
                            break;
                        }

                    case Opcode.IndexAddress:
                        {
                            var ins = (LIndexAddress)uins;
                            var node = new DArrayRef(block.stack.alt, block.stack.pri, ins.shift);
                            block.stack.set(Register.Pri, node);
                            block.add(node);
                            break;
                        }

                    case Opcode.Move:
                        {
                            var ins = (LMove)uins;
                            if (ins.reg == Register.Pri)
                            {
                                block.stack.set(Register.Pri, block.stack.alt);
                            }
                            else
                            {
                                block.stack.set(Register.Alt, block.stack.pri);
                            }

                            break;
                        }

                    case Opcode.Store:
                        {
                            var ins = (LStore)uins;
                            var store = new DStore(block.stack.alt, block.stack.pri);
                            block.add(store);
                            break;
                        }

                    case Opcode.Load:
                        {
                            var ins = (LLoad)uins;
                            var load = new DLoad(block.stack.pri);
                            block.stack.set(Register.Pri, load);
                            block.add(load);
                            break;
                        }

                    case Opcode.Swap:
                        {
                            var ins = (LSwap)uins;
                            var lhs = block.stack.popAsTemp();
                            var rhs = block.stack.reg(ins.reg);
                            var local = new DDeclareLocal(ins.pc, rhs);
                            block.stack.set(ins.reg, lhs);
                            block.stack.push(local);
                            block.add(local);
                            break;
                        }

                    case Opcode.IncI:
                        {
                            var inc = new DIncDec(block.stack.pri, 1);
                            block.add(inc);
                            break;
                        }

                    case Opcode.DecI:
                        {
                            var dec = new DIncDec(block.stack.pri, -1);
                            block.add(dec);
                            break;
                        }

                    case Opcode.IncLocal:
                        {
                            var ins = (LIncLocal)uins;
                            var local = block.stack.getName(ins.offset);
                            var inc = new DIncDec(local, 1);
                            block.add(inc);
                            break;
                        }

                    case Opcode.IncReg:
                        {
                            var ins = (LIncReg)uins;
                            var dec = new DIncDec(block.stack.reg(ins.reg), 1);
                            block.add(dec);
                            break;
                        }

                    case Opcode.DecLocal:
                        {
                            var ins = (LDecLocal)uins;
                            var local = block.stack.getName(ins.offset);
                            var dec = new DIncDec(local, -1);
                            block.add(dec);
                            break;
                        }

                    case Opcode.DecReg:
                        {
                            var ins = (LDecReg)uins;
                            var dec = new DIncDec(block.stack.reg(ins.reg), -1);
                            block.add(dec);
                            break;
                        }

                    case Opcode.Return:
                        {
                            var node = new DReturn(block.stack.pri);
                            block.add(node);
                            break;
                        }

                    case Opcode.PushLocal:
                        {
                            var ins = (LPushLocal)uins;
                            var load = new DLoad(block.stack.getName(ins.offset));
                            var local = new DDeclareLocal(ins.pc, load);
                            block.stack.push(local);
                            block.add(load);
                            block.add(local);
                            break;
                        }

                    case Opcode.Exchange:
                        {
                            var node = block.stack.alt;
                            block.stack.set(Register.Alt, block.stack.pri);
                            block.stack.set(Register.Pri, node);
                            break;
                        }

                    case Opcode.Unary:
                        {
                            var ins = (LUnary)uins;
                            var unary = new DUnary(ins.spop, block.stack.reg(ins.reg));
                            block.stack.set(Register.Pri, unary);
                            block.add(unary);
                            break;
                        }

                    case Opcode.Binary:
                        {
                            var ins = (LBinary)uins;
                            var binary = new DBinary(ins.spop, block.stack.reg(ins.lhs), block.stack.reg(ins.rhs));
                            block.stack.set(Register.Pri, binary);
                            block.add(binary);
                            break;
                        }

                    case Opcode.PushGlobal:
                        {
                            var ins = (LPushGlobal)uins;
                            var global = file_.lookupGlobal(ins.address);
                            if (global == null)
                            {
                                global = file_.lookupVariable(ins.pc, ins.address, Scope.Static);
                            }

                            var dglobal = new DGlobal(global);
                            DNode node = new DLoad(dglobal);
                            var local = new DDeclareLocal(ins.pc, node);
                            block.stack.push(local);
                            block.add(dglobal);
                            block.add(node);
                            block.add(local);
                            break;
                        }

                    case Opcode.LoadGlobal:
                        {
                            var ins = (LLoadGlobal)uins;
                            var global = file_.lookupGlobal(ins.address);
                            if (global == null)
                            {
                                global = file_.lookupVariable(ins.pc, ins.address, Scope.Static);
                            }

                            DNode dglobal = new DGlobal(global);
                            DNode node = new DLoad(dglobal);
                            block.stack.set(ins.reg, node);
                            block.add(dglobal);
                            block.add(node);
                            break;
                        }

                    case Opcode.StoreGlobal:
                        {
                            var ins = (LStoreGlobal)uins;
                            var global = file_.lookupGlobal(ins.address);
                            if (global == null)
                            {
                                global = file_.lookupVariable(ins.pc, ins.address, Scope.Static);
                            }

                            var node = new DGlobal(global);
                            var store = new DStore(node, block.stack.reg(ins.reg));
                            block.add(node);
                            block.add(store);
                            break;
                        }

                    case Opcode.Call:
                        {
                            var ins = (LCall)uins;
                            var f = file_.lookupFunction((uint)ins.address);
                            var args = (DConstant)block.stack.popValue();
                            var arguments = new List<DNode>();
                            for (var i = 0; i < args.value; i++)
                            {
                                arguments.Add(block.stack.popName());
                            }

                            var call = new DCall(f, arguments.ToArray());
                            block.stack.set(Register.Pri, call);
                            block.add(call);
                            break;
                        }

                    case Opcode.EqualConstant:
                        {
                            var ins = (LEqualConstant)uins;
                            var c = new DConstant(ins.value);
                            var node = new DBinary(SPOpcode.eq, block.stack.reg(ins.reg), c);
                            block.stack.set(Register.Pri, node);
                            block.add(c);
                            block.add(node);
                            break;
                        }

                    case Opcode.LoadIndex:
                        {
                            var ins = (LLoadIndex)uins;
                            var aref = new DArrayRef(block.stack.alt, block.stack.pri, ins.shift);
                            var load = new DLoad(aref);
                            block.stack.set(Register.Pri, load);
                            block.add(aref);
                            block.add(load);
                            break;
                        }

                    case Opcode.ZeroGlobal:
                        {
                            var ins = (LZeroGlobal)uins;
                            var global = file_.lookupGlobal(ins.address);
                            DNode dglobal = new DGlobal(global);
                            var rhs = new DConstant(0);
                            var lhs = new DStore(dglobal, rhs);
                            block.add(dglobal);
                            block.add(rhs);
                            block.add(lhs);
                            break;
                        }

                    case Opcode.IncGlobal:
                        {
                            var ins = (LIncGlobal)uins;
                            var global = file_.lookupGlobal(ins.address);
                            DNode dglobal = new DGlobal(global);

                            var load = new DLoad(dglobal);
                            var val = new DConstant(1);
                            var add = new DBinary(SPOpcode.add, load, val);
                            var store = new DStore(dglobal, add);
                            block.add(load);
                            block.add(val);
                            block.add(add);
                            block.add(store);
                            break;
                        }

                    case Opcode.StoreGlobalConstant:
                        {
                            var lstore = (LStoreGlobalConstant)uins;
                            var var = file_.lookupGlobal(lstore.address);
                            var val = new DConstant(lstore.value);
                            var global = new DGlobal(var);
                            var store = new DStore(global, val);
                            block.add(val);
                            block.add(global);
                            block.add(store);
                            break;
                        }

                    case Opcode.StoreLocalConstant:
                        {
                            var lstore = (LStoreLocalConstant)uins;
                            var var = block.stack.getName(lstore.address);
                            var val = new DConstant(lstore.value);
                            var store = new DStore(var, val);
                            block.add(val);
                            block.add(store);
                            break;
                        }

                    case Opcode.ZeroLocal:
                        {
                            var lstore = (LZeroLocal)uins;
                            var var = block.stack.getName(lstore.address);
                            var val = new DConstant(0);
                            var store = new DStore(var, val);
                            block.add(val);
                            block.add(store);
                            break;
                        }

                    case Opcode.Heap:
                        {
                            var ins = (LHeap)uins;
                            var heap = new DHeap(ins.amount);
                            block.add(heap);
                            block.stack.set(Register.Alt, heap);
                            break;
                        }

                    case Opcode.MemCopy:
                        {
                            var ins = (LMemCopy)uins;
                            var copy = new DMemCopy(block.stack.alt, block.stack.pri, ins.bytes);
                            block.add(copy);
                            break;
                        }

                    case Opcode.Switch:
                        {
                            var ins = (LSwitch)uins;
                            var switch_ = new DSwitch(block.stack.pri, ins);
                            block.add(switch_);
                            break;
                        }

                    default:
                        throw new Exception("unhandled opcode");
                }
            }

            for (var i = 0; i < block.lir.idominated.Length; i++)
            {
                var lir = block.lir.idominated[i];
                traverse(blocks_[lir.id]);
            }
        }

        public NodeBlock[] buildNodes()
        {
            blocks_[0].inherit(graph_, null);
            traverse(blocks_[0]);
            return blocks_;
        }
    }
}
