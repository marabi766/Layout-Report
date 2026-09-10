#target indesign
/* Report Layout 1.1.0 | InDesign 21.3 | UTF-8 | Standalone or Windows COM. */
(function () {
    function headingLevel(name) {
        name = String(name).toLowerCase().replace(/[\s_:\-]/g, '');
        if (/heading1$|عنوان1$|تیتر1$/.test(name)) return 1;
        if (/heading[2-9]$|عنوان[2-9]$|تیتر[2-9]$/.test(name)) return 2;
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
    if (typeof REPORT_TEST_MODE !== 'undefined') { REPORT_TEST_MODE.headingLevel=headingLevel;REPORT_TEST_MODE.flow=flow;REPORT_TEST_MODE.readCellText=readCellText;REPORT_TEST_MODE.headingGap=headingGap;REPORT_TEST_MODE.captureTables=captureTables;REPORT_TEST_MODE.compareTables=compareTables;return; }
    var standalone=typeof REPORT_CONFIG==='undefined', cfg,doc=null,log=[],warnings=[],stage='Starting',out=null;
    function say(s){log.push(s);if(out)write(new File(out.fsName+'/report-log.txt'),log.join('\r\n'));}
    function write(file,text){file.encoding='UTF-8';if(!file.open('w'))throw Error('Cannot write '+file.fsName);file.write(text);file.close();}
    function quote(s){return '"'+String(s).replace(/\\/g,'\\\\').replace(/"/g,'\\"').replace(/\r/g,'\\r').replace(/\n/g,'\\n').replace(/\t/g,'\\t')+'"';}
    function result(ok,msg){var s='{"ok":'+ok+',"message":'+quote(msg)+',"pages":'+(doc&&doc.isValid?doc.pages.length:0)+',"warnings":'+quote(warnings.join('\n'))+'}';if(out)write(new File(out.fsName+'/result.json'),s);return s;}
    function optional(label,fn){try{fn();}catch(e){warnings.push(label+': '+e.message);}}
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
        var report=new File(cfg.report),template=new File(cfg.template);
        if(!report.exists||!template.exists)throw Error('Input file is missing.');
        if(!/\.docx$/i.test(report.name))throw Error('Select a DOCX report.');
        out=new Folder(cfg.output);if(!out.exists&&!out.create())throw Error('Cannot create output folder.');
        var sourceFonts=new Folder(template.parent.fsName+'/Document fonts');
        if(sourceFonts.exists){var destFonts=new Folder(out.fsName+'/Document fonts');destFonts.create();var ff=sourceFonts.getFiles();for(var fci=0;fci<ff.length;fci++)if(ff[fci] instanceof File&&/\.(ttf|otf)$/i.test(ff[fci].name)){if(!ff[fci].copy(destFonts.fsName+'/'+ff[fci].name))warnings.push('Could not copy font: '+ff[fci].name);}}
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
        var master=doc.masterSpreads.itemByName('D1-Main Body');
        if(!master.isValid)master=doc.pages[0].appliedMaster;
        if(!master||!master.isValid)throw Error('The template needs an applied Parent/Master page for its header and footer.');
        var page=doc.pages[0], width=Number(doc.documentPreferences.pageWidth),height=Number(doc.documentPreferences.pageHeight);
        var margins=page.marginPreferences, bounds=[Number(margins.top),Number(margins.left),height-Number(margins.bottom),width-Number(margins.right)];
        // For the supplied report family, retain its exact body geometry.
        if(master.name==='D1-Main Body')bounds=[85.03937,99.21260,height-85.03937,width-99.21260];
        if(bounds[2]-bounds[0]<150||bounds[3]-bounds[1]<150)throw Error('Template margins leave insufficient text space.');
        // All changes apply to the opened COPY. Parent/Master artwork stays intact.
        for(var li=0;li<doc.layers.length;li++)doc.layers[li].locked=false;
        for(var pi=0;pi<doc.pages.length;pi++){
            var items=doc.pages[pi].pageItems.everyItem().getElements();
            for(var ii=items.length-1;ii>=0;ii--){if(items[ii].isValid){items[ii].locked=false;items[ii].remove();}}
        }
        while(doc.pages.length>1)doc.pages[-1].remove();
        page=doc.pages[0];page.appliedMaster=master;
        // Unused masters may contain the previous report's missing image links.
        // Keep them, but preflight only artwork that appears on output pages.
        var headerFrames=master.textFrames.everyItem().getElements(), updated=false;
        for(var hi=0;hi<headerFrames.length;hi++){
            var hf=headerFrames[hi],hc=String(hf.contents);
            if(hf.label==='REPORT_HEADER'||hc.indexOf('راهبرد جهانی چین')>=0||hc.indexOf('مطالعات ملل با رویکرد تجربه')>=0){
                hf.parentStory.texts[0].contents=String(cfg.title);updated=true;
            }
        }
        if(!updated)warnings.push('Header title was not replaced: label its master text frame REPORT_HEADER to enable replacement.');
        var blue=doc.colors.itemByName('Report Blue');if(!blue.isValid)blue=doc.colors.add({name:'Report Blue',model:ColorModel.PROCESS,space:ColorSpace.RGB,colorValue:[21,79,158]});
        function font(name){
            var f=app.fonts.itemByName(name);if(f.isValid&&f.status===FontStatus.INSTALLED)return f;
            var parts=name.split('\t'),family=parts[0].toLowerCase().replace(/\s/g,''),face=parts[1];
            var pools=[doc.fonts,app.fonts];
            for(var fp=0;fp<pools.length;fp++)for(var fx=0;fx<pools[fp].length;fx++){
                var candidate=pools[fp][fx],cf=candidate.fontFamily.toLowerCase().replace(/\([^)]*\)/g,'').replace(/\s/g,'');
                if(cf===family&&candidate.fontStyleName===face&&candidate.status===FontStatus.INSTALLED)return candidate;
            }
            throw Error('Required font unavailable: '+name+'. Keep Document fonts beside the template or install the bundled fonts.');
        }
        var bodyFont=font('IRNazanin\tRegular'),boldFont=font('IRNazanin\tBold'),h1Font=font('Modam\tExtraBold  [ @mimvid ]'),h2Font=font('Modam\tSemiBold  [ @mimvid ]'),enFont=font('Times New Roman\tRegular');
        function style(name,f,size,leading,align,color){
            var s=doc.paragraphStyles.itemByName(name);if(!s.isValid)s=doc.paragraphStyles.add({name:name});
            s.basedOn=doc.paragraphStyles[0];
            s.properties={appliedFont:f,fontStyle:f.fontStyleName,pointSize:size,leading:leading,justification:align,fillColor:color,firstLineIndent:0,leftIndent:0,rightIndent:0,spaceBefore:0,spaceAfter:6,hyphenation:false,kashidas:KashidasOptions.KASHIDAS_OFF,keepWithNext:0,keepAllLinesTogether:false,keepFirstLines:2,keepLastLines:2,startParagraph:StartParagraph.ANYWHERE,bulletsAndNumberingListType:ListType.NO_LIST,paragraphDirection:ParagraphDirectionOptions.RIGHT_TO_LEFT_DIRECTION};
            s.composer='Adobe World-Ready Paragraph Composer';return s;
        }
        var styles={body:style('Report Body',bodyFont,13,19,Justification.RIGHT_JUSTIFIED,doc.swatches.itemByName('Black')),h1:style('Report Heading 1',h1Font,15,24,Justification.RIGHT_ALIGN,blue),h2:style('Report Heading 2',h2Font,12,20,Justification.RIGHT_ALIGN,blue),cell:style('Report Table',bodyFont,11.5,16,Justification.RIGHT_ALIGN,doc.swatches.itemByName('Black')),english:style('Report English',enFont,10,14,Justification.LEFT_ALIGN,doc.swatches.itemByName('Black'))};
        styles.h1.keepWithNext=2;styles.h1.spaceBefore=10;styles.h1.spaceAfter=8;styles.h2.keepWithNext=2;styles.h2.spaceBefore=8;
        styles.cell.spaceAfter=0;styles.cell.keepFirstLines=1;styles.cell.keepLastLines=1;styles.english.spaceAfter=0;styles.english.paragraphDirection=ParagraphDirectionOptions.LEFT_TO_RIGHT_DIRECTION;
        var titleStyle=style('Report Cover',h1Font,26,40,Justification.CENTER_ALIGN,blue);
        var layer=doc.layers.add({name:'Report Content '+new Date().getTime()});
        function frame(p,b){var t=p.textFrames.add(layer);t.geometricBounds=b;t.textFramePreferences.insetSpacing=0;t.textFramePreferences.firstBaselineOffset=FirstBaseline.ASCENT_OFFSET;t.textFramePreferences.ignoreWrap=true;t.parentStory.storyPreferences.storyDirection=StoryDirectionOptions.RIGHT_TO_LEFT_DIRECTION;return t;}
        if(cfg.cover){var cover=frame(page,[height*.32,bounds[1],height*.58,bounds[3]]);cover.contents=String(cfg.title);cover.parentStory.paragraphs.everyItem().appliedParagraphStyle=titleStyle;page=doc.pages.add(LocationOptions.AT_END);page.appliedMaster=master;}
        var first=frame(page,bounds), last=first;
        wp.properties={removeFormatting:false,importUnusedStyles:false,preserveGraphics:false,preserveTrackChanges:false,importFootnotes:true,importEndnotes:true,importTOC:false,importIndex:false,convertBulletsAndNumbersToText:true};
        stage='Importing Word';say(stage);first.place(report,false);
        doc.recompose();
        var story=first.parentStory, originalText=String(story.contents),tableCount=story.tables.length;
        var originalTables=captureTables(story.tables);
        if(story.characters.length===0)throw Error('Word imported no text.');
        stage='Normalizing heading number spacing';say(stage);
        var headingParas=story.paragraphs.everyItem().getElements(),normalizedHeadings=0;
        for(var hp=headingParas.length-1;hp>=0;hp--){
            var heading=headingParas[hp];if(!headingLevel(heading.appliedParagraphStyle.name))continue;
            var gap=headingGap(String(heading.contents));
            if(gap&&String(heading.contents)!==gap.expected){
                if(gap.length)heading.characters.itemByRange(gap.start,gap.start+gap.length-1).contents=' ';
                else heading.insertionPoints[gap.start].contents=' ';
                if(String(heading.contents)!==gap.expected)throw Error('Heading spacing edit did not match its expected text.');
                normalizedHeadings++;
            }
        }
        // Rebaseline only after each authorized whitespace-only edit is verified.
        originalText=String(story.contents);say('Heading separators normalized: '+normalizedHeadings);
        stage='Applying Persian paragraph styles';say(stage);
        var pars=story.paragraphs.everyItem().getElements();
        for(var j=0;j<pars.length;j++){
            var p=pars[j],level=headingLevel(p.appliedParagraphStyle.name),st=level===1?styles.h1:level===2?styles.h2:styles.body;
            // Preserve bold inline emphasis before clearing Word font and size overrides.
            var runs=p.textStyleRanges.everyItem().getElements(),emphasis=[],offset=p.characters.length?p.characters[0].index:0;
            for(var ri=0;ri<runs.length;ri++)if(/bold/i.test(String(runs[ri].fontStyle))&&runs[ri].characters.length)emphasis.push([runs[ri].characters[0].index-offset,runs[ri].characters.length]);
            p.applyParagraphStyle(st,true);p.kashidas=KashidasOptions.KASHIDAS_OFF;
            if(!level)for(var ei=0;ei<emphasis.length;ei++){var a=emphasis[ei][0],b=a+emphasis[ei][1]-1;if(a>=0&&b<p.characters.length)p.characters.itemByRange(a,b).appliedFont=boldFont;}
        }
        stage='Formatting native editable tables';say(stage);
        for(var ti=0;ti<story.tables.length;ti++){
            var tb=story.tables[ti],n=tb.columns.length,tw=bounds[3]-bounds[1];
            tb.tableDirection=TableDirectionOptions.RIGHT_TO_LEFT_DIRECTION;
            // Keep imported proportions, including merged cells.
            var total=Number(tb.width);for(var ci=0;ci<n;ci++)tb.columns[ci].width=total>0?Number(tb.columns[ci].width)*tw/total:tw/n;
            for(var rr=0;rr<tb.rows.length;rr++)tb.rows[rr].autoGrow=true;
            var cells=tb.cells.everyItem().getElements();
            for(var ce=0;ce<cells.length;ce++){
                var cell=cells[ce],ct=readCellText(cell),cs=/[A-Za-z]/.test(ct)&&!/[\u0600-\u06ff]/.test(ct)?styles.english:styles.cell;
                cell.texts[0].applyParagraphStyle(cs,true);cell.topInset=5;cell.bottomInset=5;cell.leftInset=5;cell.rightInset=5;
                cell.topEdgeStrokeWeight=.4;cell.bottomEdgeStrokeWeight=.4;cell.leftEdgeStrokeWeight=.4;cell.rightEdgeStrokeWeight=.4;
                cell.topEdgeStrokeColor=blue;cell.bottomEdgeStrokeColor=blue;cell.leftEdgeStrokeColor=blue;cell.rightEdgeStrokeColor=blue;
                cell.fillColor=doc.swatches.itemByName('Paper');
            }
            // This report workflow treats the first row as its column headings.
            if(tb.rows.length>1){tb.headerRowCount=1;tb.rows[0].cells.everyItem().fillColor=blue;tb.rows[0].cells.everyItem().texts.everyItem().fillColor=doc.swatches.itemByName('Paper');}
        }
        stage='Disabling automatic kashidas';say(stage);
        for(var ks=0;ks<doc.stories.length;ks++)doc.stories[ks].texts.everyItem().kashidas=KashidasOptions.KASHIDAS_OFF;
        for(var kt=0;kt<story.tables.length;kt++)story.tables[kt].cells.everyItem().texts.everyItem().kashidas=KashidasOptions.KASHIDAS_OFF;
        stage='Flowing text and adding pages';say(stage);
        last=flow(story,last,function(){var pg=doc.pages.add(LocationOptions.AT_END);pg.appliedMaster=master;return frame(pg,bounds);},function(){doc.recompose();},300);
        if(String(story.contents)!==originalText||story.tables.length!==tableCount)throw Error('Content integrity check failed after formatting.');
        stage='Checking content integrity by stable cell ID';say(stage);compareTables(originalTables,story.tables);
        stage='Checking overflow';say(stage);
        var errors=[];
        for(var si=0;si<doc.stories.length;si++)if(doc.stories[si].overflows)errors.push('Overset story '+doc.stories[si].id);
        for(ti=0;ti<story.tables.length;ti++)for(ce=0;ce<story.tables[ti].cells.length;ce++)if(story.tables[ti].cells[ce].overflows)errors.push('Overset table '+(ti+1)+', cell '+(ce+1));
        for(var fi=0;fi<doc.fonts.length;fi++)if(doc.fonts[fi].status!==FontStatus.INSTALLED)warnings.push('Unavailable document font: '+doc.fonts[fi].fullName);
        if(errors.length)throw Error(errors.join('\n'));
        stage='Saving INDD and IDML';say(stage);
        doc.save(new File(out.fsName+'/Report.indd'));
        doc.exportFile(ExportFormat.INDESIGN_MARKUP,new File(out.fsName+'/Report.idml'),false);
        stage='Exporting PDF';say(stage);
        var pdf=doc.pdfExportPreferences,oldPdf=pdf.properties;
        try{pdf.pageRange=PageRange.ALL_PAGES;pdf.exportReaderSpreads=false;pdf.viewPDF=false;doc.exportFile(ExportFormat.PDF_TYPE,new File(out.fsName+'/Report.pdf'),false);}finally{optional('Restore PDF options',function(){pdf.properties=oldPdf;});}
        say('Completed. Pages: '+doc.pages.length+'; tables: '+tableCount+'. Content unchanged after import.');
        if(warnings.length)say('Warnings:\r\n'+warnings.join('\r\n'));
        result(true,'Completed');if(standalone)alert('Report created successfully:\n'+out.fsName+(warnings.length?'\nPlease review the warnings in report-log.txt.':''));
    }catch(e){
        var msg=stage+': '+e.message+' (line '+e.line+')';log.push(msg);
        if(doc&&doc.isValid&&out){try{doc.save(new File(out.fsName+'/Review-Needed.indd'));}catch(ignore){}}
        if(out){try{write(new File(out.fsName+'/report-log.txt'),log.join('\r\n'));result(false,msg);}catch(ignore2){}}
        if(standalone)alert('Report build failed:\n'+msg);else throw e;
    }finally{
        if(wp)for(var key in oldWord){try{wp[key]=oldWord[key];}catch(restore){}}
        if(oldUnits!==undefined)app.scriptPreferences.measurementUnit=oldUnits;
        if(oldUI!==undefined)app.scriptPreferences.userInteractionLevel=oldUI;
    }
}());
