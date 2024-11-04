"use strict";(self.webpackChunkdocs=self.webpackChunkdocs||[]).push([[559],{2535:(e,t,n)=>{n.r(t),n.d(t,{assets:()=>l,contentTitle:()=>a,default:()=>m,frontMatter:()=>s,metadata:()=>c,toc:()=>d});var r=n(4848),o=n(8453),i=n(5871);const s={title:"Binding Generator",sidebar_position:3},a=void 0,c={id:"component-model/binding-generator/index",title:"Binding Generator",description:"",source:"@site/i18n/ja/docusaurus-plugin-content-docs/current/component-model/binding-generator/index.mdx",sourceDirName:"component-model/binding-generator",slug:"/component-model/binding-generator/",permalink:"/WaaS/ja/component-model/binding-generator/",draft:!1,unlisted:!1,editUrl:"https://github.com/ruccho/WaaS/tree/main/docs/component-model/binding-generator/index.mdx",tags:[],version:"current",sidebarPosition:3,frontMatter:{title:"Binding Generator",sidebar_position:3},sidebar:"tutorialSidebar",previous:{title:"wit2waas \u306e\u4f7f\u7528",permalink:"/WaaS/ja/component-model/wit2waas"},next:{title:"\u5c5e\u6027\u306e\u4ed8\u4e0e",permalink:"/WaaS/ja/component-model/binding-generator/attributes"}},l={},d=[];function u(e){return(0,r.jsx)(i.A,{})}function m(e={}){const{wrapper:t}={...(0,o.R)(),...e.components};return t?(0,r.jsx)(t,{...e,children:(0,r.jsx)(u,{...e})}):u()}},5871:(e,t,n)=>{n.d(t,{A:()=>A});var r=n(6540),o=n(4164),i=n(4718),s=n(8774),a=n(4586);const c=["zero","one","two","few","many","other"];function l(e){return c.filter((t=>e.includes(t)))}const d={locale:"en",pluralForms:l(["one","other"]),select:e=>1===e?"one":"other"};function u(){const{i18n:{currentLocale:e}}=(0,a.A)();return(0,r.useMemo)((()=>{try{return function(e){const t=new Intl.PluralRules(e);return{locale:e,pluralForms:l(t.resolvedOptions().pluralCategories),select:e=>t.select(e)}}(e)}catch(t){return console.error(`Failed to use Intl.PluralRules for locale "${e}".\nDocusaurus will fallback to the default (English) implementation.\nError: ${t.message}\n`),d}}),[e])}function m(){const e=u();return{selectMessage:(t,n)=>function(e,t,n){const r=e.split("|");if(1===r.length)return r[0];r.length>n.pluralForms.length&&console.error(`For locale=${n.locale}, a maximum of ${n.pluralForms.length} plural forms are expected (${n.pluralForms.join(",")}), but the message contains ${r.length}: ${e}`);const o=n.select(t),i=n.pluralForms.indexOf(o);return r[Math.min(i,r.length-1)]}(n,t,e)}}var p=n(6654),g=n(1312),h=n(1107);const f={cardContainer:"cardContainer_fWXF",cardTitle:"cardTitle_rnsV",cardDescription:"cardDescription_PWke"};var x=n(4848);function b(e){let{href:t,children:n}=e;return(0,x.jsx)(s.A,{href:t,className:(0,o.A)("card padding--lg",f.cardContainer),children:n})}function j(e){let{href:t,icon:n,title:r,description:i}=e;return(0,x.jsxs)(b,{href:t,children:[(0,x.jsxs)(h.A,{as:"h2",className:(0,o.A)("text--truncate",f.cardTitle),title:r,children:[n," ",r]}),i&&(0,x.jsx)("p",{className:(0,o.A)("text--truncate",f.cardDescription),title:i,children:i})]})}function w(e){let{item:t}=e;const n=(0,i.Nr)(t),r=function(){const{selectMessage:e}=m();return t=>e(t,(0,g.T)({message:"1 item|{count} items",id:"theme.docs.DocCard.categoryDescription.plurals",description:"The default description for a category card in the generated index about how many items this category includes"},{count:t}))}();return n?(0,x.jsx)(j,{href:n,icon:"\ud83d\uddc3\ufe0f",title:t.label,description:t.description??r(t.items.length)}):null}function k(e){let{item:t}=e;const n=(0,p.A)(t.href)?"\ud83d\udcc4\ufe0f":"\ud83d\udd17",r=(0,i.cC)(t.docId??void 0);return(0,x.jsx)(j,{href:t.href,icon:n,title:t.label,description:t.description??r?.description})}function N(e){let{item:t}=e;switch(t.type){case"link":return(0,x.jsx)(k,{item:t});case"category":return(0,x.jsx)(w,{item:t});default:throw new Error(`unknown item type ${JSON.stringify(t)}`)}}function y(e){let{className:t}=e;const n=(0,i.$S)();return(0,x.jsx)(A,{items:n.items,className:t})}function A(e){const{items:t,className:n}=e;if(!t)return(0,x.jsx)(y,{...e});const r=(0,i.d1)(t);return(0,x.jsx)("section",{className:(0,o.A)("row",n),children:r.map(((e,t)=>(0,x.jsx)("article",{className:"col col--6 margin-bottom--lg",children:(0,x.jsx)(N,{item:e})},t)))})}}}]);