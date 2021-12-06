using System;

namespace NesSharp
{

    using Cycle = Action<CPU>;

    public partial class CPU
    {
        private static Cycle[][] addressingInstructions = {
/* NONE  */ new Cycle[] {},
/* ACC   */ new Cycle[] { ValFromPC, ValFromAcc },
/* IMP   */ new Cycle[] { ValFromPC, DummyReadAtPC },
/* IMM   */ new Cycle[] { ValFromPC, ValFromPC },
/* REL   */ new Cycle[] { ValFromPC, ValFromPC },
/* ZERO  */ new Cycle[] { ValFromPC, LowFromPC },
/* ZEROX */ new Cycle[] { ValFromPC, LowFromPC, LowAddX },
/* ZEROY */ new Cycle[] { ValFromPC, LowFromPC, LowAddY },
/* INDX  */ new Cycle[] { ValFromPC, ValFromPC, ValAddX, LowFromVal, HighFromVal },
/* INDY  */ new Cycle[] { ValFromPC, ValFromPC, LowFromVal, HighFromValAddY, FixAddr },
/* ABS   */ new Cycle[] { ValFromPC, LowFromPC, HighFromPC },
/* ABSX  */ new Cycle[] { ValFromPC, LowFromPC, HighFromPCAddX, FixAddr },
/* ABSY  */ new Cycle[] { ValFromPC, LowFromPC, HighFromPCAddY, FixAddr },
        };

        private static string[] addressingNames = {
/* NONE  */ "",
/* ACC   */ "A",
/* IMP   */ "impl",
/* IMM   */ "#",
/* REL   */ "rel",
/* ZERO  */ "zpg",
/* ZEROX */ "zpg,X",
/* ZEROY */ "zpg,Y",
/* INDX  */ "X,ind",
/* INDY  */ "ind,Y",
/* ABS   */ "abs",
/* ABSX  */ "abs,X",
/* ABSY  */ "abs,Y",
        };

        private static Instruction IRQInstruction = new Instruction("IRQ", AddressingMode.NONE, false, new Cycle[] {
            DummyReadAtPC,         // dummy read
            DummyReadAtPC,         // dummy read
            PushPCH,               // push PC to stack 
            PushPCL,
            PushP(false),          // push P to stack with B = false
            PCLowFromAddr(0xFFFE), // fetch PC, set I flag
            PCHighFromAddr(0xFFFF),
        });
        private static Instruction NMIInstruction = new Instruction("NMI", AddressingMode.NONE, false, new Cycle[] {
            DummyReadAtPC,         // dummy read
            DummyReadAtPC,         // dummy read
            PushPCH,               // push PC to stack
            PushPCL,
            PushP(false),          // push P to stack with B = false
            PCLowFromAddr(0xFFFA), // fetch PC, set I flag
            PCHighFromAddr(0xFFFB),
        });
        private static Instruction RESETInstruction = new Instruction("RESET", AddressingMode.NONE, false, new Cycle[] {
            DummyReadAtPC,  DummyReadAtPC,  DummyReadAtPC,  // dummy reads (I'm not sure why there are 3)
            DummyPushStack, DummyPushStack, DummyPushStack, // decrement stack 3 times
            PCLowFromAddr(0xFFFC),                          // fetch PC, set I flag
            PCHighFromAddr(0xFFFD),
        });

        private static Instruction[] instructions = new Instruction[256];

        public static byte CountLegalInstructions() {
            byte i = 0;
            foreach (Instruction instr in instructions)
            {
                if (!instr.Illegal) i++;
            }
            return i;
        }

        static CPU() {
            // Fill with jam
            Array.Fill(instructions, new Instruction("JAM", AddressingMode.IMP, false, new Cycle[] { Jam }, false, true));

            // Generate instructions
            for (int i = 0; i < 256; i++) {
                AddressingMode mode = AddressingMode.IMP;
                Cycle cycle = null;
                bool readsAddr = false;
                bool writesAddr = false;
                bool illegal = false;
                string name = "";

                if (i == 0x96 || i == 0xB6 || i == 0x97 || i == 0xB7)
                {
                    mode = AddressingMode.ZEROY;
                }
                else if (i == 0xBE || i == 0xBF)
                {
                    mode = AddressingMode.ABSY;
                }
                else switch (i & 0x1F)
                {
                    case 0x08:
                    case 0x10:
                    case 0x12:
                    case 0x18:
                        // Rows that jam or are illegal
                        continue;
                    case 0x1A:
                        cycle = NOP;
                        name = "NOP";
                        readsAddr = true;
                        illegal = true;
                        mode = AddressingMode.IMP;
                        break;
                    case 0x02:
                        if (i != 0xA2) continue; // LDX #
                        mode = AddressingMode.IMM;
                        break;
                    case 0x00:
                    case 0x09:
                        mode = AddressingMode.IMM;
                        break;
                    case 0x0B:
                        if (i != 0xEB) continue; // *SBC IMM
                        mode = AddressingMode.IMM;
                        break;
                    case 0x01:
                    case 0x03:
                        mode = AddressingMode.INDX;
                        break;
                    case 0x04:
                    case 0x05:
                    case 0x06:
                    case 0x07:
                        mode = AddressingMode.ZERO;
                        break;
                    case 0x0A:
                        mode = AddressingMode.ACC;
                        break;
                    case 0x0C:
                    case 0x0D:
                    case 0x0E:
                    case 0x0F:
                        mode = AddressingMode.ABS;
                        break;
                    case 0x11:
                    case 0x13:
                        if (i == 0x93) continue; // Unusual operation SHA
                        mode = AddressingMode.INDY;
                        break;
                    case 0x14:
                        if (i != 0x94 && i != 0xB4) { // STY zpg,X // LDY zpg,X
                            cycle = NOP;
                            name = "NOP";
                            readsAddr = true;
                            illegal = true;
                        }
                        mode = AddressingMode.ZEROX;
                        break;
                    case 0x15:
                    case 0x16:
                    case 0x17:
                        mode = AddressingMode.ZEROX;
                        break;
                    case 0x19:
                    case 0x1B:
                        if (i == 0x9B) continue; // Unusual operation SHS
                        if (i == 0xBB) continue; // Unusual operation LAS
                        mode = AddressingMode.ABSY;
                        break;
                    case 0x1C:
                        if (i == 0x9C) continue; // Unusual operation SHY
                        if (i != 0xBC) { // LDY abs,X
                            cycle = NOP;
                            name = "NOP";
                            readsAddr = true;
                            illegal = true;
                        }
                        mode = AddressingMode.ABSX;
                        break;
                    case 0x1D:
                    case 0x1E:
                    case 0x1F:
                        if (i == 0x9E) continue; // Unusual operation SHX
                        if (i == 0x9F) continue; // Unusual operation SHA
                        mode = AddressingMode.ABSX;
                        break;
                }

                if (cycle == null) switch (i & 0b11100011)
                {
                    case 0x00:                  cycle = NOP; name = "NOP"; readsAddr = true; illegal = true;    break;
                    case 0x01:                  cycle = ORA; name = "ORA"; readsAddr = true;                    break;
                    case 0x02:                  cycle = ASL; name = "ASL"; readsAddr = true; writesAddr = true; break;
                    case 0x03:                  cycle = SLO; name = "SLO"; readsAddr = true; writesAddr = true; illegal = true; break;

                    case 0x20:                  cycle = BIT; name = "BIT"; readsAddr = true;                    break;
                    case 0x21:                  cycle = AND; name = "AND"; readsAddr = true;                    break;
                    case 0x22:                  cycle = ROL; name = "ROL"; readsAddr = true; writesAddr = true; break;
                    case 0x23:                  cycle = RLA; name = "RLA"; readsAddr = true; writesAddr = true; illegal = true; break;

                    case 0x40:                  cycle = NOP; name = "NOP"; readsAddr = true; illegal = true;    break;
                    case 0x41:                  cycle = EOR; name = "EOR"; readsAddr = true;                    break;
                    case 0x42:                  cycle = LSR; name = "LSR"; readsAddr = true; writesAddr = true; break;
                    case 0x43:                  cycle = SRE; name = "SRE"; readsAddr = true; writesAddr = true; illegal = true; break;

                    case 0x60:                  cycle = NOP; name = "NOP"; readsAddr = true; illegal = true;    break;
                    case 0x61:                  cycle = ADC; name = "ADC"; readsAddr = true;                    break;
                    case 0x62:                  cycle = ROR; name = "ROR"; readsAddr = true; writesAddr = true; break;
                    case 0x63:                  cycle = RRA; name = "RRA"; readsAddr = true; writesAddr = true; illegal = true; break;

                    case 0x80: if (i != 0x80) { cycle = STY; name = "STY"; writesAddr = true; }
                               else           { cycle = NOP; name = "NOP"; readsAddr = true; illegal = true; }  break;
                    case 0x81: if (i != 0x89) { cycle = STA; name = "STA"; writesAddr = true; }
                               else           { cycle = NOP; name = "NOP"; readsAddr = true; illegal = true; }  break;
                    case 0x82:                  cycle = STX; name = "STX"; writesAddr = true;                   break;
                    case 0x83:                  cycle = SAX; name = "SAX"; writesAddr = true; illegal = true;   break;

                    case 0xA0:                  cycle = LDY; name = "LDY"; readsAddr = true;                    break;
                    case 0xA1:                  cycle = LDA; name = "LDA"; readsAddr = true;                    break;
                    case 0xA2:                  cycle = LDX; name = "LDX"; readsAddr = true;                    break;
                    case 0xA3:                  cycle = LAX; name = "LAX"; readsAddr = true; illegal = true;    break;

                    case 0xC0:                  cycle = CPY; name = "CPY"; readsAddr = true;                    break;
                    case 0xC1:                  cycle = CMP; name = "CMP"; readsAddr = true;                    break;
                    case 0xC2:                  cycle = DEC; name = "DEC"; readsAddr = true; writesAddr = true; break;
                    case 0xC3:                  cycle = DCP; name = "DCP"; readsAddr = true; writesAddr = true; illegal = true; break;

                    case 0xE0:                  cycle = CPX; name = "CPX"; readsAddr = true;                    break;
                    case 0xE1:                  cycle = SBC; name = "SBC"; readsAddr = true;                    break;
                    case 0xE2:                  cycle = INC; name = "INC"; readsAddr = true; writesAddr = true; break;
                    case 0xE3: if (i != 0xEB) { cycle = ISB; name = "ISB"; readsAddr = true; writesAddr = true; illegal = true; }
                               else           { cycle = SBC; name = "SBC"; readsAddr = true; illegal = true; }  break;
                }

                if (cycle == null)
                {
                    continue;
                }
                else
                {
                    name += " " + addressingNames[(int) mode];
                }

                if (mode == AddressingMode.IMM || mode == AddressingMode.IMP || mode == AddressingMode.ACC)
                {
                    readsAddr = false;
                    writesAddr = false;
                }

                instructions[i] = new Instruction(name, mode, readsAddr, new Cycle[] { cycle }, writesAddr, illegal);
            }

            // Rest of the instructions
            instructions[0x00] = new Instruction("BRK impl",  AddressingMode.IMP, false, new Cycle[] { PushPCH, PushPCL, PushP(true), PCLowFromAddr(0xFFFE), PCHighFromAddr(0xFFFF) });
            instructions[0x20] = new Instruction("JSR abs",   AddressingMode.IMM, false, new Cycle[] { DummyReadAtSP, PushPCH, PushPCL, JumpPC });
            instructions[0x40] = new Instruction("RTI impl",  AddressingMode.IMP, false, new Cycle[] { IncSP, PullP(true), PullPCL, PullPCH });
            instructions[0x60] = new Instruction("RTS impl",  AddressingMode.IMP, false, new Cycle[] { IncSP, PullPCL, PullPCH, ValFromPC });

            instructions[0x10] = new Instruction("BPL rel",   AddressingMode.REL, false, new Cycle[] { BPL, FixPC });
            instructions[0x30] = new Instruction("BMI rel",   AddressingMode.REL, false, new Cycle[] { BMI, FixPC });
            instructions[0x50] = new Instruction("BVC rel",   AddressingMode.REL, false, new Cycle[] { BVC, FixPC });
            instructions[0x70] = new Instruction("BVS rel",   AddressingMode.REL, false, new Cycle[] { BVS, FixPC });
            instructions[0x90] = new Instruction("BCC rel",   AddressingMode.REL, false, new Cycle[] { BCC, FixPC });
            instructions[0xB0] = new Instruction("BCS rel",   AddressingMode.REL, false, new Cycle[] { BCS, FixPC });
            instructions[0xD0] = new Instruction("BNE rel",   AddressingMode.REL, false, new Cycle[] { BNE, FixPC });
            instructions[0xF0] = new Instruction("BEQ rel",   AddressingMode.REL, false, new Cycle[] { BEQ, FixPC });

            instructions[0x08] = new Instruction("PHP impl",  AddressingMode.IMP, false, new Cycle[] { PushP(true) });
            instructions[0x28] = new Instruction("PLP impl",  AddressingMode.IMP, false, new Cycle[] { IncSP, PullP(false) });
            instructions[0x48] = new Instruction("PHA impl",  AddressingMode.IMP, false, new Cycle[] { PushA });
            instructions[0x68] = new Instruction("PLA impl",  AddressingMode.IMP, false, new Cycle[] { IncSP, PullA });
            
            instructions[0x18] = new Instruction("CLC impl",  AddressingMode.IMP, false, new Cycle[] { CLC });
            instructions[0x38] = new Instruction("SEC impl",  AddressingMode.IMP, false, new Cycle[] { SEC });
            instructions[0x58] = new Instruction("CLI impl",  AddressingMode.IMP, false, new Cycle[] { CLI });
            instructions[0x78] = new Instruction("SEI impl",  AddressingMode.IMP, false, new Cycle[] { SEI });
            instructions[0xB8] = new Instruction("CLV impl",  AddressingMode.IMP, false, new Cycle[] { CLV });
            instructions[0xD8] = new Instruction("CLD impl",  AddressingMode.IMP, false, new Cycle[] { CLD });
            instructions[0xF8] = new Instruction("SED impl",  AddressingMode.IMP, false, new Cycle[] { SED });

            instructions[0x88] = new Instruction("DEY impl",  AddressingMode.IMP, false, new Cycle[] { DEY });
            instructions[0xC8] = new Instruction("INY impl",  AddressingMode.IMP, false, new Cycle[] { INY });
            instructions[0xCA] = new Instruction("DEX impl",  AddressingMode.IMP, false, new Cycle[] { DEX });
            instructions[0xE8] = new Instruction("INX impl",  AddressingMode.IMP, false, new Cycle[] { INX });

            instructions[0x8A] = new Instruction("TXA impl",  AddressingMode.IMP, false, new Cycle[] { TXA });
            instructions[0x98] = new Instruction("TYA impl",  AddressingMode.IMP, false, new Cycle[] { TYA });
            instructions[0x9A] = new Instruction("TXS impl",  AddressingMode.IMP, false, new Cycle[] { TXS });
            instructions[0xA8] = new Instruction("TAY impl",  AddressingMode.IMP, false, new Cycle[] { TAY });
            instructions[0xAA] = new Instruction("TAX impl",  AddressingMode.IMP, false, new Cycle[] { TAX });
            instructions[0xBA] = new Instruction("TSX impl",  AddressingMode.IMP, false, new Cycle[] { TSX });

            instructions[0x4C] = new Instruction("JMP abs",   AddressingMode.IMM, false, new Cycle[] { JumpPC });
            instructions[0x6C] = new Instruction("JMP ind",   AddressingMode.ABS, true,  new Cycle[] { JumpAddr });

            instructions[0xEA] = new Instruction("NOP impl",  AddressingMode.IMP, false, new Cycle[] {});
        }
    }
}
