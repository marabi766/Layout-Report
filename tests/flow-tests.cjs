const fs=require('fs'),vm=require('vm'),assert=require('assert');
const src=fs.readFileSync(require('path').join(__dirname,'..','Layout-Report.jsx'),'utf8').replace(/^#target.*$/m,'');
const ctx={REPORT_TEST_MODE:{}};vm.createContext(ctx);vm.runInContext(src,ctx);const h=ctx.REPORT_TEST_MODE;
assert.equal(h.headingLevel('Heading 1'),1);assert.equal(h.headingLevel('Heading2'),2);assert.equal(h.headingLevel('Normal'),0);assert.equal(h.headingLevel('Body Stayles:Body Text'),0);
let story={overflows:true},frames=[{insertionPoints:{'-1':{index:100}}}],step=0;
let last=h.flow(story,frames[0],()=>{let f={insertionPoints:{'-1':{index:0}}};frames.push(f);return f;},()=>{if(step>0)frames.at(-1).insertionPoints[-1].index=100+step*100;story.overflows=step<3;step++;},20);
assert.equal(frames.length,4);assert.strictEqual(last,frames.at(-1));assert.strictEqual(frames[0].nextTextFrame,frames[1]);
let additions=0;assert.throws(()=>h.flow({overflows:true},{insertionPoints:{'-1':{index:0}}},()=>{additions++;return {insertionPoints:{'-1':{index:0}}};},()=>{},100),/cannot fit/);assert.equal(additions,3);
assert.throws(()=>h.flow({overflows:true},{insertionPoints:{'-1':{index:0}}},()=>({insertionPoints:{'-1':{index:Math.random()+1}}}),()=>{},0),/limit reached/);
assert.strictEqual(h.flow({overflows:false},last,()=>{throw Error('should not add')},()=>{},20),last);
console.log('PASS: heading mapping, multi-page thread, no-overflow case, stagnant overflow, page cap.');

const tableBefore=[{id:50,cells:[{id:1,contents:'مفهوم'},{id:2,contents:'English'},{id:3,contents:'شرح'}]}];
function adapt(tables){return tables.map(t=>({id:t.id,cells:t.cells.map(c=>({id:c.id,contents:c.contents,texts:{item:()=>({contents:c.contents})},characters:{length:c.contents.length}}))}));}
const snapshot=h.captureTables(adapt(tableBefore));
const reordered=[{id:50,cells:[{id:3,contents:'شرح'},{id:1,contents:'مفهوم'},{id:2,contents:'English'}]}];
assert.doesNotThrow(()=>h.compareTables(snapshot,adapt(reordered)));
assert.throws(()=>h.compareTables(snapshot,adapt([{id:50,cells:[{id:1,contents:'مفهوم'},{id:2,contents:'Edited'},{id:3,contents:'شرح'}]}])),/content changed/);
assert.throws(()=>h.compareTables(snapshot,adapt([{id:50,cells:[{id:1,contents:'مفهوم'}]}])),/cell count changed/);
assert.throws(()=>h.compareTables(snapshot,adapt([{id:50,cells:[{id:1,contents:'مفهوم'},{id:2,contents:'English'},{id:4,contents:'شرح'}]}])),/Cell removed/);
assert.throws(()=>h.compareTables(snapshot,[]),/Table removed/);
console.log('PASS: reordered table cells accepted; changed content, missing cell, replaced cell ID and missing table rejected.');

assert.equal(h.headingGap('۳،۴)\tموضع نویسندگان\r'.replace('\\t','\t')).expected,'۳،۴) موضع نویسندگان\r');
assert.equal(h.headingGap('(12)    Heading\r').expected,'(12) Heading\r');
assert.equal(h.headingGap('۳،۴)موضع نویسندگان\r').expected,'۳،۴) موضع نویسندگان\r');
assert.equal(h.headingGap('۲)\u2003\u00a0 عنوان\r').expected,'۲) عنوان\r');
assert.equal(h.headingGap('۳) عنوان\r').expected,'۳) عنوان\r');
assert.equal(h.headingGap('عنوان (توضیح) متن\r'),null);
console.log('PASS: heading separator becomes one space; heading prose untouched.');

const oversetCell={id:3,contents:'',texts:{item:()=>({contents:'مفهوم'})},characters:{length:5}};
assert.equal(h.readCellText(oversetCell),'مفهوم');
const beforeOverset=h.captureTables([{id:3282,cells:[oversetCell]}]);
oversetCell.contents='مفهوم';
assert.doesNotThrow(()=>h.compareTables(beforeOverset,[{id:3282,cells:[oversetCell]}]));
oversetCell.texts.item=()=>({contents:'تغییر'});
assert.throws(()=>h.compareTables(beforeOverset,[{id:3282,cells:[oversetCell]}]),/content changed/);
assert.equal(h.readCellText({id:5,texts:{item:()=>({contents:''})},characters:{length:0}}),'');
assert.equal(h.readCellText({id:6,texts:{item:()=>({contents:''})},characters:{length:2,everyItem:()=>({getElements:()=>[{contents:'ا'},{contents:'ب'}]})}}),'اب');
console.log('PASS: overset Cell.contents ignored; text model preserved across layout; true change rejected; empty cell and character fallback supported.');
