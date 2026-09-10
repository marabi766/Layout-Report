#target indesign
/* Report Layout 2.0.0-preview.1 | InDesign 21.3 | UTF-8 | Standalone or Windows COM. */
(function () {
    function layoutOptions(input) {
        var o={SchemaVersion:1,BodyFont:'IRNazanin\tRegular',BoldFont:'IRNazanin\tBold',Heading1Font:'Modam\tExtraBold  [ @mimvid ]',Heading2Font:'Modam\tSemiBold  [ @mimvid ]',Heading3Font:'Modam\tSemiBold  [ @mimvid ]',EnglishFont:'Times New Roman\tRegular',BodySize:13,BodyLeading:19,BodyAfter:6,Alignment:0,H1Size:15,H1Leading:24,H1Before:10,H1After:8,H2Size:12,H2Leading:20,H2Before:8,H2After:6,H3Size:11,H3Leading:18,H3Before:6,H3After:4,HeadingNumberSeparator:'-',AccentColor:'#154F9E',TableColor:'#154F9E',Margins:0,ParentName:'',MarginTop:30,MarginBottom:30,MarginLeft:35,MarginRight:35,TableSize:11.5,TableLeading:16,EnglishSize:10,EnglishLeading:14,CellPadding:5,TableBorder:.4,TableHeader:true,RepeatHeader:true,HeaderAlignment:0,CellVerticalAlignment:0,FormatBullets:true,BulletIndent:18,FormatNumberedLists:true,NumberedIndent:18,ImportInlineImages:true,ReplaceImagesWithPlaceholders:false,ImageMaxWidthPercent:100,ImageMaxHeightPercent:80,CenterInlineImages:true,CoverSize:26,CoverLeading:40,TocEnabled:false,TocTitle:'فهرست مطالب',TocLevel2:true,PdfPreset:'Application settings'},k;
        input=input||{};
        for(k in input)if(input.hasOwnProperty(k)){
            if(!o.hasOwnProperty(k))throw Error('Unknown layout option: '+k);
            if(typeof input[k]!==typeof o[k])throw Error('Invalid type for layout option: '+k);
            o[k]=input[k];
        }
        if(o.SchemaVersion!==1)throw Error('Unsupported layout preset schema.');
        function range(key,min,max){if(!isFinite(o[key])||o[key]<min||o[key]>max)throw Error(key+' must be between '+min+' and '+max+'.');}
        var sizes=['BodySize','H1Size','H2Size','H3Size','TableSize','EnglishSize','CoverSize'],lead=['BodyLeading','H1Leading','H2Leading','H3Leading','TableLeading','EnglishLeading','CoverLeading'];
        for(k=0;k<sizes.length;k++){range(sizes[k],6,72);range(lead[k],6,120);if(o[lead[k]]<o[sizes[k]])throw Error(lead[k]+' must be at least '+sizes[k]+'.');}
        var spaces=['BodyAfter','H1Before','H1After','H2Before','H2After','H3Before','H3After'];for(k=0;k<spaces.length;k++)range(spaces[k],0,100);
        var margins=['MarginTop','MarginBottom','MarginLeft','MarginRight'];for(k=0;k<margins.length;k++)range(margins[k],0,150);
        range('CellPadding',0,40);range('TableBorder',0,10);range('BulletIndent',0,100);range('NumberedIndent',0,100);range('ImageMaxWidthPercent',10,100);range('ImageMaxHeightPercent',10,100);
        if(o.Margins!==0&&o.Margins!==1&&o.Margins!==2)throw Error('Invalid margin source.');
        if(o.Alignment!==0&&o.Alignment!==1)throw Error('Invalid body alignment.');
        if(o.HeaderAlignment<0||o.HeaderAlignment>2||o.CellVerticalAlignment<0||o.CellVerticalAlignment>2)throw Error('Invalid table alignment.');
        var fonts=['BodyFont','BoldFont','Heading1Font','Heading2Font','Heading3Font','EnglishFont'];for(k=0;k<fonts.length;k++)if(!/^[^\t]+\t[^\t]+$/.test(o[fonts[k]]))throw Error('Font must contain family, TAB and style: '+fonts[k]);
        hexColor(o.AccentColor);hexColor(o.TableColor);
        if(typeof o.HeadingNumberSeparator!=='string'||o.HeadingNumberSeparator.length!==1||/[\s0-9\u06f0-\u06f9\u0660-\u0669()]/.test(o.HeadingNumberSeparator))throw Error('Heading number separator must be one non-numeric character.');
        if(o.TocEnabled&&!/\S/.test(o.TocTitle))throw Error('Enter a contents title.');
        if(!/\S/.test(o.PdfPreset))throw Error('Select a PDF preset.');
        return o;
    }
    function hexColor(hex){if(!/^#[0-9a-fA-F]{6}$/.test(hex))throw Error('Color must be #RRGGBB.');return [parseInt(hex.substr(1,2),16),parseInt(hex.substr(3,2),16),parseInt(hex.substr(5,2),16)];}
    function contentsText(title,entries){var lines=[String(title).replace(/[\r\n\t]+/g,' ')];for(var i=0;i<entries.length;i++){if(!entries[i].page)throw Error('Cannot resolve a heading page for the table of contents.');lines.push(String(entries[i].text).replace(/[\r\n\t]+/g,' ').replace(/\s+$/,'')+'  —  '+entries[i].page);}return lines.join('\r');}
    if(typeof REPORT_TEST_MODE!=='undefined')REPORT_TEST_MODE.contentsText=contentsText;
    function bodyBounds(o,width,height,margins,parentName){
        var b=[Number(margins.top),Number(margins.left),height-Number(margins.bottom),width-Number(margins.right)];
        if(o.Margins===0&&parentName==='D1-Main Body')b=[85.03937,99.21260,height-85.03937,width-99.21260];
        if(o.Margins===2){var pt=72/25.4;b=[o.MarginTop*pt,o.MarginLeft*pt,height-o.MarginBottom*pt,width-o.MarginRight*pt];}
        for(var i=0;i<4;i++)if(!isFinite(b[i])||b[i]<0)throw Error('Invalid template margins.');
        if(b[2]>height||b[3]>width||b[2]-b[0]<150||b[3]-b[1]<150)throw Error('Template margins leave insufficient text space.');
        return b;
    }
    function jsonString(value){return '"'+String(value).replace(/["\\\x00-\x1f\u2028\u2029]/g,function(c){var n=c.charCodeAt(0);if(c==='"')return '\\"';if(c==='\\')return '\\\\';return '\\u'+('0000'+n.toString(16)).slice(-4);})+'"';}
    function json(value){if(value===null||value===undefined)return 'null';if(typeof value==='string')return jsonString(value);if(typeof value==='number')return isFinite(value)?String(value):'null';if(typeof value==='boolean')return String(value);var parts=[],i;if(value instanceof Array){for(i=0;i<value.length;i++)parts.push(json(value[i]));return '['+parts.join(',')+']';}for(i in value)if(value.hasOwnProperty(i))parts.push(jsonString(i)+':'+json(value[i]));return '{'+parts.join(',')+'}';}
    function headingLevel(name) {
        name = String(name).toLowerCase().replace(/[\s_:\-]/g, '');
        if (/heading1$|عنوان1$|تیتر1$/.test(name)) return 1;
        if (/heading2$|عنوان2$|تیتر2$/.test(name)) return 2;
        if (/heading3$|عنوان3$|تیتر3$/.test(name)) return 3;
        return 0;
    }
    function flow(story, last, add, recompose, limit) {
        var count=0, stagnant=0, before, after, next;
        recompose();
        while (story.overflows) {
            if (count++ >= limit) throw Error('Page limit reached; inspect an oversized table or paragraph.');
            before=last.insertionPoints[-1].index;
            next=add(); last.nextTextFrame=next; last=next; recompose();
            after=last.insertionPoints[-1].index;
            stagnant=(after<=before)?stagnant+1:0;
            if(stagnant>=3) throw Error('Text cannot fit on a full page. Inspect table row height or keep options.');
        }
        return last;
    }
    function headingGap(text) {
        // Word outline numbering has been imported as literal text. Only edit
        // the separator after a numeric prefix, never parentheses in prose.
        var m=String(text).match(/^([ \t\u200e\u200f\u202a-\u202e\u2066-\u2069]*\(?[0-9\u06f0-\u06f9\u0660-\u0669]+(?:[.,\u060c\u066b\-\u2013][0-9\u06f0-\u06f9\u0660-\u0669]+)*[)\uff09])([ \t\u00a0\u2000-\u200a\u202f\u205f\u3000]*)(?=[^\r\n])/);
        if(!m)return null;
        return {start:m[1].length,length:m[2].length,expected:m[1]+' '+String(text).substring(m[0].length)};
    }
    function headingNumbering(text,separator){
        var value=String(text),m=value.match(/^([ \t\u200e\u200f\u202a-\u202e\u2066-\u2069]*\(?)([0-9\u06f0-\u06f9\u0660-\u0669]+(?:[.,\u060c\u066b\-\u2013][0-9\u06f0-\u06f9\u0660-\u0669]+)*)([)\uff09])([ \t\u00a0\u2000-\u200a\u202f\u205f\u3000]*)(?=[^\r\n])/);
        if(!m)return null;
        var prefix=m[1]+m[2].replace(/[.,\u060c\u066b\-\u2013]/g,separator)+m[3];
        return {prefixLength:m[1].length+m[2].length+m[3].length,prefix:prefix,gapLength:m[4].length,expected:prefix+' '+value.substring(m[0].length)};
    }
    function listKind(text){var value=String(text);if(/^[ \t\u200e\u200f\u202a-\u202e\u2066-\u2069]*[\u2022\u00b7\u25aa\u25e6\u2023\u2043][ \t\u00a0]+/.test(value))return 'bullet';if(/^[ \t\u200e\u200f\u202a-\u202e\u2066-\u2069]*\(?[0-9\u06f0-\u06f9\u0660-\u0669]+(?:[.,\u060c\u066b\-\u2013][0-9\u06f0-\u06f9\u0660-\u0669]+)*[.)\uff09][ \t\u00a0]+/.test(value))return 'number';return '';}
    function readCellText(cell) {
        // Cell.contents can report an empty string when its table is overset.
        // Read the text model, not the layout-dependent convenience property.
        var text=cell.texts.item(0),value=text.contents;
        if(typeof value!=='string')throw Error('Unsupported non-text table cell: '+cell.id);
        if(value.length===0&&cell.characters.length>0){
            var parts=[],characters=cell.characters.everyItem().getElements();
            for(var k=0;k<characters.length;k++){
                var c=characters[k].contents;
                if(typeof c!=='string')throw Error('Unsupported special character in table cell: '+cell.id);
                parts.push(c);
            }
            value=parts.join('');
            if(!value.length)throw Error('Cannot read text model of table cell: '+cell.id);
        }
        return value;
    }
    function captureTables(tables) {
        var snapshot={};
        for(var t=0;t<tables.length;t++){
            var table=tables[t],cells={},count=0;
            for(var c=0;c<table.cells.length;c++){
                var cell=table.cells[c];cells['cell_'+cell.id]=readCellText(cell);count++;
            }
            snapshot['table_'+table.id]={cells:cells,count:count};
        }
        return snapshot;
    }
    function compareTables(before,tables) {
        var after=captureTables(tables),tk,ck;
        for(tk in before){
            if(!after[tk])throw Error('Table removed: '+tk);
            if(before[tk].count!==after[tk].count)throw Error('Table cell count changed: '+tk);
            for(ck in before[tk].cells){
                if(!after[tk].cells.hasOwnProperty(ck))throw Error('Cell removed: '+tk+'/'+ck);
                if(before[tk].cells[ck]!==after[tk].cells[ck]){
                    var a=before[tk].cells[ck],b=after[tk].cells[ck],i=0;
                    while(i<a.length&&i<b.length&&a.charAt(i)===b.charAt(i))i++;
                    throw Error('Table content changed: '+tk+'/'+ck+'; before length='+a.length+'; after length='+b.length+'; first difference='+i+'; before char='+a.charCodeAt(i)+'; after char='+b.charCodeAt(i));
                }
            }
        }
        for(tk in after)if(!before[tk])throw Error('Unexpected new table: '+tk);
    }
    if (typeof REPORT_TEST_MODE !== 'undefined') { REPORT_TEST_MODE.layoutOptions=layoutOptions;REPORT_TEST_MODE.hexColor=hexColor;REPORT_TEST_MODE.bodyBounds=bodyBounds;REPORT_TEST_MODE.json=json;REPORT_TEST_MODE.headingLevel=headingLevel;REPORT_TEST_MODE.flow=flow;REPORT_TEST_MODE.readCellText=readCellText;REPORT_TEST_MODE.headingGap=headingGap;REPORT_TEST_MODE.headingNumbering=headingNumbering;REPORT_TEST_MODE.listKind=listKind;REPORT_TEST_MODE.captureTables=captureTables;REPORT_TEST_MODE.compareTables=compareTables;return; }
    var standalone=typeof REPORT_CONFIG==='undefined', cfg,doc=null,log=[],warnings=[],stage='Starting',out=null;
    function say(s){log.push(s);if(out)write(new File(out.fsName+'/report-log.txt'),log.join('\r\n'));}
    function write(file,text){file.encoding='UTF-8';if(!file.open('w'))throw Error('Cannot write '+file.fsName);file.write(text);file.close();}
    var metrics={tables:0,unwrappedTables:0,headings:0,heading1:0,heading2:0,heading3:0,bullets:0,numberedParagraphs:0,inlineImages:0,resizedImages:0,imagePlaceholders:0,floatingImagesSkipped:0,missingFonts:0,missingFontNames:[],pdfPresets:[],availableFonts:[],checks:[],tocPages:0};
    function unavailableFont(name){for(var mi=0;mi<metrics.missingFontNames.length;mi++)if(metrics.missingFontNames[mi]===name)return;metrics.missingFontNames.push(name);metrics.missingFonts=metrics.missingFontNames.length;warnings.push('Unavailable document font: '+name);}
    function result(ok,msg){var value={ok:ok,message:msg,version:'2.0.0-preview.1',pages:doc&&doc.isValid?doc.pages.length:0,warnings:warnings.join('\n')};for(var k in metrics)value[k]=metrics[k];var s=json(value);if(out)write(new File(out.fsName+'/result.json'),s);return s;}
    function optional(label,fn){try{fn();}catch(e){warnings.push(label+': '+e.message);}}
    var cancelled=false;
    function checkCancelled(){if(cfg&&cfg.cancelFile&&new File(cfg.cancelFile).exists){cancelled=true;throw Error('Cancelled by user.');}}
    function selectConfig(){
        var report=File.openDialog('Select Word Report','Word:*.docx');if(!report)return null;
        var template=File.openDialog('Select InDesign Template','InDesign:*.idml;*.indd;*.indt');if(!template)return null;
        var title=prompt('Report Title',decodeURI(report.displayName).replace(/\.docx$/i,''));if(title===null)return null;
        var folder=Folder.selectDialog('Output Folder');if(!folder)return null;
        return {report:report.fsName,template:template.fsName,title:title,output:folder.fsName+'/Report-'+new Date().getTime(),cover:true};
    }
    var oldUI,oldUnits,oldWord={},wp,wordKeys=['removeFormatting','importUnusedStyles','preserveGraphics','preserveTrackChanges','importFootnotes','importEndnotes','importTOC','importIndex','convertBulletsAndNumbersToText'];
    try {
        cfg=standalone?selectConfig():REPORT_CONFIG;if(!cfg)return;
        var options=layoutOptions(cfg.layout),validationOnly=cfg.mode==='validate';
        var report=new File(cfg.report||''),template=new File(cfg.template);
        if(!template.exists||(!validationOnly&&!report.exists))throw Error('Input file is missing.');
        if(!validationOnly&&!/\.docx$/i.test(report.name))throw Error('Select a DOCX report.');
        if(!validationOnly&&!/\S/.test(String(cfg.title||'')))throw Error('Enter a report title.');
        out=new Folder(cfg.output);if(!out.exists&&!out.create())throw Error('Cannot create output folder.');
        var sourceFonts=new Folder(template.parent.fsName+'/Document fonts');
        if(sourceFonts.exists){var destFonts=new Folder(out.fsName+'/Document fonts');destFonts.create();var ff=sourceFonts.getFiles();for(var fci=0;fci<ff.length;fci++){checkCancelled();if(ff[fci] instanceof File&&/\.(ttf|otf)$/i.test(ff[fci].name)){if(!ff[fci].copy(destFonts.fsName+'/'+ff[fci].name))warnings.push('Could not copy font: '+ff[fci].name);}}}
        if(new File(out.fsName+'/Report.indd').exists)throw Error('Output already exists. Select a new output folder.');
        oldUI=app.scriptPreferences.userInteractionLevel;oldUnits=app.scriptPreferences.measurementUnit;
        app.scriptPreferences.userInteractionLevel=UserInteractionLevels.NEVER_INTERACT;
        app.scriptPreferences.measurementUnit=MeasurementUnits.POINTS;
        wp=app.wordRTFImportPreferences;
        for(var wi=0;wi<wordKeys.length;wi++)oldWord[wordKeys[wi]]=wp[wordKeys[wi]];
        stage='Opening a copy of the template';say(stage);
        doc=app.open(template,true,OpenOptions.OPEN_COPY);
        doc.viewPreferences.horizontalMeasurementUnits=MeasurementUnits.POINTS;
        doc.viewPreferences.verticalMeasurementUnits=MeasurementUnits.POINTS;
        doc.viewPreferences.rulerOrigin=RulerOrigin.PAGE_ORIGIN;
        doc.zeroPoint=[0,0];
        doc.textDefaults.kashidas=KashidasOptions.KASHIDAS_OFF;
        if(doc.documentPreferences.facingPages)throw Error('This version requires a single-page template (Facing Pages off).');
        var master=options.ParentName?doc.masterSpreads.itemByName(options.ParentName):doc.masterSpreads.itemByName('D1-Main Body');
        if(!master.isValid&&!options.ParentName)master=doc.pages[0].appliedMaster;
        if(!master||!master.isValid)throw Error('The template needs an applied Parent/Master page for its header and footer.');
        var page=doc.pages[0], width=Number(doc.documentPreferences.pageWidth),height=Number(doc.documentPreferences.pageHeight);
        var margins=page.marginPreferences,bounds=bodyBounds(options,width,height,margins,master.name);
        function font(name){
            var f=app.fonts.itemByName(name);if(f.isValid&&f.status===FontStatus.INSTALLED)return f;
            var parts=name.split('\t'),family=parts[0].toLowerCase().replace(/\s/g,''),face=parts[1];
            // A full app.fonts traversal can take minutes or stall InDesign. The
            // direct lookup above covers installed fonts; only the small document
            // font collection is used for tolerant family-name matching.
            var pools=[doc.fonts];
            for(var fp=0;fp<pools.length;fp++)for(var fx=0;fx<pools[fp].length;fx++){
                checkCancelled();
                var candidate=pools[fp][fx],cf=candidate.fontFamily.toLowerCase().replace(/\([^)]*\)/g,'').replace(/\s/g,'');
                if(cf===family&&candidate.fontStyleName===face&&candidate.status===FontStatus.INSTALLED)return candidate;
            }
            throw Error('Required font unavailable: '+name+'. Keep Document fonts beside the template or install the bundled fonts.');
        }
        stage='Validating template and layout';say(stage);
        checkCancelled();
        metrics.checks.push('Single-page layout; valid parent: '+master.name);
        metrics.checks.push('Body frame: '+bounds.join(', ')+' pt');
        var headerFrames=master.allPageItems,headers=[],legacyHeader=false;
        for(var hi=0;hi<headerFrames.length;hi++){
            var hf=headerFrames[hi];if(!(hf instanceof TextFrame))continue;
            var hc=String(hf.contents);
            if(hf.label==='REPORT_HEADER')headers.push(hf);
            else if(hc.indexOf('راهبرد جهانی چین')>=0||hc.indexOf('مطالعات ملل با رویکرد تجربه')>=0){headers.push(hf);legacyHeader=true;}
        }
        if(!headers.length)throw Error('No report header. Set Script Label REPORT_HEADER on a parent text frame.');
        if(legacyHeader)warnings.push('Legacy header recognized. Label this parent text frame REPORT_HEADER for future templates.');
        metrics.checks.push('Header frame found.');
        say('Checking PDF presets');
        for(var pfi=0;pfi<app.pdfExportPresets.length;pfi++){checkCancelled();metrics.pdfPresets.push(app.pdfExportPresets[pfi].name);}
        var pdfPreset=null;
        if(options.PdfPreset!=='Application settings'){
            pdfPreset=app.pdfExportPresets.itemByName(options.PdfPreset);
            if(!pdfPreset.isValid)throw Error('PDF preset not installed: '+options.PdfPreset+'. Choose an exact name from the validation results.');
        }
        say('Checking required fonts');
        var requiredNames=[options.BodyFont,options.BoldFont,options.Heading1Font,options.Heading2Font,options.Heading3Font,options.EnglishFont],requiredFonts=[];
        for(var rfi=0;rfi<requiredNames.length;rfi++){checkCancelled();try{var resolved=font(requiredNames[rfi]);requiredFonts.push(resolved);metrics.availableFonts.push(resolved.fontFamily+'\t'+resolved.fontStyleName);}catch(fontError){metrics.missingFontNames.push(requiredNames[rfi]);}}
        metrics.missingFonts=metrics.missingFontNames.length;
        if(metrics.missingFonts)throw Error('Required fonts unavailable: '+metrics.missingFontNames.join('; '));
        var bodyFont=requiredFonts[0],boldFont=requiredFonts[1],h1Font=requiredFonts[2],h2Font=requiredFonts[3],h3Font=requiredFonts[4],enFont=requiredFonts[5];
        metrics.checks.push('Selected fonts and PDF preset available.');
        metrics.checks.push('Report paragraph styles and colors will be created or updated on a copy.');
        var styleNames=['Report Body','Report Heading 1','Report Heading 2','Report Heading 3','Report Bullet','Report Numbered','Report Table','Report Table Header','Report English','Report Cover'];
        for(var vsi=0;vsi<styleNames.length;vsi++)metrics.checks.push(styleNames[vsi]+': '+(doc.paragraphStyles.itemByName(styleNames[vsi]).isValid?'will update':'will create'));
        if(validationOnly){for(var vfi=0;vfi<doc.fonts.length;vfi++){checkCancelled();if(doc.fonts[vfi].status!==FontStatus.INSTALLED)unavailableFont(doc.fonts[vfi].fullName);}say('Template validation completed. This does not replace document Preflight after import.');result(true,'Template validated');if(standalone)alert('Template validated.');return;}
        // All changes apply to the opened COPY. Persistent artwork belongs on the parent.
        for(var li=0;li<doc.layers.length;li++){checkCancelled();doc.layers[li].locked=false;}
        for(var pi=0;pi<doc.pages.length;pi++){
            var items=doc.pages[pi].pageItems.everyItem().getElements();
            for(var ii=items.length-1;ii>=0;ii--){checkCancelled();if(items[ii].isValid){items[ii].locked=false;items[ii].remove();}}
        }
        while(doc.pages.length>1)doc.pages[-1].remove();
        page=doc.pages[0];page.appliedMaster=master;
        for(hi=0;hi<headers.length;hi++){headers[hi].locked=false;headers[hi].parentStory.texts[0].contents=String(cfg.title);}
        function color(name,value){var c=doc.colors.itemByName(name);if(!c.isValid)c=doc.colors.add({name:name});c.properties={model:ColorModel.PROCESS,space:ColorSpace.RGB,colorValue:hexColor(value)};return c;}
        var blue=color('Report Blue',options.AccentColor),tableBlue=color('Report Table Accent',options.TableColor);
        function style(name,f,size,leading,align,color){
            var s=doc.paragraphStyles.itemByName(name);if(!s.isValid)s=doc.paragraphStyles.add({name:name});
            s.basedOn=doc.paragraphStyles[0];
            s.properties={appliedFont:f,fontStyle:f.fontStyleName,pointSize:size,leading:leading,justification:align,fillColor:color,firstLineIndent:0,leftIndent:0,rightIndent:0,spaceBefore:0,spaceAfter:6,hyphenation:false,kashidas:KashidasOptions.KASHIDAS_OFF,keepWithNext:0,keepAllLinesTogether:false,keepFirstLines:2,keepLastLines:2,startParagraph:StartParagraph.ANYWHERE,bulletsAndNumberingListType:ListType.NO_LIST,paragraphDirection:ParagraphDirectionOptions.RIGHT_TO_LEFT_DIRECTION};
            s.composer='Adobe World-Ready Paragraph Composer';return s;
        }
        var headerJustification=options.HeaderAlignment===0?Justification.CENTER_ALIGN:options.HeaderAlignment===1?Justification.RIGHT_ALIGN:Justification.LEFT_ALIGN;
        var styles={body:style('Report Body',bodyFont,options.BodySize,options.BodyLeading,options.Alignment===0?Justification.RIGHT_JUSTIFIED:Justification.RIGHT_ALIGN,doc.swatches.itemByName('Black')),h1:style('Report Heading 1',h1Font,options.H1Size,options.H1Leading,Justification.RIGHT_ALIGN,blue),h2:style('Report Heading 2',h2Font,options.H2Size,options.H2Leading,Justification.RIGHT_ALIGN,blue),h3:style('Report Heading 3',h3Font,options.H3Size,options.H3Leading,Justification.RIGHT_ALIGN,blue),bullet:style('Report Bullet',bodyFont,options.BodySize,options.BodyLeading,Justification.RIGHT_ALIGN,doc.swatches.itemByName('Black')),numbered:style('Report Numbered',bodyFont,options.BodySize,options.BodyLeading,Justification.RIGHT_ALIGN,doc.swatches.itemByName('Black')),cell:style('Report Table',bodyFont,options.TableSize,options.TableLeading,Justification.RIGHT_ALIGN,doc.swatches.itemByName('Black')),header:style('Report Table Header',bodyFont,options.TableSize,options.TableLeading,headerJustification,doc.swatches.itemByName('Paper')),english:style('Report English',enFont,options.EnglishSize,options.EnglishLeading,Justification.LEFT_ALIGN,doc.swatches.itemByName('Black'))};
        styles.body.spaceAfter=options.BodyAfter;
        styles.h1.keepWithNext=2;styles.h1.spaceBefore=options.H1Before;styles.h1.spaceAfter=options.H1After;styles.h2.keepWithNext=2;styles.h2.spaceBefore=options.H2Before;styles.h2.spaceAfter=options.H2After;styles.h3.keepWithNext=2;styles.h3.spaceBefore=options.H3Before;styles.h3.spaceAfter=options.H3After;
        styles.bullet.rightIndent=options.BulletIndent;styles.bullet.firstLineIndent=-options.BulletIndent;styles.bullet.spaceAfter=options.BodyAfter;styles.numbered.rightIndent=options.NumberedIndent;styles.numbered.firstLineIndent=-options.NumberedIndent;styles.numbered.spaceAfter=options.BodyAfter;
        styles.cell.spaceAfter=0;styles.cell.keepFirstLines=1;styles.cell.keepLastLines=1;styles.english.spaceAfter=0;styles.english.paragraphDirection=ParagraphDirectionOptions.LEFT_TO_RIGHT_DIRECTION;
        var titleStyle=style('Report Cover',h1Font,options.CoverSize,options.CoverLeading,Justification.CENTER_ALIGN,blue);
        var layer=doc.layers.add({name:'Report Content '+new Date().getTime()});
        function frame(p,b){var t=p.textFrames.add(layer);t.geometricBounds=b;t.textFramePreferences.insetSpacing=0;t.textFramePreferences.firstBaselineOffset=FirstBaseline.ASCENT_OFFSET;t.textFramePreferences.ignoreWrap=true;t.parentStory.storyPreferences.storyDirection=StoryDirectionOptions.RIGHT_TO_LEFT_DIRECTION;return t;}
        if(cfg.cover){
            var contentHeight=bounds[2]-bounds[0],coverTop=options.Margins===0?height*.32:bounds[0]+contentHeight*.25,coverBottom=options.Margins===0?height*.58:bounds[0]+contentHeight*.55,detailsTop=options.Margins===0?height*.60:bounds[0]+contentHeight*.60;
            var cover=frame(page,[coverTop,bounds[1],coverBottom,bounds[3]]);cover.contents=String(cfg.title);cover.parentStory.paragraphs.everyItem().appliedParagraphStyle=titleStyle;
            var coverDetails=[],detailKeys=['subtitle','author','organization','date'];for(var cd=0;cd<detailKeys.length;cd++)if(cfg[detailKeys[cd]])coverDetails.push(String(cfg[detailKeys[cd]]));
            if(coverDetails.length){var detailFrame=frame(page,[detailsTop,bounds[1],bounds[2],bounds[3]]);detailFrame.contents=coverDetails.join('\r');detailFrame.parentStory.paragraphs.everyItem().appliedParagraphStyle=style('Report Cover Details',bodyFont,options.BodySize,options.BodyLeading,Justification.CENTER_ALIGN,doc.swatches.itemByName('Black'));}
            page=doc.pages.add(LocationOptions.AT_END);page.appliedMaster=master;
        }
        doc.metadataPreferences.documentTitle=String(cfg.title);doc.metadataPreferences.author=String(cfg.author||'');
        var first=frame(page,bounds), last=first;
        wp.properties={removeFormatting:false,importUnusedStyles:false,preserveGraphics:options.ImportInlineImages,preserveTrackChanges:false,importFootnotes:true,importEndnotes:true,importTOC:false,importIndex:false,convertBulletsAndNumbersToText:true};
        stage='Importing Word';say(stage);first.place(report,false);
        checkCancelled();
        doc.recompose();
        var story=first.parentStory,originalText,tableCount,originalTables;
        if(story.characters.length===0)throw Error('Word imported no text.');
        stage='Normalizing heading number spacing';say(stage);
        var headingParas=story.paragraphs.everyItem().getElements(),normalizedHeadings=0;
        for(var hp=headingParas.length-1;hp>=0;hp--){
            checkCancelled();
            var heading=headingParas[hp];if(!headingLevel(heading.appliedParagraphStyle.name))continue;
            var gap=headingNumbering(String(heading.contents),options.HeadingNumberSeparator);
            if(gap&&String(heading.contents)!==gap.expected){
                heading.characters.itemByRange(0,gap.prefixLength-1).contents=gap.prefix;
                if(gap.gapLength)heading.characters.itemByRange(gap.prefixLength,gap.prefixLength+gap.gapLength-1).contents=' ';
                else heading.insertionPoints[gap.prefixLength].contents=' ';
                if(String(heading.contents)!==gap.expected)throw Error('Heading number normalization did not match its expected text.');
                normalizedHeadings++;
            }
        }
        // Rebaseline only after each authorized whitespace-only edit is verified.
        originalText=String(story.contents);say('Heading separators normalized: '+normalizedHeadings);
        stage='Applying Persian paragraph styles';say(stage);
        var pars=story.paragraphs.everyItem().getElements(),headingRefs=[];
        for(var j=0;j<pars.length;j++){
            checkCancelled();
            var p=pars[j],level=headingLevel(p.appliedParagraphStyle.name),kind=level?'':listKind(p.contents),st=level===1?styles.h1:level===2?styles.h2:level===3?styles.h3:(kind==='bullet'&&options.FormatBullets)?styles.bullet:(kind==='number'&&options.FormatNumberedLists)?styles.numbered:styles.body;
            if(level){metrics.headings++;if(level===1)metrics.heading1++;else if(level===2)metrics.heading2++;else metrics.heading3++;headingRefs.push({paragraph:p,level:level});}else if(kind==='bullet'&&options.FormatBullets)metrics.bullets++;else if(kind==='number'&&options.FormatNumberedLists)metrics.numberedParagraphs++;
            // Preserve bold inline emphasis before clearing Word font and size overrides.
            var runs=p.textStyleRanges.everyItem().getElements(),emphasis=[],offset=p.characters.length?p.characters[0].index:0;
            for(var ri=0;ri<runs.length;ri++)if(/bold/i.test(String(runs[ri].fontStyle))&&runs[ri].characters.length)emphasis.push([runs[ri].characters[0].index-offset,runs[ri].characters.length]);
            p.applyParagraphStyle(st,true);p.kashidas=KashidasOptions.KASHIDAS_OFF;
            if(level){var headingFont=level===1?h1Font:level===2?h2Font:h3Font;p.appliedFont=headingFont;p.fontStyle=headingFont.fontStyleName;p.fillColor=blue;}
            if(!level)for(var ei=0;ei<emphasis.length;ei++){var a=emphasis[ei][0],b=a+emphasis[ei][1]-1;if(a>=0&&b<p.characters.length)p.characters.itemByRange(a,b).appliedFont=boldFont;}
        }
        stage='Sizing inline images';say(stage);
        if(options.ImportInlineImages){
            var graphics=story.allGraphics,graphicCount=graphics.length,maxImageWidth=(bounds[3]-bounds[1])*options.ImageMaxWidthPercent/100,maxImageHeight=(bounds[2]-bounds[0])*options.ImageMaxHeightPercent/100;
            // Placeholder conversion removes graphics from this live collection,
            // so walk it backwards in that mode to avoid skipping any item.
            for(var imageStep=0;imageStep<graphicCount;imageStep++){
                var gi=options.ReplaceImagesWithPlaceholders?graphicCount-1-imageStep:imageStep;
                checkCancelled();
                try{
                    var graphic=graphics[gi],holder=graphic.parent,isInline=false;
                    try{isInline=holder&&holder.isValid&&holder.anchoredObjectSettings.anchoredPosition===AnchorPosition.INLINE_POSITION;}catch(anchorError){isInline=false;}
                    if(!isInline){metrics.floatingImagesSkipped++;continue;}
                    // Anchored frames in overset text do not expose geometricBounds.
                    // Resolving their inner-coordinate anchors works for raster,
                    // WMF and other Word-imported graphics before pagination.
                    metrics.inlineImages++;var imageTopLeft=holder.resolve(AnchorPoint.TOP_LEFT_ANCHOR,CoordinateSpaces.INNER_COORDINATES)[0],imageBottomRight=holder.resolve(AnchorPoint.BOTTOM_RIGHT_ANCHOR,CoordinateSpaces.INNER_COORDINATES)[0],imageWidth=Math.abs(Number(imageBottomRight[0])-Number(imageTopLeft[0])),imageHeight=Math.abs(Number(imageBottomRight[1])-Number(imageTopLeft[1]));
                    if(!isFinite(imageWidth)||!isFinite(imageHeight)||imageWidth<=0||imageHeight<=0){warnings.push('Inline image '+(gi+1)+' has invalid dimensions and was not resized.');continue;}
                    var imageScale=Math.min(1,maxImageWidth/imageWidth,maxImageHeight/imageHeight);
                    if(imageScale<1){holder.resize(CoordinateSpaces.INNER_COORDINATES,AnchorPoint.TOP_LEFT_ANCHOR,ResizeMethods.MULTIPLYING_CURRENT_DIMENSIONS_BY,[imageScale,imageScale]);metrics.resizedImages++;}
                    if(options.ReplaceImagesWithPlaceholders){
                        graphic.remove();holder.label='REPORT_IMAGE_PLACEHOLDER_'+(gi+1);holder.strokeWeight=.75;holder.strokeColor=blue;holder.fillColor=doc.swatches.itemByName('Paper');metrics.imagePlaceholders++;
                    }
                    if(options.CenterInlineImages){try{var centeredAnchor=holder.anchoredObjectSettings;centeredAnchor.anchoredPosition=AnchorPosition.ABOVE_LINE;centeredAnchor.horizontalAlignment=HorizontalAlignment.CENTER_ALIGN;centeredAnchor.anchorSpaceAbove=options.BodyAfter;centeredAnchor.anchorYoffset=options.BodyAfter;}catch(imageAlignError){warnings.push('Could not place inline image '+(gi+1)+' above its text line: '+imageAlignError.message);}}
                }catch(imageError){warnings.push('Inline image '+(gi+1)+' could not be processed and was left unchanged: '+imageError.message);}
            }
            if(metrics.floatingImagesSkipped)warnings.push(metrics.floatingImagesSkipped+' non-inline/floating image(s) were left unchanged. Convert them to In Line with Text in Word for automatic sizing.');
        }
        // InDesign cannot split a table row between text frames. Word sometimes
        // imports large drawing/text containers as a one-row, one-cell table.
        // A large single row cannot continue across pages, so unwrap only these
        // long container cells while keeping their text and anchored objects.
        for(var unwrapIndex=story.tables.length-1;unwrapIndex>=0;unwrapIndex--){
            var unwrapTable=story.tables[unwrapIndex];
            if(unwrapTable.rows.length===1&&unwrapTable.columns.length===1&&unwrapTable.cells[0].characters.length>1000){unwrapTable.convertToText('\t','\r');metrics.unwrappedTables++;}
        }
        if(metrics.unwrappedTables){doc.recompose();warnings.push(metrics.unwrappedTables+' oversized single-cell Word container(s) were converted to normal text so they could continue across pages.');}
        originalText=String(story.contents);tableCount=story.tables.length;metrics.tables=tableCount;originalTables=captureTables(story.tables);
        stage='Formatting native editable tables';say(stage);
        for(var ti=0;ti<story.tables.length;ti++){
            checkCancelled();
            var tb=story.tables[ti],n=tb.columns.length,tw=bounds[3]-bounds[1];
            tb.tableDirection=TableDirectionOptions.RIGHT_TO_LEFT_DIRECTION;
            // Keep imported proportions, including merged cells.
            var total=Number(tb.width);for(var ci=0;ci<n;ci++)tb.columns[ci].width=total>0?Number(tb.columns[ci].width)*tw/total:tw/n;
            for(var rr=0;rr<tb.rows.length;rr++)tb.rows[rr].autoGrow=true;
            var cells=tb.cells.everyItem().getElements();
            for(var ce=0;ce<cells.length;ce++){
                checkCancelled();
                var cell=cells[ce],ct=readCellText(cell),englishOnly=/[A-Za-z]/.test(ct)&&!/[\u0600-\u06ff]/.test(ct),cs=englishOnly?styles.english:styles.cell;
                cell.texts[0].applyParagraphStyle(cs,true);cell.topInset=options.CellPadding;cell.bottomInset=options.CellPadding;cell.leftInset=options.CellPadding;cell.rightInset=options.CellPadding;
                cell.verticalJustification=options.CellVerticalAlignment===0?VerticalJustification.CENTER_ALIGN:options.CellVerticalAlignment===1?VerticalJustification.TOP_ALIGN:VerticalJustification.BOTTOM_ALIGN;
                if(englishOnly)cell.texts[0].paragraphs.everyItem().paragraphDirection=ParagraphDirectionOptions.LEFT_TO_RIGHT_DIRECTION;
                cell.topEdgeStrokeWeight=options.TableBorder;cell.bottomEdgeStrokeWeight=options.TableBorder;cell.leftEdgeStrokeWeight=options.TableBorder;cell.rightEdgeStrokeWeight=options.TableBorder;
                cell.topEdgeStrokeColor=tableBlue;cell.bottomEdgeStrokeColor=tableBlue;cell.leftEdgeStrokeColor=tableBlue;cell.rightEdgeStrokeColor=tableBlue;
                cell.fillColor=doc.swatches.itemByName('Paper');
            }
            // This report workflow treats the first row as its column headings.
            if(options.TableHeader&&tb.rows.length>1){tb.headerRowCount=options.RepeatHeader?1:0;var firstRowCells=tb.rows[0].cells.everyItem().getElements();for(var hci=0;hci<firstRowCells.length;hci++){var headerText=firstRowCells[hci].texts[0],headerEnglish=/[A-Za-z]/.test(readCellText(firstRowCells[hci]))&&!/[\u0600-\u06ff]/.test(readCellText(firstRowCells[hci]));firstRowCells[hci].fillColor=tableBlue;headerText.applyParagraphStyle(styles.header,true);headerText.fillColor=doc.swatches.itemByName('Paper');headerText.paragraphs.everyItem().justification=headerJustification;if(headerEnglish){headerText.appliedFont=enFont;headerText.fontStyle=enFont.fontStyleName;headerText.paragraphs.everyItem().paragraphDirection=ParagraphDirectionOptions.LEFT_TO_RIGHT_DIRECTION;}}}
            else if(tb.headerRowCount!==0)tb.headerRowCount=0;
        }
        stage='Disabling automatic kashidas';say(stage);
        for(var ks=0;ks<doc.stories.length;ks++)doc.stories[ks].texts.everyItem().kashidas=KashidasOptions.KASHIDAS_OFF;
        for(var kt=0;kt<story.tables.length;kt++)story.tables[kt].cells.everyItem().texts.everyItem().kashidas=KashidasOptions.KASHIDAS_OFF;
        stage='Flowing text and adding pages';say(stage);
        last=flow(story,last,function(){checkCancelled();var pg=doc.pages.add(LocationOptions.AT_END);pg.appliedMaster=master;return frame(pg,bounds);},function(){checkCancelled();doc.recompose();},300);
        if(options.TocEnabled){
            stage='Generating table of contents';say(stage);
            var selectedHeadings=[];for(var th=0;th<headingRefs.length;th++)if(headingRefs[th].level===1||(options.TocLevel2&&headingRefs[th].level===2))selectedHeadings.push(headingRefs[th]);
            if(!selectedHeadings.length)throw Error('Contents requested but no matching Word Heading 1/2 styles were found.');
            var bodyPage=first.parentPage,tocPage=doc.pages.add(LocationOptions.BEFORE,bodyPage);tocPage.appliedMaster=master;
            var tocFirst=frame(tocPage,bounds),tocLast=tocFirst,tocStory=tocFirst.parentStory;
            var tocBodyStyle=style('Report Contents Entry',bodyFont,options.BodySize,options.BodyLeading,Justification.RIGHT_ALIGN,doc.swatches.itemByName('Black'));
            tocBodyStyle.keepAllLinesTogether=true;tocBodyStyle.spaceAfter=options.BodyAfter;
            var tocTitleStyle=style('Report Contents Title',h1Font,options.H1Size,options.H1Leading,Justification.RIGHT_ALIGN,blue);tocTitleStyle.keepWithNext=2;
            function tocEntries(){
                var entries=[];for(var te=0;te<selectedHeadings.length;te++){
                    var par=selectedHeadings[te].paragraph,frames=par.insertionPoints[0].parentTextFrames;
                    if(!frames.length||!frames[0].parentPage)throw Error('Cannot locate heading on a page.');
                    entries.push({text:String(par.contents),page:frames[0].parentPage.name});
                }return entries;
            }
            var tocStable=false;
            for(var iteration=0;iteration<20;iteration++){
                checkCancelled();
                doc.recompose();var tocValue=contentsText(options.TocTitle,tocEntries());tocStory.contents=tocValue;
                tocStory.paragraphs.everyItem().appliedParagraphStyle=tocBodyStyle;
                tocStory.paragraphs[0].appliedParagraphStyle=tocTitleStyle;
                for(var tp=0;tp<selectedHeadings.length;tp++)if(selectedHeadings[tp].level===2)tocStory.paragraphs[tp+1].rightIndent=12;
                tocLast=flow(tocStory,tocLast,function(){var nextPage=doc.pages.add(LocationOptions.BEFORE,bodyPage);nextPage.appliedMaster=master;return frame(nextPage,bounds);},function(){doc.recompose();},100);
                doc.recompose();if(contentsText(options.TocTitle,tocEntries())===tocValue){tocStable=true;break;}
            }
            if(!tocStable)throw Error('Contents pagination did not stabilize.');
            metrics.tocPages=tocStory.textContainers.length;
            metrics.checks.push('Contents page numbers stabilized after '+(iteration+1)+' pass(es).');
        }
        if(String(story.contents)!==originalText||story.tables.length!==tableCount)throw Error('Content integrity check failed after formatting.');
        stage='Checking content integrity by stable cell ID';say(stage);compareTables(originalTables,story.tables);
        stage='Checking overflow';say(stage);
        var errors=[];
        for(var si=0;si<doc.stories.length;si++)if(doc.stories[si].overflows)errors.push('Overset story '+doc.stories[si].id);
        for(ti=0;ti<story.tables.length;ti++)for(ce=0;ce<story.tables[ti].cells.length;ce++)if(story.tables[ti].cells[ce].overflows)errors.push('Overset table '+(ti+1)+', cell '+(ce+1));
        for(var fi=0;fi<doc.fonts.length;fi++)if(doc.fonts[fi].status!==FontStatus.INSTALLED)unavailableFont(doc.fonts[fi].fullName);
        metrics.missingFonts=metrics.missingFontNames.length;
        if(errors.length)throw Error(errors.join('\n'));
        stage='Saving INDD and IDML';say(stage);
        doc.save(new File(out.fsName+'/Report.indd'));
        doc.exportFile(ExportFormat.INDESIGN_MARKUP,new File(out.fsName+'/Report.idml'),false);
        stage='Exporting PDF';say(stage);
        // PDF export preferences belong to the InDesign application, not the document.
        // Accessing this preference through the document fails in InDesign 21.3.
        var pdf=app.pdfExportPreferences,oldPdf=pdf.properties;
        try{pdf.pageRange=PageRange.ALL_PAGES;pdf.exportReaderSpreads=false;pdf.viewPDF=false;if(pdfPreset)doc.exportFile(ExportFormat.PDF_TYPE,new File(out.fsName+'/Report.pdf'),false,pdfPreset);else doc.exportFile(ExportFormat.PDF_TYPE,new File(out.fsName+'/Report.pdf'),false);}finally{optional('Restore PDF options',function(){pdf.properties=oldPdf;});}
        say('Completed. Pages: '+doc.pages.length+'; tables: '+tableCount+'. Content unchanged after import.');
        if(warnings.length)say('Warnings:\r\n'+warnings.join('\r\n'));
        result(true,'Completed');if(standalone)alert('Report created successfully:\n'+out.fsName+(warnings.length?'\nPlease review the warnings in report-log.txt.':''));
    }catch(e){
        var msg=stage+': '+e.message+' (line '+e.line+')';log.push(msg);
        if(doc&&doc.isValid&&out&&!validationOnly){try{doc.save(new File(out.fsName+'/Review-Needed.indd'));}catch(ignore){}}
        if(out){try{write(new File(out.fsName+'/report-log.txt'),log.join('\r\n'));result(false,msg);}catch(ignore2){}}
        if(standalone)alert('Report build failed:\n'+msg);else throw e;
    }finally{
        if(doc&&doc.isValid&&(cancelled||validationOnly||cfg.closeAfterBuild)){try{doc.close(SaveOptions.NO);}catch(closeError){}}
        if(wp)for(var key in oldWord){try{wp[key]=oldWord[key];}catch(restore){}}
        if(oldUnits!==undefined)app.scriptPreferences.measurementUnit=oldUnits;
        if(oldUI!==undefined)app.scriptPreferences.userInteractionLevel=oldUI;
    }
}());
