"use strict";(self.webpackChunkdocs=self.webpackChunkdocs||[]).push([[81397],{92822:(e,t,n)=>{n.r(t),n.d(t,{assets:()=>c,contentTitle:()=>s,default:()=>u,frontMatter:()=>r,metadata:()=>a,toc:()=>o});const a=JSON.parse('{"id":"WaaS.Runtime/ExternalFunction","title":"Class ExternalFunction","description":"Represents a function that can be invoked from a WebAssembly module.","source":"@site/api/WaaS.Runtime/ExternalFunction.md","sourceDirName":"WaaS.Runtime","slug":"/WaaS.Runtime/ExternalFunction","permalink":"/WaaS/ja/api/WaaS.Runtime/ExternalFunction","draft":false,"unlisted":false,"tags":[],"version":"current","frontMatter":{"title":"Class ExternalFunction","sidebar_label":"ExternalFunction","description":"Represents a function that can be invoked from a WebAssembly module."},"sidebar":"tutorialSidebar","previous":{"title":"ExportInstance","permalink":"/WaaS/ja/api/WaaS.Runtime/ExportInstance"},"next":{"title":"ExternalFunctionCoreBoxedDelegate","permalink":"/WaaS/ja/api/WaaS.Runtime/ExternalFunctionCoreBoxedDelegate"}}');var l=n(74848),i=n(28453);const r={title:"Class ExternalFunction",sidebar_label:"ExternalFunction",description:"Represents a function that can be invoked from a WebAssembly module."},s="Class ExternalFunction",c={},o=[{value:"<strong>Assembly</strong>: WaaS.Core.dll",id:"assembly-waascoredll",level:6},{value:"View Source",id:"view-source",level:6},{value:"Properties",id:"properties",level:2},{value:"Type",id:"type",level:3},{value:"View Source",id:"view-source-1",level:6},{value:"Methods",id:"methods",level:2},{value:"CreateFrame(ExecutionContext, ReadOnlySpan&lt;StackValueItem&gt;)",id:"createframeexecutioncontext-readonlyspanstackvalueitem",level:3},{value:"View Source",id:"view-source-2",level:6},{value:"Returns",id:"returns",level:5},{value:"Parameters",id:"parameters",level:5},{value:"Invoke(ExecutionContext, ReadOnlySpan&lt;StackValueItem&gt;, Span&lt;StackValueItem&gt;)",id:"invokeexecutioncontext-readonlyspanstackvalueitem-spanstackvalueitem",level:3},{value:"View Source",id:"view-source-3",level:6},{value:"Parameters",id:"parameters-1",level:5},{value:"Implements",id:"implements",level:2}];function d(e){const t={a:"a",br:"br",code:"code",em:"em",h1:"h1",h2:"h2",h3:"h3",h5:"h5",h6:"h6",header:"header",li:"li",p:"p",pre:"pre",strong:"strong",table:"table",tbody:"tbody",td:"td",th:"th",thead:"thead",tr:"tr",ul:"ul",...(0,i.R)(),...e.components};return(0,l.jsxs)(l.Fragment,{children:[(0,l.jsx)(t.header,{children:(0,l.jsx)(t.h1,{id:"class-externalfunction",children:"Class ExternalFunction"})}),"\n",(0,l.jsx)(t.p,{children:"Represents a function that can be invoked from a WebAssembly module."}),"\n",(0,l.jsxs)(t.h6,{id:"assembly-waascoredll",children:[(0,l.jsx)(t.strong,{children:"Assembly"}),": WaaS.Core.dll"]}),"\n",(0,l.jsx)(t.h6,{id:"view-source",children:(0,l.jsx)(t.a,{href:"https://github.com/ruccho/WaaS/blob/feature/component/WaaS.Unity/Packages/com.ruccho.waas/Core/Scripts/Runtime/ExternalFunction.cs#L10",children:"View Source"})}),"\n",(0,l.jsx)(t.pre,{children:(0,l.jsx)(t.code,{className:"language-csharp",metastring:'title="Declaration"',children:"public abstract class ExternalFunction : IInvocableFunction, IExternal\n"})}),"\n",(0,l.jsxs)(t.p,{children:[(0,l.jsx)(t.strong,{children:"Derived:"}),(0,l.jsx)(t.br,{}),"\n",(0,l.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Runtime/ExternalFunctionCoreBoxedDelegate",children:"WaaS.Runtime.ExternalFunctionCoreBoxedDelegate"}),", ",(0,l.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Runtime/ExternalFunctionDelegate",children:"WaaS.Runtime.ExternalFunctionDelegate"}),", ",(0,l.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Runtime/ExternalFunctionPointer",children:"WaaS.Runtime.ExternalFunctionPointer"})]}),"\n",(0,l.jsxs)(t.p,{children:[(0,l.jsx)(t.strong,{children:"Implements:"}),(0,l.jsx)(t.br,{}),"\n",(0,l.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Runtime/IInvocableFunction",children:"WaaS.Runtime.IInvocableFunction"}),", ",(0,l.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Runtime/IExternal",children:"WaaS.Runtime.IExternal"})]}),"\n",(0,l.jsx)(t.h2,{id:"properties",children:"Properties"}),"\n",(0,l.jsx)(t.h3,{id:"type",children:"Type"}),"\n",(0,l.jsx)(t.h6,{id:"view-source-1",children:(0,l.jsx)(t.a,{href:"https://github.com/ruccho/WaaS/blob/feature/component/WaaS.Unity/Packages/com.ruccho.waas/Core/Scripts/Runtime/ExternalFunction.cs#L12",children:"View Source"})}),"\n",(0,l.jsx)(t.pre,{children:(0,l.jsx)(t.code,{className:"language-csharp",metastring:'title="Declaration"',children:"public abstract FunctionType Type { get; }\n"})}),"\n",(0,l.jsx)(t.h2,{id:"methods",children:"Methods"}),"\n",(0,l.jsx)(t.h3,{id:"createframeexecutioncontext-readonlyspanstackvalueitem",children:"CreateFrame(ExecutionContext, ReadOnlySpan<StackValueItem>)"}),"\n",(0,l.jsx)(t.h6,{id:"view-source-2",children:(0,l.jsx)(t.a,{href:"https://github.com/ruccho/WaaS/blob/feature/component/WaaS.Unity/Packages/com.ruccho.waas/Core/Scripts/Runtime/ExternalFunction.cs#L14",children:"View Source"})}),"\n",(0,l.jsx)(t.pre,{children:(0,l.jsx)(t.code,{className:"language-csharp",metastring:'title="Declaration"',children:"public StackFrame CreateFrame(ExecutionContext context, ReadOnlySpan<StackValueItem> inputValues)\n"})}),"\n",(0,l.jsx)(t.h5,{id:"returns",children:"Returns"}),"\n",(0,l.jsx)(t.p,{children:(0,l.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Runtime/StackFrame",children:"WaaS.Runtime.StackFrame"})}),"\n",(0,l.jsx)(t.h5,{id:"parameters",children:"Parameters"}),"\n",(0,l.jsxs)(t.table,{children:[(0,l.jsx)(t.thead,{children:(0,l.jsxs)(t.tr,{children:[(0,l.jsx)(t.th,{style:{textAlign:"left"},children:"Type"}),(0,l.jsx)(t.th,{style:{textAlign:"left"},children:"Name"})]})}),(0,l.jsxs)(t.tbody,{children:[(0,l.jsxs)(t.tr,{children:[(0,l.jsx)(t.td,{style:{textAlign:"left"},children:(0,l.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Runtime/ExecutionContext",children:"WaaS.Runtime.ExecutionContext"})}),(0,l.jsx)(t.td,{style:{textAlign:"left"},children:(0,l.jsx)(t.em,{children:"context"})})]}),(0,l.jsxs)(t.tr,{children:[(0,l.jsx)(t.td,{style:{textAlign:"left"},children:(0,l.jsx)(t.code,{children:"System.ReadOnlySpan<WaaS.Runtime.StackValueItem>"})}),(0,l.jsx)(t.td,{style:{textAlign:"left"},children:(0,l.jsx)(t.em,{children:"inputValues"})})]})]})]}),"\n",(0,l.jsx)(t.h3,{id:"invokeexecutioncontext-readonlyspanstackvalueitem-spanstackvalueitem",children:"Invoke(ExecutionContext, ReadOnlySpan<StackValueItem>, Span<StackValueItem>)"}),"\n",(0,l.jsx)(t.h6,{id:"view-source-3",children:(0,l.jsx)(t.a,{href:"https://github.com/ruccho/WaaS/blob/feature/component/WaaS.Unity/Packages/com.ruccho.waas/Core/Scripts/Runtime/ExternalFunction.cs#L19",children:"View Source"})}),"\n",(0,l.jsx)(t.pre,{children:(0,l.jsx)(t.code,{className:"language-csharp",metastring:'title="Declaration"',children:"public abstract void Invoke(ExecutionContext context, ReadOnlySpan<StackValueItem> parameters, Span<StackValueItem> results)\n"})}),"\n",(0,l.jsx)(t.h5,{id:"parameters-1",children:"Parameters"}),"\n",(0,l.jsxs)(t.table,{children:[(0,l.jsx)(t.thead,{children:(0,l.jsxs)(t.tr,{children:[(0,l.jsx)(t.th,{style:{textAlign:"left"},children:"Type"}),(0,l.jsx)(t.th,{style:{textAlign:"left"},children:"Name"})]})}),(0,l.jsxs)(t.tbody,{children:[(0,l.jsxs)(t.tr,{children:[(0,l.jsx)(t.td,{style:{textAlign:"left"},children:(0,l.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Runtime/ExecutionContext",children:"WaaS.Runtime.ExecutionContext"})}),(0,l.jsx)(t.td,{style:{textAlign:"left"},children:(0,l.jsx)(t.em,{children:"context"})})]}),(0,l.jsxs)(t.tr,{children:[(0,l.jsx)(t.td,{style:{textAlign:"left"},children:(0,l.jsx)(t.code,{children:"System.ReadOnlySpan<WaaS.Runtime.StackValueItem>"})}),(0,l.jsx)(t.td,{style:{textAlign:"left"},children:(0,l.jsx)(t.em,{children:"parameters"})})]}),(0,l.jsxs)(t.tr,{children:[(0,l.jsx)(t.td,{style:{textAlign:"left"},children:(0,l.jsx)(t.code,{children:"System.Span<WaaS.Runtime.StackValueItem>"})}),(0,l.jsx)(t.td,{style:{textAlign:"left"},children:(0,l.jsx)(t.em,{children:"results"})})]})]})]}),"\n",(0,l.jsx)(t.h2,{id:"implements",children:"Implements"}),"\n",(0,l.jsxs)(t.ul,{children:["\n",(0,l.jsx)(t.li,{children:(0,l.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Runtime/IInvocableFunction",children:"WaaS.Runtime.IInvocableFunction"})}),"\n",(0,l.jsx)(t.li,{children:(0,l.jsx)(t.a,{href:"/WaaS/ja/api/WaaS.Runtime/IExternal",children:"WaaS.Runtime.IExternal"})}),"\n"]})]})}function u(e={}){const{wrapper:t}={...(0,i.R)(),...e.components};return t?(0,l.jsx)(t,{...e,children:(0,l.jsx)(d,{...e})}):d(e)}},28453:(e,t,n)=>{n.d(t,{R:()=>r,x:()=>s});var a=n(96540);const l={},i=a.createContext(l);function r(e){const t=a.useContext(i);return a.useMemo((function(){return"function"==typeof e?e(t):{...t,...e}}),[t,e])}function s(e){let t;return t=e.disableParentContext?"function"==typeof e.components?e.components(l):e.components||l:r(e.components),a.createElement(i.Provider,{value:t},e.children)}}}]);