"use strict";(self.webpackChunkdocs=self.webpackChunkdocs||[]).push([[50484],{50484:(t,e,a)=>{a.d(e,{diagram:()=>L});var i,n=a(69664),r=(a(79972),a(79740),a(6396),a(5081),a(34483),a(52294),a(62392),a(86825),a(85039)),d=a(45567),s=a(20007),o=a(62334),g=a(697),c={},p=(0,d.K2)(((t,e)=>{c[t]=e}),"set"),h=(0,d.K2)((t=>c[t]),"get"),l=(0,d.K2)((()=>Object.keys(c)),"keys"),x=(0,d.K2)((()=>l().length),"size"),D={get:h,set:p,keys:l,size:x},u=(0,d.K2)((t=>t.append("circle").attr("class","start-state").attr("r",(0,d.D7)().state.sizeUnit).attr("cx",(0,d.D7)().state.padding+(0,d.D7)().state.sizeUnit).attr("cy",(0,d.D7)().state.padding+(0,d.D7)().state.sizeUnit)),"drawStartState"),f=(0,d.K2)((t=>t.append("line").style("stroke","grey").style("stroke-dasharray","3").attr("x1",(0,d.D7)().state.textHeight).attr("class","divider").attr("x2",2*(0,d.D7)().state.textHeight).attr("y1",0).attr("y2",0)),"drawDivider"),y=(0,d.K2)(((t,e)=>{const a=t.append("text").attr("x",2*(0,d.D7)().state.padding).attr("y",(0,d.D7)().state.textHeight+2*(0,d.D7)().state.padding).attr("font-size",(0,d.D7)().state.fontSize).attr("class","state-title").text(e.id),i=a.node().getBBox();return t.insert("rect",":first-child").attr("x",(0,d.D7)().state.padding).attr("y",(0,d.D7)().state.padding).attr("width",i.width+2*(0,d.D7)().state.padding).attr("height",i.height+2*(0,d.D7)().state.padding).attr("rx",(0,d.D7)().state.radius),a}),"drawSimpleState"),w=(0,d.K2)(((t,e)=>{const a=(0,d.K2)((function(t,e,a){const i=t.append("tspan").attr("x",2*(0,d.D7)().state.padding).text(e);a||i.attr("dy",(0,d.D7)().state.textHeight)}),"addTspan"),i=t.append("text").attr("x",2*(0,d.D7)().state.padding).attr("y",(0,d.D7)().state.textHeight+1.3*(0,d.D7)().state.padding).attr("font-size",(0,d.D7)().state.fontSize).attr("class","state-title").text(e.descriptions[0]).node().getBBox(),n=i.height,r=t.append("text").attr("x",(0,d.D7)().state.padding).attr("y",n+.4*(0,d.D7)().state.padding+(0,d.D7)().state.dividerMargin+(0,d.D7)().state.textHeight).attr("class","state-description");let s=!0,o=!0;e.descriptions.forEach((function(t){s||(a(r,t,o),o=!1),s=!1}));const g=t.append("line").attr("x1",(0,d.D7)().state.padding).attr("y1",(0,d.D7)().state.padding+n+(0,d.D7)().state.dividerMargin/2).attr("y2",(0,d.D7)().state.padding+n+(0,d.D7)().state.dividerMargin/2).attr("class","descr-divider"),c=r.node().getBBox(),p=Math.max(c.width,i.width);return g.attr("x2",p+3*(0,d.D7)().state.padding),t.insert("rect",":first-child").attr("x",(0,d.D7)().state.padding).attr("y",(0,d.D7)().state.padding).attr("width",p+2*(0,d.D7)().state.padding).attr("height",c.height+n+2*(0,d.D7)().state.padding).attr("rx",(0,d.D7)().state.radius),t}),"drawDescrState"),m=(0,d.K2)(((t,e,a)=>{const i=(0,d.D7)().state.padding,n=2*(0,d.D7)().state.padding,r=t.node().getBBox(),s=r.width,o=r.x,g=t.append("text").attr("x",0).attr("y",(0,d.D7)().state.titleShift).attr("font-size",(0,d.D7)().state.fontSize).attr("class","state-title").text(e.id),c=g.node().getBBox().width+n;let p,h=Math.max(c,s);h===s&&(h+=n);const l=t.node().getBBox();e.doc,p=o-i,c>s&&(p=(s-h)/2+i),Math.abs(o-l.x)<i&&c>s&&(p=o-(c-s)/2);const x=1-(0,d.D7)().state.textHeight;return t.insert("rect",":first-child").attr("x",p).attr("y",x).attr("class",a?"alt-composit":"composit").attr("width",h).attr("height",l.height+(0,d.D7)().state.textHeight+(0,d.D7)().state.titleShift+1).attr("rx","0"),g.attr("x",p+i),c<=s&&g.attr("x",o+(h-n)/2-c/2+i),t.insert("rect",":first-child").attr("x",p).attr("y",(0,d.D7)().state.titleShift-(0,d.D7)().state.textHeight-(0,d.D7)().state.padding).attr("width",h).attr("height",3*(0,d.D7)().state.textHeight).attr("rx",(0,d.D7)().state.radius),t.insert("rect",":first-child").attr("x",p).attr("y",(0,d.D7)().state.titleShift-(0,d.D7)().state.textHeight-(0,d.D7)().state.padding).attr("width",h).attr("height",l.height+3+2*(0,d.D7)().state.textHeight).attr("rx",(0,d.D7)().state.radius),t}),"addTitleAndBox"),b=(0,d.K2)((t=>(t.append("circle").attr("class","end-state-outer").attr("r",(0,d.D7)().state.sizeUnit+(0,d.D7)().state.miniPadding).attr("cx",(0,d.D7)().state.padding+(0,d.D7)().state.sizeUnit+(0,d.D7)().state.miniPadding).attr("cy",(0,d.D7)().state.padding+(0,d.D7)().state.sizeUnit+(0,d.D7)().state.miniPadding),t.append("circle").attr("class","end-state-inner").attr("r",(0,d.D7)().state.sizeUnit).attr("cx",(0,d.D7)().state.padding+(0,d.D7)().state.sizeUnit+2).attr("cy",(0,d.D7)().state.padding+(0,d.D7)().state.sizeUnit+2))),"drawEndState"),B=(0,d.K2)(((t,e)=>{let a=(0,d.D7)().state.forkWidth,i=(0,d.D7)().state.forkHeight;if(e.parentId){let t=a;a=i,i=t}return t.append("rect").style("stroke","black").style("fill","black").attr("width",a).attr("height",i).attr("x",(0,d.D7)().state.padding).attr("y",(0,d.D7)().state.padding)}),"drawForkJoinState"),k=(0,d.K2)(((t,e,a,i)=>{let n=0;const r=i.append("text");r.style("text-anchor","start"),r.attr("class","noteText");let s=t.replace(/\r\n/g,"<br/>");s=s.replace(/\n/g,"<br/>");const o=s.split(d.Y2.lineBreakRegex);let g=1.25*(0,d.D7)().state.noteMargin;for(const c of o){const t=c.trim();if(t.length>0){const i=r.append("tspan");if(i.text(t),0===g){g+=i.node().getBBox().height}n+=g,i.attr("x",e+(0,d.D7)().state.noteMargin),i.attr("y",a+n+1.25*(0,d.D7)().state.noteMargin)}}return{textWidth:r.node().getBBox().width,textHeight:n}}),"_drawLongText"),S=(0,d.K2)(((t,e)=>{e.attr("class","state-note");const a=e.append("rect").attr("x",0).attr("y",(0,d.D7)().state.padding),i=e.append("g"),{textWidth:n,textHeight:r}=k(t,0,0,i);return a.attr("height",r+2*(0,d.D7)().state.noteMargin),a.attr("width",n+2*(0,d.D7)().state.noteMargin),a}),"drawNote"),N=(0,d.K2)((function(t,e){const a=e.id,i={id:a,label:e.id,width:0,height:0},n=t.append("g").attr("id",a).attr("class","stateGroup");"start"===e.type&&u(n),"end"===e.type&&b(n),"fork"!==e.type&&"join"!==e.type||B(n,e),"note"===e.type&&S(e.note.text,n),"divider"===e.type&&f(n),"default"===e.type&&0===e.descriptions.length&&y(n,e),"default"===e.type&&e.descriptions.length>0&&w(n,e);const r=n.node().getBBox();return i.width=r.width+2*(0,d.D7)().state.padding,i.height=r.height+2*(0,d.D7)().state.padding,D.set(a,i),i}),"drawState"),E=0,K=(0,d.K2)((function(t,e,a){const i=(0,d.K2)((function(t){switch(t){case n.iP.relationType.AGGREGATION:return"aggregation";case n.iP.relationType.EXTENSION:return"extension";case n.iP.relationType.COMPOSITION:return"composition";case n.iP.relationType.DEPENDENCY:return"dependency"}}),"getRelationType");e.points=e.points.filter((t=>!Number.isNaN(t.y)));const o=e.points,g=(0,s.n8j)().x((function(t){return t.x})).y((function(t){return t.y})).curve(s.qrM),c=t.append("path").attr("d",g(o)).attr("id","edge"+E).attr("class","transition");let p="";if((0,d.D7)().state.arrowMarkerAbsolute&&(p=window.location.protocol+"//"+window.location.host+window.location.pathname+window.location.search,p=p.replace(/\(/g,"\\("),p=p.replace(/\)/g,"\\)")),c.attr("marker-end","url("+p+"#"+i(n.iP.relationType.DEPENDENCY)+"End)"),void 0!==a.title){const i=t.append("g").attr("class","stateLabel"),{x:n,y:s}=r._K.calcLabelPosition(e.points),o=d.Y2.getRows(a.title);let g=0;const c=[];let p=0,h=0;for(let t=0;t<=o.length;t++){const e=i.append("text").attr("text-anchor","middle").text(o[t]).attr("x",n).attr("y",s+g),a=e.node().getBBox();if(p=Math.max(p,a.width),h=Math.min(h,a.x),d.Rm.info(a.x,n,s+g),0===g){const t=e.node().getBBox();g=t.height,d.Rm.info("Title height",g,s)}c.push(e)}let l=g*o.length;if(o.length>1){const t=(o.length-1)*g*.5;c.forEach(((e,a)=>e.attr("y",s+a*g-t))),l=g*o.length}const x=i.node().getBBox();i.insert("rect",":first-child").attr("class","box").attr("x",n-p/2-(0,d.D7)().state.padding/2).attr("y",s-l/2-(0,d.D7)().state.padding/2-3.5).attr("width",p+(0,d.D7)().state.padding).attr("height",l+(0,d.D7)().state.padding),d.Rm.info(x)}E++}),"drawEdge"),M={},v=(0,d.K2)((function(){}),"setConf"),R=(0,d.K2)((function(t){t.append("defs").append("marker").attr("id","dependencyEnd").attr("refX",19).attr("refY",7).attr("markerWidth",20).attr("markerHeight",28).attr("orient","auto").append("path").attr("d","M 19,7 L9,13 L14,7 L9,1 Z")}),"insertMarkers"),z=(0,d.K2)((function(t,e,a,n){i=(0,d.D7)().state;const r=(0,d.D7)().securityLevel;let o;"sandbox"===r&&(o=(0,s.Ltv)("#i"+e));const g="sandbox"===r?(0,s.Ltv)(o.nodes()[0].contentDocument.body):(0,s.Ltv)("body"),c="sandbox"===r?o.nodes()[0].contentDocument:document;d.Rm.debug("Rendering diagram "+t);const p=g.select(`[id='${e}']`);R(p);const h=n.db.getRootDoc();T(h,p,void 0,!1,g,c,n);const l=i.padding,x=p.node().getBBox(),D=x.width+2*l,u=x.height+2*l,f=1.75*D;(0,d.a$)(p,u,f,i.useMaxWidth),p.attr("viewBox",`${x.x-i.padding}  ${x.y-i.padding} `+D+" "+u)}),"draw"),H=(0,d.K2)((t=>t?t.length*i.fontSizeFactor:1),"getLabelWidth"),T=(0,d.K2)(((t,e,a,n,r,s,c)=>{const p=new g.T({compound:!0,multigraph:!0});let h,l=!0;for(h=0;h<t.length;h++)if("relation"===t[h].stmt){l=!1;break}a?p.setGraph({rankdir:"LR",multigraph:!0,compound:!0,ranker:"tight-tree",ranksep:l?1:i.edgeLengthFactor,nodeSep:l?1:50,isMultiGraph:!0}):p.setGraph({rankdir:"TB",multigraph:!0,compound:!0,ranksep:l?1:i.edgeLengthFactor,nodeSep:l?1:50,ranker:"tight-tree",isMultiGraph:!0}),p.setDefaultEdgeLabel((function(){return{}})),c.db.extract(t);const x=c.db.getStates(),D=c.db.getRelations(),u=Object.keys(x);for(const d of u){const t=x[d];let o;if(a&&(t.parentId=a),t.doc){let a=e.append("g").attr("id",t.id).attr("class","stateGroup");o=T(t.doc,a,t.id,!n,r,s,c);{a=m(a,t,n);let e=a.node().getBBox();o.width=e.width,o.height=e.height+i.padding/2,M[t.id]={y:i.compositTitleSize}}}else o=N(e,t,p);if(t.note){const a={descriptions:[],id:t.id+"-note",note:t.note,type:"note"},i=N(e,a,p);"left of"===t.note.position?(p.setNode(o.id+"-note",i),p.setNode(o.id,o)):(p.setNode(o.id,o),p.setNode(o.id+"-note",i)),p.setParent(o.id,o.id+"-group"),p.setParent(o.id+"-note",o.id+"-group")}else p.setNode(o.id,o)}d.Rm.debug("Count=",p.nodeCount(),p);let f=0;D.forEach((function(t){f++,d.Rm.debug("Setting edge",t),p.setEdge(t.id1,t.id2,{relation:t,width:H(t.title),height:i.labelHeight*d.Y2.getRows(t.title).length,labelpos:"c"},"id"+f)})),(0,o.Zp)(p),d.Rm.debug("Graph after layout",p.nodes());const y=e.node();p.nodes().forEach((function(t){if(void 0!==t&&void 0!==p.node(t)){d.Rm.warn("Node "+t+": "+JSON.stringify(p.node(t))),r.select("#"+y.id+" #"+t).attr("transform","translate("+(p.node(t).x-p.node(t).width/2)+","+(p.node(t).y+(M[t]?M[t].y:0)-p.node(t).height/2)+" )"),r.select("#"+y.id+" #"+t).attr("data-x-shift",p.node(t).x-p.node(t).width/2);s.querySelectorAll("#"+y.id+" #"+t+" .divider").forEach((t=>{const e=t.parentElement;let a=0,i=0;e&&(e.parentElement&&(a=e.parentElement.getBBox().width),i=parseInt(e.getAttribute("data-x-shift"),10),Number.isNaN(i)&&(i=0)),t.setAttribute("x1",0-i+8),t.setAttribute("x2",a-i-8)}))}else d.Rm.debug("No Node "+t+": "+JSON.stringify(p.node(t)))}));let w=y.getBBox();p.edges().forEach((function(t){void 0!==t&&void 0!==p.edge(t)&&(d.Rm.debug("Edge "+t.v+" -> "+t.w+": "+JSON.stringify(p.edge(t))),K(e,p.edge(t),p.edge(t).relation))})),w=y.getBBox();const b={id:a||"root",label:a||"root",width:0,height:0};return b.width=w.width+2*i.padding,b.height=w.height+2*i.padding,d.Rm.debug("Doc rendered",b,p),b}),"renderDoc"),P={setConf:v,draw:z},L={parser:n.Zk,db:n.iP,renderer:P,styles:n.tM,init:(0,d.K2)((t=>{t.state||(t.state={}),t.state.arrowMarkerAbsolute=t.arrowMarkerAbsolute,n.iP.clear()}),"init")}}}]);