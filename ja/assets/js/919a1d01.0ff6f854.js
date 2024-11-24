"use strict";(self.webpackChunkdocs=self.webpackChunkdocs||[]).push([[56613],{67225:(e,n,r)=>{r.r(n),r.d(n,{assets:()=>i,contentTitle:()=>o,default:()=>p,frontMatter:()=>c,metadata:()=>t,toc:()=>d});const t=JSON.parse('{"id":"component-model/binding-generator/use-generated-code","title":"\u751f\u6210\u3055\u308c\u305f\u30b3\u30fc\u30c9\u3092\u5229\u7528\u3059\u308b","description":"\u30a4\u30f3\u30bf\u30fc\u30d5\u30a7\u30fc\u30b9\u5b9f\u88c5\u3092\u30b3\u30f3\u30dd\u30fc\u30cd\u30f3\u30c8\u306b\u516c\u958b\u3059\u308b","source":"@site/i18n/ja/docusaurus-plugin-content-docs/current/component-model/binding-generator/use-generated-code.mdx","sourceDirName":"component-model/binding-generator","slug":"/component-model/binding-generator/use-generated-code","permalink":"/WaaS/ja/component-model/binding-generator/use-generated-code","draft":false,"unlisted":false,"editUrl":"https://github.com/ruccho/WaaS/tree/main/docs/docs/component-model/binding-generator/use-generated-code.mdx","tags":[],"version":"current","sidebarPosition":2,"frontMatter":{"title":"\u751f\u6210\u3055\u308c\u305f\u30b3\u30fc\u30c9\u3092\u5229\u7528\u3059\u308b","sidebar_position":2},"sidebar":"tutorialSidebar","previous":{"title":"\u5c5e\u6027\u306e\u4ed8\u4e0e","permalink":"/WaaS/ja/component-model/binding-generator/attributes"}}');var s=r(74848),a=r(28453);const c={title:"\u751f\u6210\u3055\u308c\u305f\u30b3\u30fc\u30c9\u3092\u5229\u7528\u3059\u308b",sidebar_position:2},o=void 0,i={},d=[{value:"\u30a4\u30f3\u30bf\u30fc\u30d5\u30a7\u30fc\u30b9\u5b9f\u88c5\u3092\u30b3\u30f3\u30dd\u30fc\u30cd\u30f3\u30c8\u306b\u516c\u958b\u3059\u308b",id:"\u30a4\u30f3\u30bf\u30fc\u30d5\u30a7\u30fc\u30b9\u5b9f\u88c5\u3092\u30b3\u30f3\u30dd\u30fc\u30cd\u30f3\u30c8\u306b\u516c\u958b\u3059\u308b",level:3},{value:"\u6240\u6709\u6a29\u306e\u30cf\u30f3\u30c9\u30ea\u30f3\u30b0",id:"\u6240\u6709\u6a29\u306e\u30cf\u30f3\u30c9\u30ea\u30f3\u30b0",level:4},{value:"\u30ea\u30bd\u30fc\u30b9\u3092 C# \u306e\u30aa\u30d6\u30b8\u30a7\u30af\u30c8\u306b\u7d10\u3065\u3051\u308b",id:"\u30ea\u30bd\u30fc\u30b9\u3092-c-\u306e\u30aa\u30d6\u30b8\u30a7\u30af\u30c8\u306b\u7d10\u3065\u3051\u308b",level:3},{value:"\u30b3\u30f3\u30dd\u30fc\u30cd\u30f3\u30c8\u304c\u516c\u958b\u3059\u308b\u95a2\u6570\u3092\u547c\u3073\u51fa\u3059",id:"\u30b3\u30f3\u30dd\u30fc\u30cd\u30f3\u30c8\u304c\u516c\u958b\u3059\u308b\u95a2\u6570\u3092\u547c\u3073\u51fa\u3059",level:3}];function l(e){const n={admonition:"admonition",br:"br",code:"code",h3:"h3",h4:"h4",li:"li",p:"p",pre:"pre",ul:"ul",...(0,a.R)(),...e.components};return(0,s.jsxs)(s.Fragment,{children:[(0,s.jsx)(n.h3,{id:"\u30a4\u30f3\u30bf\u30fc\u30d5\u30a7\u30fc\u30b9\u5b9f\u88c5\u3092\u30b3\u30f3\u30dd\u30fc\u30cd\u30f3\u30c8\u306b\u516c\u958b\u3059\u308b",children:"\u30a4\u30f3\u30bf\u30fc\u30d5\u30a7\u30fc\u30b9\u5b9f\u88c5\u3092\u30b3\u30f3\u30dd\u30fc\u30cd\u30f3\u30c8\u306b\u516c\u958b\u3059\u308b"}),"\n",(0,s.jsxs)(n.p,{children:["\u30a4\u30f3\u30bf\u30fc\u30d5\u30a7\u30fc\u30b9\u306e\u30e1\u30f3\u30d0\u3068\u3057\u3066\u9759\u7684\u30e1\u30bd\u30c3\u30c9 ",(0,s.jsx)(n.code,{children:"CreateWaaSInstance()"})," \u304c\u5b9a\u7fa9\u3055\u308c\u308b\u306e\u3067\u3001\u3053\u3061\u3089\u3092\u4f7f\u3044\u307e\u3059\u3002"]}),"\n",(0,s.jsx)(n.pre,{children:(0,s.jsx)(n.code,{className:"language-csharp",children:"// <auto-generated />\r\n\r\npartial interface IEnv\r\n{\r\n    public static IInstance CreateWaaSInstance(IEnv target);\r\n}\n"})}),"\n",(0,s.jsxs)(n.p,{children:[(0,s.jsx)(n.code,{children:"CreateWaaSInstance()"}),"\u306f\u3001C# \u3067\u5b9f\u88c5\u3055\u308c\u305f\u30a4\u30f3\u30bf\u30fc\u30d5\u30a7\u30fc\u30b9\u3092\u30e9\u30c3\u30d7\u3057\u3066 ",(0,s.jsx)(n.code,{children:"IInstance"})," \u306b\u5909\u63db\u3059\u308b\u30e1\u30bd\u30c3\u30c9\u3067\u3059\u3002",(0,s.jsx)(n.br,{}),"\n",(0,s.jsx)(n.code,{children:"IInstance"})," \u306b\u5909\u63db\u3059\u308b\u3053\u3068\u3067\u3001\u30b3\u30f3\u30dd\u30fc\u30cd\u30f3\u30c8\u306e\u30a4\u30f3\u30b9\u30bf\u30f3\u30b9\u5316\u6642\u306b\u30a4\u30f3\u30dd\u30fc\u30c8\u3067\u304d\u308b\u3088\u3046\u306b\u306a\u308a\u307e\u3059\u3002",(0,s.jsx)(n.br,{}),"\n","\u30b3\u30f3\u30dd\u30fc\u30cd\u30f3\u30c8\u306b\u5fc5\u8981\u306a\u5b9f\u88c5\u3092 C# \u5074\u304b\u3089\u63d0\u4f9b\u3057\u305f\u3044\u5834\u5408\u306b\u4f7f\u7528\u3067\u304d\u307e\u3059\u3002"]}),"\n",(0,s.jsx)(n.pre,{children:(0,s.jsx)(n.code,{className:"language-csharp",children:'var component = LoadComponent();\r\n\r\nvar instance = component.Instantiate(null, new Dictionary<string, ISortedExportable>()\r\n{\r\n    { "my-game:my-sequencer/env", IEnv.CreateWaaSInstance(new EnvImpl()) }\r\n});\r\n\r\npublic class EnvImpl : IEnv { /* \u3053\u3053\u3092\u5b9f\u88c5 */ }\n'})}),"\n",(0,s.jsx)(n.h4,{id:"\u6240\u6709\u6a29\u306e\u30cf\u30f3\u30c9\u30ea\u30f3\u30b0",children:"\u6240\u6709\u6a29\u306e\u30cf\u30f3\u30c9\u30ea\u30f3\u30b0"}),"\n",(0,s.jsxs)(n.p,{children:["\u5f15\u6570\u3068\u3057\u3066 ",(0,s.jsx)(n.code,{children:"Owned<T>"})," \u304c C# \u30b3\u30fc\u30c9\u306b\u6e21\u3055\u308c\u308b\u5834\u5408\u3001\u4ee5\u4e0b\u306e\u3044\u305a\u308c\u304b\u3092\u884c\u3046\u5fc5\u8981\u304c\u3042\u308a\u307e\u3059\u3002"]}),"\n",(0,s.jsxs)(n.ul,{children:["\n",(0,s.jsxs)(n.li,{children:[(0,s.jsx)(n.code,{children:"Owned<T>.Dispose()"})," \u306b\u3088\u308b\u6240\u6709\u6a29\u306e\u7834\u68c4"]}),"\n",(0,s.jsxs)(n.li,{children:[(0,s.jsx)(n.code,{children:"Owned<T>"})," \u3092\u3055\u3089\u306b\u4ed6\u306e\u95a2\u6570\u306b\u6e21\u3059\u3053\u3068\u306b\u3088\u308b\u6240\u6709\u6a29\u306e\u79fb\u8b72"]}),"\n"]}),"\n",(0,s.jsx)(n.p,{children:"\u3053\u308c\u3092\u884c\u308f\u306a\u3044\u5834\u5408\u3001\u30ea\u30bd\u30fc\u30b9\u304c\u30ea\u30fc\u30af\u3057\u307e\u3059\u3002"}),"\n",(0,s.jsx)(n.h3,{id:"\u30ea\u30bd\u30fc\u30b9\u3092-c-\u306e\u30aa\u30d6\u30b8\u30a7\u30af\u30c8\u306b\u7d10\u3065\u3051\u308b",children:"\u30ea\u30bd\u30fc\u30b9\u3092 C# \u306e\u30aa\u30d6\u30b8\u30a7\u30af\u30c8\u306b\u7d10\u3065\u3051\u308b"}),"\n",(0,s.jsx)(n.p,{children:"\u30ea\u30bd\u30fc\u30b9\u578b\u306e\u5b9f\u88c5\u3092 C# \u5074\u3067\u63d0\u4f9b\u3059\u308b\u5834\u5408\u3001\u30ea\u30bd\u30fc\u30b9\u3092 C# \u306e\u30aa\u30d6\u30b8\u30a7\u30af\u30c8\u306b\u7d10\u3065\u3051\u308b\u3053\u3068\u304c\u3067\u304d\u307e\u3059\u3002"}),"\n",(0,s.jsxs)(n.p,{children:["\u30ea\u30bd\u30fc\u30b9\u3092 C# \u306e\u30aa\u30d6\u30b8\u30a7\u30af\u30c8\u306b\u7d10\u3065\u3051\u308b\u306b\u306f\u3001\u30ea\u30bd\u30fc\u30b9\u578b\u306e\u5b9f\u88c5\u30af\u30e9\u30b9\u3067 ",(0,s.jsx)(n.code,{children:"HostResourceTypeBase<T>"})," \u3092\u7d99\u627f\u3057\u307e\u3059\u3002\r\n",(0,s.jsx)(n.code,{children:"Wrap()"})," \u307e\u305f\u306f ",(0,s.jsx)(n.code,{children:"Unwrap()"})," \u30e1\u30bd\u30c3\u30c9\u3092\u4f7f\u7528\u3057\u3066\u3001\u30ea\u30bd\u30fc\u30b9\u578b\u3068 C# \u306e\u30aa\u30d6\u30b8\u30a7\u30af\u30c8\u3092\u5909\u63db\u3067\u304d\u307e\u3059\u3002"]}),"\n",(0,s.jsx)(n.pre,{children:(0,s.jsx)(n.code,{className:"language-csharp",children:'using WaaS.ComponentModel.Binding;\r\nusing WaaS.ComponentModel.Runtime;\r\nusing System;\r\nusing System.IO;\r\n\r\n[ComponentInterface(@"env")]\r\npublic partial interface IEnv\r\n{\r\n    [ComponentResource(@"stream")]\r\n    public partial interface IStreamResourceImpl : IResourceImpl\r\n    {\r\n        [ComponentApi("[constructor]stream")]\r\n        Owned<IStreamResourceImpl> Create();\r\n\r\n        [ComponentApi("[method]stream.write-byte")]\r\n        void WriteByte(Borrowed<IStreamResourceImpl> self, byte value);\r\n    }\r\n}\r\n\r\npublic class StreamResourceType : HostResourceTypeBase<Stream>, IEnv.IStreamResourceImpl\r\n{\r\n    public Owned<IEnv.IStreamResourceImpl> Create()\r\n    {\r\n        var stream = File.Open("hoge.txt", FileMode.Create, FileAccess.Write);\r\n\r\n        // Stream -> Owned<IStreamResourceImpl>\r\n        var handle = Wrap<IEnv.IStreamResourceImpl>(stream);\r\n\r\n        return handle;\r\n    }\r\n\r\n    public void WriteByte(Borrowed<IEnv.IStreamResourceImpl> self, byte value)\r\n    {\r\n        // Borrowed<IStreamResourceImpl> -> Stream\r\n        var stream = Unwrap(self);\r\n\r\n        stream.WriteByte(value);\r\n    }\r\n\r\n    public IResourceType Type => this;\r\n}\r\n\r\npublic class EnvImpl : IEnv\r\n{\r\n    public IEnv.IStreamResourceImpl Stream { get; } = new StreamResourceType();\r\n}\n'})}),"\n",(0,s.jsx)(n.admonition,{type:"warning",children:(0,s.jsxs)(n.p,{children:[(0,s.jsx)(n.code,{children:"Wrap()"})," \u3067 ",(0,s.jsx)(n.code,{children:"Owned<T>"})," \u3092\u4f5c\u6210\u3059\u308b\u3068\u3001\u5185\u90e8\u306e\u30c6\u30fc\u30d6\u30eb\u304b\u3089\u5bfe\u8c61\u306e\u30aa\u30d6\u30b8\u30a7\u30af\u30c8\u304c\u53c2\u7167\u3055\u308c\u307e\u3059\u3002",(0,s.jsx)(n.br,{}),"\n",(0,s.jsx)(n.code,{children:"Owned<T>"})," \u3092\u78ba\u5b9f\u306b\u623b\u308a\u5024\u3068\u3057\u3066\u8fd4\u3059\u3053\u3068\u3067\u3001\u305d\u306e\u5f8c\u30ea\u30bd\u30fc\u30b9\u304c\u4e0d\u8981\u306b\u306a\u3063\u305f\u30bf\u30a4\u30df\u30f3\u30b0\u3067\u53c2\u7167\u304c\u89e3\u653e\u3055\u308c\u307e\u3059\u3002",(0,s.jsx)(n.code,{children:"Owned<T>"})," \u3092\u4f5c\u6210\u3057\u305f\u307e\u307e\u8fd4\u3055\u305a\u306b\u3044\u308b\u3068\u30e1\u30e2\u30ea\u30ea\u30fc\u30af\u3059\u308b\u306e\u3067\u6ce8\u610f\u3057\u3066\u304f\u3060\u3055\u3044\u3002"]})}),"\n",(0,s.jsx)(n.h3,{id:"\u30b3\u30f3\u30dd\u30fc\u30cd\u30f3\u30c8\u304c\u516c\u958b\u3059\u308b\u95a2\u6570\u3092\u547c\u3073\u51fa\u3059",children:"\u30b3\u30f3\u30dd\u30fc\u30cd\u30f3\u30c8\u304c\u516c\u958b\u3059\u308b\u95a2\u6570\u3092\u547c\u3073\u51fa\u3059"}),"\n",(0,s.jsxs)(n.p,{children:["\u30a4\u30f3\u30bf\u30fc\u30d5\u30a7\u30fc\u30b9\u306e\u30e1\u30f3\u30d0\u3068\u3057\u3066\u3001\u69cb\u9020\u4f53 ",(0,s.jsx)(n.code,{children:"Wrapper"})," \u304c\u5b9a\u7fa9\u3055\u308c\u308b\u306e\u3067\u3001\u3053\u3061\u3089\u3092\u4f7f\u3044\u307e\u3059\u3002"]}),"\n",(0,s.jsx)(n.pre,{children:(0,s.jsx)(n.code,{className:"language-csharp",children:"// <auto-generated />\r\n\r\npartial interface IEnv\r\n{\r\n    public readonly struct Wrapper : IEnv\r\n    {\r\n        public Wrapper(IInstance instance, ExecutionContext context);\r\n\r\n        public ValueTask ShowMessage(string @speaker, string @message);\r\n        public ValueTask<uint> ShowOptions(ReadOnlyMemory<string> @options)\r\n    }\r\n}\n"})}),"\n",(0,s.jsxs)(n.p,{children:[(0,s.jsx)(n.code,{children:"Wrapper"})," \u306f\u3001\u5916\u90e8\u3067\u4f5c\u6210\u3057\u305f ",(0,s.jsx)(n.code,{children:"IInstance"})," \u3092\u30e9\u30c3\u30d7\u3057\u3001",(0,s.jsx)(n.code,{children:"IEnv"})," \u306b\u5909\u63db\u3059\u308b\u69cb\u9020\u4f53\u3067\u3059\u3002",(0,s.jsx)(n.br,{}),"\n","\u5bfe\u8c61\u306e\u30a4\u30f3\u30b9\u30bf\u30f3\u30b9\u304c ",(0,s.jsx)(n.code,{children:"IEnv"})," \u306b\u5bfe\u5fdc\u3059\u308b\u95a2\u6570\u7b49\u3092\u30a8\u30af\u30b9\u30dd\u30fc\u30c8\u3057\u3066\u3044\u308b\u3053\u3068\u304c\u524d\u63d0\u3068\u306a\u308a\u307e\u3059\uff08\u81ea\u52d5\u7684\u306a\u30c1\u30a7\u30c3\u30af\u306f\u884c\u308f\u308c\u307e\u305b\u3093\uff09\u3002",(0,s.jsx)(n.br,{}),"\n","\u30b3\u30f3\u30dd\u30fc\u30cd\u30f3\u30c8\u304c\u30a8\u30af\u30b9\u30dd\u30fc\u30c8\u3059\u308b\u95a2\u6570\u3092 C# \u304b\u3089\u547c\u3073\u51fa\u3059\u969b\u306b\u4f7f\u7528\u3067\u304d\u307e\u3059\u3002"]}),"\n",(0,s.jsx)(n.pre,{children:(0,s.jsx)(n.code,{className:"language-csharp",children:'var component = await componentAsset.LoadComponentAsync();\r\n\r\nvar instance = component.Instantiate(null, new Dictionary<string, ISortedExportable>());\r\n\r\nusing var context = new ExecutionContext();\r\n\r\nvar wrapper = new IEnv.Wrapper(instance, context);\r\n\r\nawait wraapper.ShowMessage("\u307c\u304f", "\u3053\u3093\u306b\u3061\u306f");\n'})})]})}function p(e={}){const{wrapper:n}={...(0,a.R)(),...e.components};return n?(0,s.jsx)(n,{...e,children:(0,s.jsx)(l,{...e})}):l(e)}},28453:(e,n,r)=>{r.d(n,{R:()=>c,x:()=>o});var t=r(96540);const s={},a=t.createContext(s);function c(e){const n=t.useContext(a);return t.useMemo((function(){return"function"==typeof e?e(n):{...n,...e}}),[n,e])}function o(e){let n;return n=e.disableParentContext?"function"==typeof e.components?e.components(s):e.components||s:c(e.components),t.createElement(a.Provider,{value:n},e.children)}}}]);