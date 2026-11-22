const assert=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm');
const source=fs.readFileSync(__dirname+'/kakao-auto-bridge.js','utf8');
const context={SIGNATURE_SIZE:96,sources:new Map(),buckets:new Map(),stats:{matches:0,ambiguous:0,comparisons:[]}};
// Native implementations are checked independently in the x86 fixture.
context.nativeBytes=x=>x;
context.equalBytes=(a,b,n)=>a.slice(0,n).every((v,i)=>v===b[i])?1:0;
context.hashBytes=(a,n)=>{let h=2166136261;for(let i=0;i<n;i++)h=Math.imul(h^a[i],16777619)>>>0;return h;};
for(const name of ['same','fingerprint','compatibleAspect','mapSourceRect','coarsePixels','chooseSource']){
  const start=source.indexOf('function '+name+'(');let end=source.indexOf('{',start),depth=1;
  while(depth&&++end<source.length){if(source[end]==='{')depth++;else if(source[end]==='}')depth--;}
  vm.runInNewContext(source.slice(start,end+1),context);
}
let cases=0;
for(const [sw,sh] of [[200,200],[210,300],[300,210],[100,300]])for(const scale of [.75,1,1.25,1.5,1.75,2,2.5,3,4]){
  const w=Math.round(sw*scale),h=Math.round(sh*scale);
  assert.ok(context.compatibleAspect(w,h,sw,sh));
  for(const [x,y,rw,rh] of [[0,0,w,h],[0,Math.floor(h/3),w,Math.floor(h/2)],[w-1,h-1,1,1]]){
    const rect=context.mapSourceRect(w,h,x,y,rw,rh,sw,sh);
    assert.ok(rect&&rect.x>=0&&rect.y>=0&&rect.w>0&&rect.h>0&&rect.x+rect.w<=sw&&rect.y+rect.h<=sh);cases++;
  }
}
assert.equal(context.mapSourceRect(300,300,0,0,301,300,200,200),null);
assert.equal(context.mapSourceRect(300,300,-1,0,100,100,200,200),null);
assert.equal(context.compatibleAspect(300,200,200,200),false);
const ref=Uint8Array.from({length:88*88*3},(_,i)=>Math.floor(i/3)%88<44?20:230);
function add(id,bytes){context.sources.set(id,{id,w:200,h:200,refs:[bytes],coarse:[context.coarsePixels(bytes,96,96)]});const key=context.fingerprint(bytes);context.buckets.set(key,[...(context.buckets.get(key)||[]),id]);}
add('one',ref);assert.equal(context.chooseSource(ref,300,300),'one');
assert.equal(context.chooseSource(Uint8Array.from(ref,x=>x+2),800,800),'one');
assert.equal(context.chooseSource(Uint8Array.from(ref,x=>255-x),300,300),null);
add('duplicate',ref);assert.equal(context.chooseSource(ref,300,300),null);
assert.equal(context.chooseSource(Uint8Array.from(ref,x=>x+2),300,300),null);
assert.ok(!source.includes('s.w===w&&s.h===h'));
assert.ok(source.includes('same(actual,old.pixels)'),'GDI handle reuse must validate content');
assert.ok(source.includes('cache.size>=MAX_MATCH_CACHE'));
// Same HBITMAP may be repainted/reused without DeleteObject. Validate content,
// including a previous negative result, before serving a cached match.
const matchCode=source.slice(source.indexOf('function match('),source.indexOf('\nconst blend='));
const cacheContext={cache:new Map(),sources:new Map([['one',{w:200,h:200}]]),MAX_MATCH_CACHE:128,
  stats:{performance:{calls:0,matchMs:0,fromBitmapMs:0,normalizeMs:0,chooseMs:0}},
  compatibleAspect:context.compatibleAspect,same:context.same,Memory:{alloc:()=>({readPointer:()=>1})},ptr:()=>0,
  fromBitmap:()=>0,dispose:()=>{},normalizedPixels:()=>[cacheContext.content],
  chooseSource:()=>cacheContext.content[0]===7?'one':null,content:new Uint8Array([0])};
vm.runInNewContext(matchCode,cacheContext);const handle={toString:()=> 'same-handle'};
assert.equal(cacheContext.match(handle,300,300),null);
cacheContext.content=new Uint8Array([7]);assert.equal(cacheContext.match(handle,300,300),'one');
cacheContext.content=new Uint8Array([9]);assert.equal(cacheContext.match(handle,300,300),null);
for(let i=0;i<300;i++)cacheContext.match({toString:()=>String(i)},300,300);
assert.equal(cacheContext.cache.size,128);
// Invoke the actual shipped draw callback with fake native pointers. It must
// map the source crop but preserve every destination and blend argument.
const p=n=>({toInt32:()=>n,toString:()=>String(n),isNull:()=>false});
let callback;
const drawContext={busy:false,stopped:false,uiThread:42,app:{base:{add:n=>n}},
  stats:{sizes:{},replacements:0,errors:[]},Memory:{alloc:()=>({add:offset=>({readS32:()=>offset===4?400:600})})},
  currentObject:()=>p(1),getObject:()=>true,mapSourceRect:context.mapSourceRect,match:()=> 'one',ptr:p,
  sources:new Map([['one',{w:200,h:300,frames:[{dc:p(99),end:1000}],started:Date.now(),duration:1000,lastCycle:0,report:{loops:0,replacements:0,frameUse:{}}}]]),
  pending:new Set(),listeners:[],blend:1,Interceptor:{attach:(_,cb)=>{callback=cb;}}};
const hook=source.slice(source.indexOf('listeners.push(Interceptor.attach(blend,'),source.indexOf("listeners.push(Interceptor.attach(gdi."));
vm.runInNewContext(hook,drawContext);
for(const [dw,dh] of [[1280,720],[1920,1080],[3840,2160],[7680,4320]]){
  const args=[1,20,30,dw,dh,2,40,60,200,300,0x1ff0000].map(p),before=args.slice();
  callback.onEnter.call({threadId:42,returnAddress:{equals:()=>true,compare:address=>typeof address==='object'?1:-1}},args);
  for(const i of [0,1,2,3,4,10])assert.equal(args[i],before[i]);
  assert.deepEqual(args.slice(6,10).map(x=>x.toInt32()),[20,30,100,150]);
}
assert.equal(drawContext.stats.replacements,4);assert.equal(drawContext.stats.errors.length,0);
console.log(`Resolution regression: ${cases} scaled/cropped rectangles and matching/ambiguity checks passed`);
