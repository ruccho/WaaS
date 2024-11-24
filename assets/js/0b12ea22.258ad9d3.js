"use strict";(self.webpackChunkdocs=self.webpackChunkdocs||[]).push([[38090],{48723:(e,a,t)=>{t.r(a),t.d(a,{assets:()=>d,contentTitle:()=>n,default:()=>h,frontMatter:()=>i,metadata:()=>l,toc:()=>o});const l=JSON.parse('{"id":"WaaS.Models/BinaryInstruction`TValue, TValueType`","title":"Class BinaryInstruction<TValue, TValueType>","description":"Base class for binary operator instructions with the same input and output types.","source":"@site/api/WaaS.Models/BinaryInstruction`TValue, TValueType`.md","sourceDirName":"WaaS.Models","slug":"/WaaS.Models/BinaryInstruction`TValue, TValueType`","permalink":"/WaaS/api/WaaS.Models/BinaryInstruction`TValue, TValueType`","draft":false,"unlisted":false,"tags":[],"version":"current","frontMatter":{"title":"Class BinaryInstruction<TValue, TValueType>","sidebar_label":"BinaryInstruction<TValue, TValueType>","description":"Base class for binary operator instructions with the same input and output types."},"sidebar":"tutorialSidebar","previous":{"title":"BinaryBoolInstruction<TValue, TValueType>","permalink":"/WaaS/api/WaaS.Models/BinaryBoolInstruction`TValue, TValueType`"},"next":{"title":"Block","permalink":"/WaaS/api/WaaS.Models/Block"}}');var s=t(74848),r=t(28453);const i={title:"Class BinaryInstruction<TValue, TValueType>",sidebar_label:"BinaryInstruction<TValue, TValueType>",description:"Base class for binary operator instructions with the same input and output types."},n="Class BinaryInstruction<TValue, TValueType>",d={},o=[{value:"<strong>Assembly</strong>: WaaS.Core.dll",id:"assembly-waascoredll",level:6},{value:"Methods",id:"methods",level:2},{value:"Execute(WasmStackFrame)",id:"executewasmstackframe",level:3},{value:"View Source",id:"view-source",level:6},{value:"Parameters",id:"parameters",level:5},{value:"Operate(TValue, TValue)",id:"operatetvalue-tvalue",level:3},{value:"View Source",id:"view-source-1",level:6},{value:"Returns",id:"returns",level:5},{value:"Parameters",id:"parameters-1",level:5},{value:"PreValidateStackState(in ValidationContext)",id:"prevalidatestackstatein-validationcontext",level:3},{value:"View Source",id:"view-source-2",level:6},{value:"Returns",id:"returns-1",level:5},{value:"Parameters",id:"parameters-2",level:5},{value:"ValidateStackState(in ValidationContext, ref ValidationBlockStackState)",id:"validatestackstatein-validationcontext-ref-validationblockstackstate",level:3},{value:"View Source",id:"view-source-3",level:6},{value:"Parameters",id:"parameters-3",level:5}];function c(e){const a={a:"a",code:"code",em:"em",h1:"h1",h2:"h2",h3:"h3",h5:"h5",h6:"h6",header:"header",p:"p",pre:"pre",strong:"strong",table:"table",tbody:"tbody",td:"td",th:"th",thead:"thead",tr:"tr",...(0,r.R)(),...e.components},{Details:t}=a;return t||function(e,a){throw new Error("Expected "+(a?"component":"object")+" `"+e+"` to be defined: you likely forgot to import, pass, or provide it.")}("Details",!0),(0,s.jsxs)(s.Fragment,{children:[(0,s.jsx)(a.header,{children:(0,s.jsx)(a.h1,{id:"class-binaryinstructiontvalue-tvaluetype",children:"Class BinaryInstruction<TValue, TValueType>"})}),"\n",(0,s.jsx)(a.p,{children:"Base class for binary operator instructions with the same input and output types."}),"\n",(0,s.jsxs)(a.h6,{id:"assembly-waascoredll",children:[(0,s.jsx)(a.strong,{children:"Assembly"}),": WaaS.Core.dll"]}),"\n",(0,s.jsx)(a.pre,{children:(0,s.jsx)(a.code,{className:"language-csharp",metastring:'title="Declaration"',children:"public abstract class BinaryInstruction<TValue, TValueType> : Instruction where TValue : unmanaged where TValueType : struct, IValueType<TValue>\n"})}),"\n",(0,s.jsxs)(a.p,{children:[(0,s.jsx)(a.strong,{children:"Inheritance:"})," ",(0,s.jsx)(a.code,{children:"System.Object"})," -> ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/Instruction",children:"WaaS.Models.Instruction"})]}),"\n",(0,s.jsx)(a.p,{children:(0,s.jsx)(a.strong,{children:"Derived:"})}),"\n",(0,s.jsxs)(t,{children:[(0,s.jsx)("summary",{children:"Expand"}),(0,s.jsxs)(a.p,{children:[(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/AddF32",children:"WaaS.Models.AddF32"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/AddF64",children:"WaaS.Models.AddF64"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/AddI32",children:"WaaS.Models.AddI32"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/AddI64",children:"WaaS.Models.AddI64"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/AndI32",children:"WaaS.Models.AndI32"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/AndI64",children:"WaaS.Models.AndI64"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/CopysignF32",children:"WaaS.Models.CopysignF32"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/CopysignF64",children:"WaaS.Models.CopysignF64"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/DivF32",children:"WaaS.Models.DivF32"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/DivF64",children:"WaaS.Models.DivF64"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/DivI32S",children:"WaaS.Models.DivI32S"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/DivI32U",children:"WaaS.Models.DivI32U"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/DivI64S",children:"WaaS.Models.DivI64S"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/DivI64U",children:"WaaS.Models.DivI64U"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/MaxF32",children:"WaaS.Models.MaxF32"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/MaxF64",children:"WaaS.Models.MaxF64"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/MinF32",children:"WaaS.Models.MinF32"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/MinF64",children:"WaaS.Models.MinF64"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/MulF32",children:"WaaS.Models.MulF32"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/MulF64",children:"WaaS.Models.MulF64"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/MulI32",children:"WaaS.Models.MulI32"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/MulI64",children:"WaaS.Models.MulI64"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/OrI32",children:"WaaS.Models.OrI32"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/OrI64",children:"WaaS.Models.OrI64"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/RemI32S",children:"WaaS.Models.RemI32S"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/RemI32U",children:"WaaS.Models.RemI32U"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/RemI64S",children:"WaaS.Models.RemI64S"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/RemI64U",children:"WaaS.Models.RemI64U"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/RotlI32",children:"WaaS.Models.RotlI32"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/RotlI64",children:"WaaS.Models.RotlI64"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/RotrI32",children:"WaaS.Models.RotrI32"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/RotrI64",children:"WaaS.Models.RotrI64"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/ShlI32",children:"WaaS.Models.ShlI32"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/ShlI64",children:"WaaS.Models.ShlI64"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/ShrI32S",children:"WaaS.Models.ShrI32S"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/ShrI32U",children:"WaaS.Models.ShrI32U"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/ShrI64S",children:"WaaS.Models.ShrI64S"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/ShrI64U",children:"WaaS.Models.ShrI64U"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/SubF32",children:"WaaS.Models.SubF32"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/SubF64",children:"WaaS.Models.SubF64"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/SubI32",children:"WaaS.Models.SubI32"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/SubI64",children:"WaaS.Models.SubI64"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/XorI32",children:"WaaS.Models.XorI32"}),", ",(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/XorI64",children:"WaaS.Models.XorI64"})]})]}),"\n",(0,s.jsx)(a.h2,{id:"methods",children:"Methods"}),"\n",(0,s.jsx)(a.h3,{id:"executewasmstackframe",children:"Execute(WasmStackFrame)"}),"\n",(0,s.jsx)(a.p,{children:"Executes the instruction."}),"\n",(0,s.jsx)(a.h6,{id:"view-source",children:(0,s.jsx)(a.a,{href:"https://github.com/ruccho/WaaS/blob/feature/component/WaaS.Unity/Packages/com.ruccho.waas/Core/Scripts/Models/Instructions/Instructions.Numeric.cs#L146",children:"View Source"})}),"\n",(0,s.jsx)(a.pre,{children:(0,s.jsx)(a.code,{className:"language-csharp",metastring:'title="Declaration"',children:"public override void Execute(WasmStackFrame current)\n"})}),"\n",(0,s.jsx)(a.h5,{id:"parameters",children:"Parameters"}),"\n",(0,s.jsxs)(a.table,{children:[(0,s.jsx)(a.thead,{children:(0,s.jsxs)(a.tr,{children:[(0,s.jsx)(a.th,{style:{textAlign:"left"},children:"Type"}),(0,s.jsx)(a.th,{style:{textAlign:"left"},children:"Name"})]})}),(0,s.jsx)(a.tbody,{children:(0,s.jsxs)(a.tr,{children:[(0,s.jsx)(a.td,{style:{textAlign:"left"},children:(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Runtime/WasmStackFrame",children:"WaaS.Runtime.WasmStackFrame"})}),(0,s.jsx)(a.td,{style:{textAlign:"left"},children:(0,s.jsx)(a.em,{children:"current"})})]})})]}),"\n",(0,s.jsx)(a.h3,{id:"operatetvalue-tvalue",children:"Operate(TValue, TValue)"}),"\n",(0,s.jsx)(a.h6,{id:"view-source-1",children:(0,s.jsx)(a.a,{href:"https://github.com/ruccho/WaaS/blob/feature/component/WaaS.Unity/Packages/com.ruccho.waas/Core/Scripts/Models/Instructions/Instructions.Numeric.cs#L154",children:"View Source"})}),"\n",(0,s.jsx)(a.pre,{children:(0,s.jsx)(a.code,{className:"language-csharp",metastring:'title="Declaration"',children:"protected abstract TValue Operate(TValue lhs, TValue rhs)\n"})}),"\n",(0,s.jsx)(a.h5,{id:"returns",children:"Returns"}),"\n",(0,s.jsx)(a.p,{children:(0,s.jsx)(a.code,{children:"<TValue>"})}),"\n",(0,s.jsx)(a.h5,{id:"parameters-1",children:"Parameters"}),"\n",(0,s.jsxs)(a.table,{children:[(0,s.jsx)(a.thead,{children:(0,s.jsxs)(a.tr,{children:[(0,s.jsx)(a.th,{style:{textAlign:"left"},children:"Type"}),(0,s.jsx)(a.th,{style:{textAlign:"left"},children:"Name"})]})}),(0,s.jsxs)(a.tbody,{children:[(0,s.jsxs)(a.tr,{children:[(0,s.jsx)(a.td,{style:{textAlign:"left"},children:(0,s.jsx)(a.code,{children:"<TValue>"})}),(0,s.jsx)(a.td,{style:{textAlign:"left"},children:(0,s.jsx)(a.em,{children:"lhs"})})]}),(0,s.jsxs)(a.tr,{children:[(0,s.jsx)(a.td,{style:{textAlign:"left"},children:(0,s.jsx)(a.code,{children:"<TValue>"})}),(0,s.jsx)(a.td,{style:{textAlign:"left"},children:(0,s.jsx)(a.em,{children:"rhs"})})]})]})]}),"\n",(0,s.jsx)(a.h3,{id:"prevalidatestackstatein-validationcontext",children:"PreValidateStackState(in ValidationContext)"}),"\n",(0,s.jsx)(a.p,{children:"Get the number of values to pop and push from the stack to validate stack depth."}),"\n",(0,s.jsx)(a.h6,{id:"view-source-2",children:(0,s.jsx)(a.a,{href:"https://github.com/ruccho/WaaS/blob/feature/component/WaaS.Unity/Packages/com.ruccho.waas/Core/Scripts/Models/Instructions/Instructions.Numeric.cs#L156",children:"View Source"})}),"\n",(0,s.jsx)(a.pre,{children:(0,s.jsx)(a.code,{className:"language-csharp",metastring:'title="Declaration"',children:"public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)\n"})}),"\n",(0,s.jsx)(a.h5,{id:"returns-1",children:"Returns"}),"\n",(0,s.jsx)(a.p,{children:(0,s.jsx)(a.code,{children:"System.ValueTuple<System.UInt32,System.UInt32>"})}),"\n",(0,s.jsx)(a.h5,{id:"parameters-2",children:"Parameters"}),"\n",(0,s.jsxs)(a.table,{children:[(0,s.jsx)(a.thead,{children:(0,s.jsxs)(a.tr,{children:[(0,s.jsx)(a.th,{style:{textAlign:"left"},children:"Type"}),(0,s.jsx)(a.th,{style:{textAlign:"left"},children:"Name"})]})}),(0,s.jsx)(a.tbody,{children:(0,s.jsxs)(a.tr,{children:[(0,s.jsx)(a.td,{style:{textAlign:"left"},children:(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/ValidationContext",children:"WaaS.Models.ValidationContext"})}),(0,s.jsx)(a.td,{style:{textAlign:"left"},children:(0,s.jsx)(a.em,{children:"context"})})]})})]}),"\n",(0,s.jsx)(a.h3,{id:"validatestackstatein-validationcontext-ref-validationblockstackstate",children:"ValidateStackState(in ValidationContext, ref ValidationBlockStackState)"}),"\n",(0,s.jsx)(a.p,{children:"Simulates stack operations to validate the stack state."}),"\n",(0,s.jsx)(a.h6,{id:"view-source-3",children:(0,s.jsx)(a.a,{href:"https://github.com/ruccho/WaaS/blob/feature/component/WaaS.Unity/Packages/com.ruccho.waas/Core/Scripts/Models/Instructions/Instructions.Numeric.cs#L161",children:"View Source"})}),"\n",(0,s.jsx)(a.pre,{children:(0,s.jsx)(a.code,{className:"language-csharp",metastring:'title="Declaration"',children:"public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)\n"})}),"\n",(0,s.jsx)(a.h5,{id:"parameters-3",children:"Parameters"}),"\n",(0,s.jsxs)(a.table,{children:[(0,s.jsx)(a.thead,{children:(0,s.jsxs)(a.tr,{children:[(0,s.jsx)(a.th,{style:{textAlign:"left"},children:"Type"}),(0,s.jsx)(a.th,{style:{textAlign:"left"},children:"Name"})]})}),(0,s.jsxs)(a.tbody,{children:[(0,s.jsxs)(a.tr,{children:[(0,s.jsx)(a.td,{style:{textAlign:"left"},children:(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/ValidationContext",children:"WaaS.Models.ValidationContext"})}),(0,s.jsx)(a.td,{style:{textAlign:"left"},children:(0,s.jsx)(a.em,{children:"context"})})]}),(0,s.jsxs)(a.tr,{children:[(0,s.jsx)(a.td,{style:{textAlign:"left"},children:(0,s.jsx)(a.a,{href:"/WaaS/api/WaaS.Models/ValidationBlockStackState",children:"WaaS.Models.ValidationBlockStackState"})}),(0,s.jsx)(a.td,{style:{textAlign:"left"},children:(0,s.jsx)(a.em,{children:"stackState"})})]})]})]})]})}function h(e={}){const{wrapper:a}={...(0,r.R)(),...e.components};return a?(0,s.jsx)(a,{...e,children:(0,s.jsx)(c,{...e})}):c(e)}},28453:(e,a,t)=>{t.d(a,{R:()=>i,x:()=>n});var l=t(96540);const s={},r=l.createContext(s);function i(e){const a=l.useContext(r);return l.useMemo((function(){return"function"==typeof e?e(a):{...a,...e}}),[a,e])}function n(e){let a;return a=e.disableParentContext?"function"==typeof e.components?e.components(s):e.components||s:i(e.components),l.createElement(r.Provider,{value:a},e.children)}}}]);