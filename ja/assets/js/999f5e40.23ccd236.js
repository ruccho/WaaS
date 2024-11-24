"use strict";(self.webpackChunkdocs=self.webpackChunkdocs||[]).push([[14114],{70098:(e,t,a)=>{a.r(t),a.d(t,{assets:()=>c,contentTitle:()=>r,default:()=>h,frontMatter:()=>l,metadata:()=>i,toc:()=>o});const i=JSON.parse('{"id":"WaaS.Models/BlockInstruction","title":"Class BlockInstruction","description":"Base of block instructions.","source":"@site/api/WaaS.Models/BlockInstruction.md","sourceDirName":"WaaS.Models","slug":"/WaaS.Models/BlockInstruction","permalink":"/WaaS/ja/api/WaaS.Models/BlockInstruction","draft":false,"unlisted":false,"tags":[],"version":"current","frontMatter":{"title":"Class BlockInstruction","sidebar_label":"BlockInstruction","description":"Base of block instructions."},"sidebar":"tutorialSidebar","previous":{"title":"BlockDelimiterInstruction","permalink":"/WaaS/ja/api/WaaS.Models/BlockDelimiterInstruction"},"next":{"title":"BlockType","permalink":"/WaaS/ja/api/WaaS.Models/BlockType"}}');var n=a(74848),s=a(28453);const l={title:"Class BlockInstruction",sidebar_label:"BlockInstruction",description:"Base of block instructions."},r="Class BlockInstruction",c={},o=[{value:"<strong>Assembly</strong>: WaaS.Core.dll",id:"assembly-waascoredll",level:6},{value:"Properties",id:"properties",level:2},{value:"BlockType",id:"blocktype",level:3},{value:"View Source",id:"view-source",level:6},{value:"End",id:"end",level:3},{value:"View Source",id:"view-source-1",level:6},{value:"Arity",id:"arity",level:3},{value:"View Source",id:"view-source-2",level:6},{value:"Methods",id:"methods",level:2},{value:"PreValidateStackState(in ValidationContext)",id:"prevalidatestackstatein-validationcontext",level:3},{value:"View Source",id:"view-source-3",level:6},{value:"Returns",id:"returns",level:5},{value:"Parameters",id:"parameters",level:5},{value:"ValidateStackState(in ValidationContext, ref ValidationBlockStackState)",id:"validatestackstatein-validationcontext-ref-validationblockstackstate",level:3},{value:"View Source",id:"view-source-4",level:6},{value:"Parameters",id:"parameters-1",level:5},{value:"OnBeforeBlockEnter(WasmStackFrame, out uint)",id:"onbeforeblockenterwasmstackframe-out-uint",level:3},{value:"View Source",id:"view-source-5",level:6},{value:"Parameters",id:"parameters-2",level:5},{value:"Execute(WasmStackFrame)",id:"executewasmstackframe",level:3},{value:"View Source",id:"view-source-6",level:6},{value:"Parameters",id:"parameters-3",level:5},{value:"InjectDelimiter(BlockDelimiterInstruction)",id:"injectdelimiterblockdelimiterinstruction",level:3},{value:"View Source",id:"view-source-7",level:6},{value:"Parameters",id:"parameters-4",level:5}];function d(e){const t={a:"a",br:"br",code:"code",em:"em",h1:"h1",h2:"h2",h3:"h3",h5:"h5",h6:"h6",header:"header",p:"p",pre:"pre",strong:"strong",table:"table",tbody:"tbody",td:"td",th:"th",thead:"thead",tr:"tr",...(0,s.R)(),...e.components};return(0,n.jsxs)(n.Fragment,{children:[(0,n.jsx)(t.header,{children:(0,n.jsx)(t.h1,{id:"class-blockinstruction",children:"Class BlockInstruction"})}),"\n",(0,n.jsx)(t.p,{children:"Base of block instructions."}),"\n",(0,n.jsxs)(t.h6,{id:"assembly-waascoredll",children:[(0,n.jsx)(t.strong,{children:"Assembly"}),": WaaS.Core.dll"]}),"\n",(0,n.jsx)(t.pre,{children:(0,n.jsx)(t.code,{className:"language-csharp",metastring:'title="Declaration"',children:"public abstract class BlockInstruction : Instruction\n"})}),"\n",(0,n.jsxs)(t.p,{children:[(0,n.jsx)(t.strong,{children:"Inheritance:"})," ",(0,n.jsx)(t.code,{children:"System.Object"})," -> ",(0,n.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Models/Instruction",children:"WaaS.Models.Instruction"})]}),"\n",(0,n.jsxs)(t.p,{children:[(0,n.jsx)(t.strong,{children:"Derived:"}),(0,n.jsx)(t.br,{}),"\n",(0,n.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Models/Block",children:"WaaS.Models.Block"}),", ",(0,n.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Models/If",children:"WaaS.Models.If"}),", ",(0,n.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Models/Loop",children:"WaaS.Models.Loop"})]}),"\n",(0,n.jsx)(t.h2,{id:"properties",children:"Properties"}),"\n",(0,n.jsx)(t.h3,{id:"blocktype",children:"BlockType"}),"\n",(0,n.jsx)(t.h6,{id:"view-source",children:(0,n.jsx)(t.a,{href:"https://github.com/ruccho/WaaS/blob/feature/component/WaaS.Unity/Packages/com.ruccho.waas/Core/Scripts/Models/Instructions/Instructions.Control.cs#L92",children:"View Source"})}),"\n",(0,n.jsx)(t.pre,{children:(0,n.jsx)(t.code,{className:"language-csharp",metastring:'title="Declaration"',children:"public BlockType BlockType { get; }\n"})}),"\n",(0,n.jsx)(t.h3,{id:"end",children:"End"}),"\n",(0,n.jsx)(t.h6,{id:"view-source-1",children:(0,n.jsx)(t.a,{href:"https://github.com/ruccho/WaaS/blob/feature/component/WaaS.Unity/Packages/com.ruccho.waas/Core/Scripts/Models/Instructions/Instructions.Control.cs#L94",children:"View Source"})}),"\n",(0,n.jsx)(t.pre,{children:(0,n.jsx)(t.code,{className:"language-csharp",metastring:'title="Declaration"',children:"public End End { get; }\n"})}),"\n",(0,n.jsx)(t.h3,{id:"arity",children:"Arity"}),"\n",(0,n.jsx)(t.h6,{id:"view-source-2",children:(0,n.jsx)(t.a,{href:"https://github.com/ruccho/WaaS/blob/feature/component/WaaS.Unity/Packages/com.ruccho.waas/Core/Scripts/Models/Instructions/Instructions.Control.cs#L96",children:"View Source"})}),"\n",(0,n.jsx)(t.pre,{children:(0,n.jsx)(t.code,{className:"language-csharp",metastring:'title="Declaration"',children:"public abstract uint Arity { get; }\n"})}),"\n",(0,n.jsx)(t.h2,{id:"methods",children:"Methods"}),"\n",(0,n.jsx)(t.h3,{id:"prevalidatestackstatein-validationcontext",children:"PreValidateStackState(in ValidationContext)"}),"\n",(0,n.jsx)(t.p,{children:"Get the number of values to pop and push from the stack to validate stack depth."}),"\n",(0,n.jsx)(t.h6,{id:"view-source-3",children:(0,n.jsx)(t.a,{href:"https://github.com/ruccho/WaaS/blob/feature/component/WaaS.Unity/Packages/com.ruccho.waas/Core/Scripts/Models/Instructions/Instructions.Control.cs#L98",children:"View Source"})}),"\n",(0,n.jsx)(t.pre,{children:(0,n.jsx)(t.code,{className:"language-csharp",metastring:'title="Declaration"',children:"public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)\n"})}),"\n",(0,n.jsx)(t.h5,{id:"returns",children:"Returns"}),"\n",(0,n.jsx)(t.p,{children:(0,n.jsx)(t.code,{children:"System.ValueTuple<System.UInt32,System.UInt32>"})}),"\n",(0,n.jsx)(t.h5,{id:"parameters",children:"Parameters"}),"\n",(0,n.jsxs)(t.table,{children:[(0,n.jsx)(t.thead,{children:(0,n.jsxs)(t.tr,{children:[(0,n.jsx)(t.th,{style:{textAlign:"left"},children:"Type"}),(0,n.jsx)(t.th,{style:{textAlign:"left"},children:"Name"})]})}),(0,n.jsx)(t.tbody,{children:(0,n.jsxs)(t.tr,{children:[(0,n.jsx)(t.td,{style:{textAlign:"left"},children:(0,n.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Models/ValidationContext",children:"WaaS.Models.ValidationContext"})}),(0,n.jsx)(t.td,{style:{textAlign:"left"},children:(0,n.jsx)(t.em,{children:"context"})})]})})]}),"\n",(0,n.jsx)(t.h3,{id:"validatestackstatein-validationcontext-ref-validationblockstackstate",children:"ValidateStackState(in ValidationContext, ref ValidationBlockStackState)"}),"\n",(0,n.jsx)(t.p,{children:"Simulates stack operations to validate the stack state."}),"\n",(0,n.jsx)(t.h6,{id:"view-source-4",children:(0,n.jsx)(t.a,{href:"https://github.com/ruccho/WaaS/blob/feature/component/WaaS.Unity/Packages/com.ruccho.waas/Core/Scripts/Models/Instructions/Instructions.Control.cs#L103",children:"View Source"})}),"\n",(0,n.jsx)(t.pre,{children:(0,n.jsx)(t.code,{className:"language-csharp",metastring:'title="Declaration"',children:"public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)\n"})}),"\n",(0,n.jsx)(t.h5,{id:"parameters-1",children:"Parameters"}),"\n",(0,n.jsxs)(t.table,{children:[(0,n.jsx)(t.thead,{children:(0,n.jsxs)(t.tr,{children:[(0,n.jsx)(t.th,{style:{textAlign:"left"},children:"Type"}),(0,n.jsx)(t.th,{style:{textAlign:"left"},children:"Name"})]})}),(0,n.jsxs)(t.tbody,{children:[(0,n.jsxs)(t.tr,{children:[(0,n.jsx)(t.td,{style:{textAlign:"left"},children:(0,n.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Models/ValidationContext",children:"WaaS.Models.ValidationContext"})}),(0,n.jsx)(t.td,{style:{textAlign:"left"},children:(0,n.jsx)(t.em,{children:"context"})})]}),(0,n.jsxs)(t.tr,{children:[(0,n.jsx)(t.td,{style:{textAlign:"left"},children:(0,n.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Models/ValidationBlockStackState",children:"WaaS.Models.ValidationBlockStackState"})}),(0,n.jsx)(t.td,{style:{textAlign:"left"},children:(0,n.jsx)(t.em,{children:"stackState"})})]})]})]}),"\n",(0,n.jsx)(t.h3,{id:"onbeforeblockenterwasmstackframe-out-uint",children:"OnBeforeBlockEnter(WasmStackFrame, out uint)"}),"\n",(0,n.jsx)(t.h6,{id:"view-source-5",children:(0,n.jsx)(t.a,{href:"https://github.com/ruccho/WaaS/blob/feature/component/WaaS.Unity/Packages/com.ruccho.waas/Core/Scripts/Models/Instructions/Instructions.Control.cs#L107",children:"View Source"})}),"\n",(0,n.jsx)(t.pre,{children:(0,n.jsx)(t.code,{className:"language-csharp",metastring:'title="Declaration"',children:"protected abstract void OnBeforeBlockEnter(WasmStackFrame current, out uint continuationIndex)\n"})}),"\n",(0,n.jsx)(t.h5,{id:"parameters-2",children:"Parameters"}),"\n",(0,n.jsxs)(t.table,{children:[(0,n.jsx)(t.thead,{children:(0,n.jsxs)(t.tr,{children:[(0,n.jsx)(t.th,{style:{textAlign:"left"},children:"Type"}),(0,n.jsx)(t.th,{style:{textAlign:"left"},children:"Name"})]})}),(0,n.jsxs)(t.tbody,{children:[(0,n.jsxs)(t.tr,{children:[(0,n.jsx)(t.td,{style:{textAlign:"left"},children:(0,n.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Runtime/WasmStackFrame",children:"WaaS.Runtime.WasmStackFrame"})}),(0,n.jsx)(t.td,{style:{textAlign:"left"},children:(0,n.jsx)(t.em,{children:"current"})})]}),(0,n.jsxs)(t.tr,{children:[(0,n.jsx)(t.td,{style:{textAlign:"left"},children:(0,n.jsx)(t.code,{children:"System.UInt32"})}),(0,n.jsx)(t.td,{style:{textAlign:"left"},children:(0,n.jsx)(t.em,{children:"continuationIndex"})})]})]})]}),"\n",(0,n.jsx)(t.h3,{id:"executewasmstackframe",children:"Execute(WasmStackFrame)"}),"\n",(0,n.jsx)(t.p,{children:"Executes the instruction."}),"\n",(0,n.jsx)(t.h6,{id:"view-source-6",children:(0,n.jsx)(t.a,{href:"https://github.com/ruccho/WaaS/blob/feature/component/WaaS.Unity/Packages/com.ruccho.waas/Core/Scripts/Models/Instructions/Instructions.Control.cs#L109",children:"View Source"})}),"\n",(0,n.jsx)(t.pre,{children:(0,n.jsx)(t.code,{className:"language-csharp",metastring:'title="Declaration"',children:"public override sealed void Execute(WasmStackFrame current)\n"})}),"\n",(0,n.jsx)(t.h5,{id:"parameters-3",children:"Parameters"}),"\n",(0,n.jsxs)(t.table,{children:[(0,n.jsx)(t.thead,{children:(0,n.jsxs)(t.tr,{children:[(0,n.jsx)(t.th,{style:{textAlign:"left"},children:"Type"}),(0,n.jsx)(t.th,{style:{textAlign:"left"},children:"Name"})]})}),(0,n.jsx)(t.tbody,{children:(0,n.jsxs)(t.tr,{children:[(0,n.jsx)(t.td,{style:{textAlign:"left"},children:(0,n.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Runtime/WasmStackFrame",children:"WaaS.Runtime.WasmStackFrame"})}),(0,n.jsx)(t.td,{style:{textAlign:"left"},children:(0,n.jsx)(t.em,{children:"current"})})]})})]}),"\n",(0,n.jsx)(t.h3,{id:"injectdelimiterblockdelimiterinstruction",children:"InjectDelimiter(BlockDelimiterInstruction)"}),"\n",(0,n.jsx)(t.h6,{id:"view-source-7",children:(0,n.jsx)(t.a,{href:"https://github.com/ruccho/WaaS/blob/feature/component/WaaS.Unity/Packages/com.ruccho.waas/Core/Scripts/Models/Instructions/Instructions.Control.cs#L115",children:"View Source"})}),"\n",(0,n.jsx)(t.pre,{children:(0,n.jsx)(t.code,{className:"language-csharp",metastring:'title="Declaration"',children:"public virtual void InjectDelimiter(BlockDelimiterInstruction delimiter)\n"})}),"\n",(0,n.jsx)(t.h5,{id:"parameters-4",children:"Parameters"}),"\n",(0,n.jsxs)(t.table,{children:[(0,n.jsx)(t.thead,{children:(0,n.jsxs)(t.tr,{children:[(0,n.jsx)(t.th,{style:{textAlign:"left"},children:"Type"}),(0,n.jsx)(t.th,{style:{textAlign:"left"},children:"Name"})]})}),(0,n.jsx)(t.tbody,{children:(0,n.jsxs)(t.tr,{children:[(0,n.jsx)(t.td,{style:{textAlign:"left"},children:(0,n.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Models/BlockDelimiterInstruction",children:"WaaS.Models.BlockDelimiterInstruction"})}),(0,n.jsx)(t.td,{style:{textAlign:"left"},children:(0,n.jsx)(t.em,{children:"delimiter"})})]})})]})]})}function h(e={}){const{wrapper:t}={...(0,s.R)(),...e.components};return t?(0,n.jsx)(t,{...e,children:(0,n.jsx)(d,{...e})}):d(e)}},28453:(e,t,a)=>{a.d(t,{R:()=>l,x:()=>r});var i=a(96540);const n={},s=i.createContext(n);function l(e){const t=i.useContext(s);return i.useMemo((function(){return"function"==typeof e?e(t):{...t,...e}}),[t,e])}function r(e){let t;return t=e.disableParentContext?"function"==typeof e.components?e.components(n):e.components||n:l(e.components),i.createElement(s.Provider,{value:t},e.children)}}}]);