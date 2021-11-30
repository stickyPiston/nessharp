using System;

namespace NesSharp
{

    using Cycle = Action<CPU>;

    public partial class CPU
    {
        private static Cycle[][] addressingInstructions = {
/* NONE  */ new Cycle[] {},
/* ACC   */ new Cycle[] { FetchPC, DummyFetchPC },
/* IMP   */ new Cycle[] { FetchPC, DummyFetchPC },
/* IMM   */ new Cycle[] { FetchPC, FetchPC },
/* REL   */ new Cycle[] { FetchPC, FetchPC },
/* ZERO  */ new Cycle[] { FetchPC, ZpgPC },
/* ZEROX */ new Cycle[] {}, // TODO
/* ZEROY */ new Cycle[] {}, // TODO
/* IND   */ new Cycle[] {}, // TODO
/* INDX  */ new Cycle[] {}, // TODO
/* INDY  */ new Cycle[] {}, // TODO
/* INDYW */ new Cycle[] {}, // TODO
/* ABS   */ new Cycle[] {}, // TODO
/* ABSX  */ new Cycle[] {}, // TODO
/* ABSXW */ new Cycle[] {}, // TODO
/* ABSY  */ new Cycle[] {}, // TODO
/* ABSYW */ new Cycle[] {}, // TODO
        };

        private static Instruction IRQInstruction = new Instruction("IRQ", AddressingMode.NONE, new Cycle[] {
            DummyFetchPC,       // dummy read
            DummyFetchPC,       // dummy read
            PushPCH,            // push PC to stack 
            PushPCL,
            PushP(false),       // push P to stack with B = false
            FetchPCLow(0xFFFE), // fetch PC, set I flag
            FetchPCHigh(0xFFFF),
        });
        private static Instruction NMIInstruction = new Instruction("NMI", AddressingMode.NONE, new Cycle[] {
            DummyFetchPC,       // dummy read
            DummyFetchPC,       // dummy read
            PushPCH,            // push PC to stack 
            PushPCL,
            PushP(false),       // push P to stack with B = false
            FetchPCLow(0xFFFA), // fetch PC, set I flag
            FetchPCHigh(0xFFFB),
        });
        private static Instruction RESETInstruction = new Instruction("RESET", AddressingMode.NONE, new Cycle[] {
            DummyFetchPC,   DummyFetchPC,   DummyFetchPC,   // dummy reads (I'm not sure why there are 3)
            DummyPushStack, DummyPushStack, DummyPushStack, // decrement stack 3 times
            FetchPCLow(0xFFFC),                             // fetch PC, set I flag
            FetchPCHigh(0xFFFD),
        });
        private static Instruction[] instructions = {
/* 00 */    new Instruction("BRK impl",  AddressingMode.IMP,   new Cycle[] { PushPCH, PushPCL, PushP(true), FetchPCLow(0xFFFE), FetchPCHigh(0xFFFF) }),
/* 01 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 02 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 03 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 04 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 05 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 06 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 07 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 08 */    new Instruction("PHP impl",  AddressingMode.IMP,   new Cycle[] { PushP(true) }),
/* 09 */    new Instruction("ORA #",     AddressingMode.IMM,   new Cycle[] { ORA }),
/* 0a */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 0b */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 0c */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 0d */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 0e */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 0f */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 10 */    new Instruction("BPL rel",   AddressingMode.REL,   new Cycle[] { BPL, FixPC }),
/* 11 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 12 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 13 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 14 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 15 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 16 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 17 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 18 */    new Instruction("CLC impl",  AddressingMode.IMP,   new Cycle[] { CLC }),
/* 19 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 1a */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 1b */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 1c */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 1d */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 1e */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 1f */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 20 */    new Instruction("JSR abs",   AddressingMode.IMM,   new Cycle[] { DummyPeekStack, PushPCH, PushPCL, JumpPC }),
/* 21 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 22 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 23 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 24 */    new Instruction("BIT zpg",   AddressingMode.ZERO,  new Cycle[] { BIT }),
/* 25 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 26 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 27 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 28 */    new Instruction("PLP impl",  AddressingMode.IMP,   new Cycle[] { IncSP, PullP }),
/* 29 */    new Instruction("AND #",     AddressingMode.IMM,   new Cycle[] { AND }),
/* 2a */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 2b */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 2c */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 2d */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 2e */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 2f */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 30 */    new Instruction("BMI rel",   AddressingMode.REL,   new Cycle[] { BMI, FixPC }),
/* 31 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 32 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 33 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 34 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 35 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 36 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 37 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 38 */    new Instruction("SEC impl",  AddressingMode.IMP,   new Cycle[] { SEC }),
/* 39 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 3a */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 3b */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 3c */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 3d */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 3e */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 3f */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 40 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 41 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 42 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 43 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 44 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 45 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 46 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 47 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 48 */    new Instruction("PHA impl",  AddressingMode.IMP,   new Cycle[] { PushA }),
/* 49 */    new Instruction("EOR #",     AddressingMode.IMM,   new Cycle[] {  EOR }),
/* 4a */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 4b */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 4c */    new Instruction("JMP abs",   AddressingMode.IMM,   new Cycle[] { JumpPC }),
/* 4d */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 4e */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 4f */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 50 */    new Instruction("BVC rel",   AddressingMode.REL,   new Cycle[] {  BVC, FixPC }),
/* 51 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 52 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 53 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 54 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 55 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 56 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 57 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 58 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 59 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 5a */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 5b */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 5c */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 5d */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 5e */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 5f */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 60 */    new Instruction("RTS impl",  AddressingMode.IMP,   new Cycle[] { IncSP, PullPCL, PullPCH, FetchPC }),
/* 61 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 62 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 63 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 64 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 65 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 66 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 67 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 68 */    new Instruction("PLA impl",  AddressingMode.IMP,   new Cycle[] { IncSP, PullA }),
/* 69 */    new Instruction("ADC #",     AddressingMode.IMM,   new Cycle[] { ADC }),
/* 6a */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 6b */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 6c */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 6d */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 6e */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 6f */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 70 */    new Instruction("BVS rel",   AddressingMode.REL,   new Cycle[] { BVS, FixPC }),
/* 71 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 72 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 73 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 74 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 75 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 76 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 77 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 78 */    new Instruction("SEI impl",  AddressingMode.IMP,   new Cycle[] { SEI }),
/* 79 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 7a */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 7b */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 7c */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 7d */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 7e */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 7f */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 80 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 81 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 82 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 83 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 84 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 85 */    new Instruction("STA zpg",   AddressingMode.ZERO,  new Cycle[] { STA }),
/* 86 */    new Instruction("STX zpg",   AddressingMode.ZERO,  new Cycle[] { STX }),
/* 87 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 88 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 89 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 8a */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 8b */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 8c */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 8d */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 8e */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 8f */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 90 */    new Instruction("BCC rel",   AddressingMode.REL,   new Cycle[] { BCC, FixPC }),
/* 91 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 92 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 93 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 94 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 95 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 96 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 97 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 98 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 99 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 9a */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 9b */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 9c */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 9d */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 9e */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* 9f */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* a0 */    new Instruction("LDY #",     AddressingMode.IMM,   new Cycle[] { LDY }),
/* a1 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* a2 */    new Instruction("LDX #",     AddressingMode.IMM,   new Cycle[] { LDX }),
/* a3 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* a4 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* a5 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* a6 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* a7 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* a8 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* a9 */    new Instruction("LDA #",     AddressingMode.IMM,   new Cycle[] { LDA }),
/* aa */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* ab */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* ac */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* ad */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* ae */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* af */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* b0 */    new Instruction("BCS rel",   AddressingMode.REL,   new Cycle[] { BCS, FixPC }),
/* b1 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* b2 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* b3 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* b4 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* b5 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* b6 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* b7 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* b8 */    new Instruction("CLV impl",  AddressingMode.IMP,   new Cycle[] { CLV }),
/* b9 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* ba */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* bb */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* bc */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* bd */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* be */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* bf */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* c0 */    new Instruction("CPY #",     AddressingMode.IMM,   new Cycle[] { CPY }),
/* c1 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* c2 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* c3 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* c4 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* c5 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* c6 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* c7 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* c8 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* c9 */    new Instruction("CMP #",     AddressingMode.IMM,   new Cycle[] { CMP }),
/* ca */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* cb */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* cc */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* cd */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* ce */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* cf */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* d0 */    new Instruction("BNE rel",   AddressingMode.REL,   new Cycle[] { BNE, FixPC }),
/* d1 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* d2 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* d3 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* d4 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* d5 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* d6 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* d7 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* d8 */    new Instruction("CLD impl",  AddressingMode.IMP,   new Cycle[] { CLD }),
/* d9 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* da */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* db */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* dc */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* dd */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* de */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* df */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* e0 */    new Instruction("CPX #",     AddressingMode.IMM,   new Cycle[] { CPX }),
/* e1 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* e2 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* e3 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* e4 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* e5 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* e6 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* e7 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* e8 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* e9 */    new Instruction("SBC #",     AddressingMode.IMM,   new Cycle[] { SBC }),
/* ea */    new Instruction("NOP impl",  AddressingMode.IMP,   new Cycle[] {}),
/* eb */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* ec */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* ed */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* ee */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* ef */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* f0 */    new Instruction("BEQ rel",   AddressingMode.REL,   new Cycle[] { BEQ, FixPC }),
/* f1 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* f2 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* f3 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* f4 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* f5 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* f6 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* f7 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* f8 */    new Instruction("SED impl",  AddressingMode.IMP,   new Cycle[] { SED }),
/* f9 */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* fa */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* fb */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* fc */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* fd */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* fe */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
/* ff */    new Instruction("JAM",       AddressingMode.IMP,   new Cycle[] { Jam }),
        };
    }
}
