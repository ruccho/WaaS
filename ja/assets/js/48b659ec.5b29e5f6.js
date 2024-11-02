"use strict";(self.webpackChunkdocs=self.webpackChunkdocs||[]).push([[393],{3689:(e,n,s)=>{s.r(n),s.d(n,{assets:()=>u,contentTitle:()=>l,default:()=>m,frontMatter:()=>a,metadata:()=>c,toc:()=>h});var r=s(4848),o=s(8453),d=(s(6540),s(4164)),t=s(7559);function i(e){let{children:n,className:s}=e;return(0,r.jsx)(r.Fragment,{children:(0,r.jsx)("span",{className:(0,d.A)(s,t.G.docs.docVersionBadge,"badge badge--secondary"),children:n})})}const a={title:"\u30e2\u30b8\u30e5\u30fc\u30eb\u306e\u30ed\u30fc\u30c9",sidebar_position:1},l=void 0,c={id:"core/module-loading",title:"\u30e2\u30b8\u30e5\u30fc\u30eb\u306e\u30ed\u30fc\u30c9",description:"\u57fa\u672c",source:"@site/i18n/ja/docusaurus-plugin-content-docs/current/core/module-loading.mdx",sourceDirName:"core",slug:"/core/module-loading",permalink:"/WaaS/ja/core/module-loading",draft:!1,unlisted:!1,editUrl:"https://github.com/ruccho/WaaS/tree/main/docs/core/module-loading.mdx",tags:[],version:"current",sidebarPosition:1,frontMatter:{title:"\u30e2\u30b8\u30e5\u30fc\u30eb\u306e\u30ed\u30fc\u30c9",sidebar_position:1},sidebar:"tutorialSidebar",previous:{title:"Core API",permalink:"/WaaS/ja/core/"},next:{title:"\u30a4\u30f3\u30b9\u30bf\u30f3\u30b9\u5316",permalink:"/WaaS/ja/core/instantiation"}},u={},h=[{value:"\u57fa\u672c",id:"\u57fa\u672c",level:2},{value:"\u30b9\u30c8\u30ea\u30fc\u30df\u30f3\u30b0",id:"\u30b9\u30c8\u30ea\u30fc\u30df\u30f3\u30b0",level:2},{value:"\u30a2\u30bb\u30c3\u30c8\u3068\u3057\u3066\u306e\u30ed\u30fc\u30c9 <Badge>Unity</Badge>",id:"\u30a2\u30bb\u30c3\u30c8\u3068\u3057\u3066\u306e\u30ed\u30fc\u30c9-unity",level:2}];function p(e){const n={br:"br",code:"code",h2:"h2",p:"p",pre:"pre",...(0,o.R)(),...e.components};return(0,r.jsxs)(r.Fragment,{children:[(0,r.jsx)(n.h2,{id:"\u57fa\u672c",children:"\u57fa\u672c"}),"\n",(0,r.jsxs)(n.p,{children:[(0,r.jsx)(n.code,{children:"Module.Create()"})," \u3092\u4f7f\u7528\u3057\u307e\u3059\u3002",(0,r.jsx)(n.br,{}),"\n",(0,r.jsx)(n.code,{children:"ReadOnlySpan<byte>"})," \u307e\u305f\u306f ",(0,r.jsx)(n.code,{children:"ReadOnlySequence<byte>"})," \u304b\u3089\u306e\u30ed\u30fc\u30c9\u304c\u53ef\u80fd\u3067\u3059\u3002"]}),"\n",(0,r.jsx)(n.pre,{children:(0,r.jsx)(n.code,{className:"language-csharp",children:'using WaaS.Runtime;\r\n\r\nSpan<byte> bytes = System.IO.File.LoadAllBytes("foo.wasm");\r\nvar module = Module.Create(bytes);\n'})}),"\n",(0,r.jsx)(n.h2,{id:"\u30b9\u30c8\u30ea\u30fc\u30df\u30f3\u30b0",children:"\u30b9\u30c8\u30ea\u30fc\u30df\u30f3\u30b0"}),"\n",(0,r.jsx)(n.p,{children:"\u73fe\u6642\u70b9\u3067\u306f\u672a\u5bfe\u5fdc\u3067\u3059\u3002"}),"\n",(0,r.jsxs)(n.h2,{id:"\u30a2\u30bb\u30c3\u30c8\u3068\u3057\u3066\u306e\u30ed\u30fc\u30c9-unity",children:["\u30a2\u30bb\u30c3\u30c8\u3068\u3057\u3066\u306e\u30ed\u30fc\u30c9 ",(0,r.jsx)(i,{children:"Unity"})]}),"\n",(0,r.jsxs)(n.p,{children:["\u62e1\u5f35\u5b50 ",(0,r.jsx)(n.code,{children:"*.wasm"})," \u3092\u3082\u3064\u30d5\u30a1\u30a4\u30eb\u3092 Unity \u30d7\u30ed\u30b8\u30a7\u30af\u30c8\u306b\u30a4\u30f3\u30dd\u30fc\u30c8\u3059\u308b\u3068\u3001\u81ea\u52d5\u7684\u306b ",(0,r.jsx)(n.code,{children:"WaaS.Unity.ModuleAsset"})," \u3068\u3057\u3066\u30ed\u30fc\u30c9\u3055\u308c\u307e\u3059\u3002"]}),"\n",(0,r.jsxs)(n.p,{children:["\u30e2\u30b8\u30e5\u30fc\u30eb\u306f ",(0,r.jsx)(n.code,{children:"[SerializeField]"})," \u3068\u3057\u3066\u30a2\u30b5\u30a4\u30f3\u3067\u304d\u3001",(0,r.jsx)(n.code,{children:"ModuleAsset.LoadModule()"})," \u307e\u305f\u306f ",(0,r.jsx)(n.code,{children:"ModuleAsset.LoadModuleAsync()"})," \u3067\u30ed\u30fc\u30c9\u3067\u304d\u307e\u3059\u3002"]}),"\n",(0,r.jsx)(n.pre,{children:(0,r.jsx)(n.code,{className:"language-csharp",children:"using UnityEngine;\r\nusing WaaS.Runtime;\r\nusing WaaS.Unity;\r\n\r\n[SerializeField] private ModuleAsset moduleAsset;\r\n\r\n// \u540c\u671f\r\nvar module = moduleAsset.LoadModule();\r\n\r\n// \u975e\u540c\u671f\r\nvar module = await ModuleAsset.LoadModuleAsync();\n"})}),"\n",(0,r.jsxs)(n.p,{children:[(0,r.jsx)(n.code,{children:"ModuleAsset"})," \u306e\u8a2d\u5b9a\u3067 ",(0,r.jsx)(n.code,{children:"Deserialize On Load"}),"\u3092\u6709\u52b9\u5316\u3059\u308b\u3068\u3001\u30a2\u30bb\u30c3\u30c8\u306e\u30ed\u30fc\u30c9\u6642\u306b\u540c\u671f\u7684\u306b\u30e2\u30b8\u30e5\u30fc\u30eb\u3092\u30d7\u30ea\u30ed\u30fc\u30c9\u3057\u307e\u3059\u3002",(0,r.jsx)(n.br,{}),"\n","\u3053\u308c\u306b\u3088\u3063\u3066\u3001",(0,r.jsx)(n.code,{children:"ModuleAsset.LoadModule()"})," \u306e\u5f85\u3061\u6642\u9593\u3092\u6e1b\u3089\u3059\u3053\u3068\u304c\u3067\u304d\u307e\u3059\u3002"]})]})}function m(e={}){const{wrapper:n}={...(0,o.R)(),...e.components};return n?(0,r.jsx)(n,{...e,children:(0,r.jsx)(p,{...e})}):p(e)}},8453:(e,n,s)=>{s.d(n,{R:()=>t,x:()=>i});var r=s(6540);const o={},d=r.createContext(o);function t(e){const n=r.useContext(d);return r.useMemo((function(){return"function"==typeof e?e(n):{...n,...e}}),[n,e])}function i(e){let n;return n=e.disableParentContext?"function"==typeof e.components?e.components(o):e.components||o:t(e.components),r.createElement(d.Provider,{value:n},e.children)}}}]);