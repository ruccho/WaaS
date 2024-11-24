"use strict";(self.webpackChunkdocs=self.webpackChunkdocs||[]).push([[88983],{29179:(e,t,n)=>{n.r(t),n.d(t,{assets:()=>l,contentTitle:()=>a,default:()=>m,frontMatter:()=>c,metadata:()=>r,toc:()=>u});const r=JSON.parse('{"id":"getting-started/index","title":"Getting Started","description":"","source":"@site/docs/getting-started/index.mdx","sourceDirName":"getting-started","slug":"/getting-started/","permalink":"/WaaS/getting-started/","draft":false,"unlisted":false,"editUrl":"https://github.com/ruccho/WaaS/tree/main/docs/docs/getting-started/index.mdx","tags":[],"version":"current","sidebarPosition":2,"frontMatter":{"title":"Getting Started","sidebar_position":2},"sidebar":"tutorialSidebar","previous":{"title":"Wasm Specifications","permalink":"/WaaS/core/spec"},"next":{"title":"Installation","permalink":"/WaaS/getting-started/installation"}}');var s=n(74848),i=n(28453),o=n(5871);const c={title:"Getting Started",sidebar_position:2},a=void 0,l={},u=[];function d(e){return(0,s.jsx)(o.A,{})}function m(e={}){const{wrapper:t}={...(0,i.R)(),...e.components};return t?(0,s.jsx)(t,{...e,children:(0,s.jsx)(d,{...e})}):d()}},5871:(e,t,n)=>{n.d(t,{A:()=>S});var r=n(96540),s=n(34164),i=n(44718),o=n(28774),c=n(44586);const a=["zero","one","two","few","many","other"];function l(e){return a.filter((t=>e.includes(t)))}const u={locale:"en",pluralForms:l(["one","other"]),select:e=>1===e?"one":"other"};function d(){const{i18n:{currentLocale:e}}=(0,c.A)();return(0,r.useMemo)((()=>{try{return function(e){const t=new Intl.PluralRules(e);return{locale:e,pluralForms:l(t.resolvedOptions().pluralCategories),select:e=>t.select(e)}}(e)}catch(t){return console.error(`Failed to use Intl.PluralRules for locale "${e}".\nDocusaurus will fallback to the default (English) implementation.\nError: ${t.message}\n`),u}}),[e])}function m(){const e=d();return{selectMessage:(t,n)=>function(e,t,n){const r=e.split("|");if(1===r.length)return r[0];r.length>n.pluralForms.length&&console.error(`For locale=${n.locale}, a maximum of ${n.pluralForms.length} plural forms are expected (${n.pluralForms.join(",")}), but the message contains ${r.length}: ${e}`);const s=n.select(t),i=n.pluralForms.indexOf(s);return r[Math.min(i,r.length-1)]}(n,t,e)}}var f=n(16654),p=n(21312),h=n(51107);const g={cardContainer:"cardContainer_fWXF",cardTitle:"cardTitle_rnsV",cardDescription:"cardDescription_PWke"};var x=n(74848);function j(e){let{href:t,children:n}=e;return(0,x.jsx)(o.A,{href:t,className:(0,s.A)("card padding--lg",g.cardContainer),children:n})}function b(e){let{href:t,icon:n,title:r,description:i}=e;return(0,x.jsxs)(j,{href:t,children:[(0,x.jsxs)(h.A,{as:"h2",className:(0,s.A)("text--truncate",g.cardTitle),title:r,children:[n," ",r]}),i&&(0,x.jsx)("p",{className:(0,s.A)("text--truncate",g.cardDescription),title:i,children:i})]})}function w(e){let{item:t}=e;const n=(0,i.Nr)(t),r=function(){const{selectMessage:e}=m();return t=>e(t,(0,p.T)({message:"1 item|{count} items",id:"theme.docs.DocCard.categoryDescription.plurals",description:"The default description for a category card in the generated index about how many items this category includes"},{count:t}))}();return n?(0,x.jsx)(b,{href:n,icon:"\ud83d\uddc3\ufe0f",title:t.label,description:t.description??r(t.items.length)}):null}function v(e){let{item:t}=e;const n=(0,f.A)(t.href)?"\ud83d\udcc4\ufe0f":"\ud83d\udd17",r=(0,i.cC)(t.docId??void 0);return(0,x.jsx)(b,{href:t.href,icon:n,title:t.label,description:t.description??r?.description})}function y(e){let{item:t}=e;switch(t.type){case"link":return(0,x.jsx)(v,{item:t});case"category":return(0,x.jsx)(w,{item:t});default:throw new Error(`unknown item type ${JSON.stringify(t)}`)}}function N(e){let{className:t}=e;const n=(0,i.$S)();return(0,x.jsx)(S,{items:n.items,className:t})}function S(e){const{items:t,className:n}=e;if(!t)return(0,x.jsx)(N,{...e});const r=(0,i.d1)(t);return(0,x.jsx)("section",{className:(0,s.A)("row",n),children:r.map(((e,t)=>(0,x.jsx)("article",{className:"col col--6 margin-bottom--lg",children:(0,x.jsx)(y,{item:e})},t)))})}},28453:(e,t,n)=>{n.d(t,{R:()=>o,x:()=>c});var r=n(96540);const s={},i=r.createContext(s);function o(e){const t=r.useContext(i);return r.useMemo((function(){return"function"==typeof e?e(t):{...t,...e}}),[t,e])}function c(e){let t;return t=e.disableParentContext?"function"==typeof e.components?e.components(s):e.components||s:o(e.components),r.createElement(i.Provider,{value:t},e.children)}}}]);