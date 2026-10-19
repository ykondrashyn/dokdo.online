#!/usr/bin/env python3
"""Record a real-player startup/network smoke, or exercise the browser bridge.

Requires Playwright. Neither mode replaces the full gameplay/device checklist.
"""
import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
from urllib.parse import urlparse
from playwright.sync_api import sync_playwright

def run(args):
    directory=Path(args.output); directory.mkdir(parents=True,exist_ok=True)
    record={'browser':args.browser,'url':args.url,'stub':args.stub,
            'started_utc':datetime.now(timezone.utc).isoformat(),'requests':[],
            'websockets':[],'console_errors':[],'checks':{}}
    with sync_playwright() as playwright:
        kind=getattr(playwright,args.browser)
        options={'headless':not args.headed}
        if args.executable: options['executable_path']=args.executable
        if args.browser=='chromium': options['args']=['--no-sandbox','--enable-unsafe-swiftshader','--use-angle=swiftshader']
        else: options['firefox_user_prefs']={'webgl.force-enabled':True,'webgl.out-of-process':False}
        browser=kind.launch(**options)
        context=browser.new_context(viewport={'width':720,'height':1000})
        page=context.new_page()
        page.on('request',lambda request: record['requests'].append({'url':request.url,'method':request.method,'type':request.resource_type}))
        page.on('websocket',lambda socket: record['websockets'].append(socket.url))
        page.on('pageerror',lambda error: record['console_errors'].append(str(error)))
        page.on('console',lambda msg: record['console_errors'].append(msg.text) if msg.type=='error' else None)
        if args.stub:
            # The bridge fixture does not render Unity; isolate its capability gate.
            page.add_init_script('''const context = HTMLCanvasElement.prototype.getContext;
              HTMLCanvasElement.prototype.getContext = function(kind,...args) {
                return kind === "webgl2" ? {} : context.call(this,kind,...args);
              };
              HTMLElement.prototype.requestFullscreen = function(options) {
                window.fullscreenRequest = {id:this.id, options};
                return Promise.resolve("fullscreen-fixture");
              };
              window.testViewport = Object.assign(new EventTarget(), {width:720,height:1000,offsetLeft:0,offsetTop:0});
              Object.defineProperty(window,"visualViewport",{value:testViewport});''')
            root=Path(__file__).resolve().parents[1]/'Assets/WebGLTemplates/DokdoStandalone'
            def route(request):
                path=urlparse(request.request.url).path
                if path.endswith('/'):
                    content=(root/'index.html').read_text()
                    for var,value in [('LOADER_FILENAME','fixture.loader.js'),('DATA_FILENAME','fixture.data'),('FRAMEWORK_FILENAME','fixture.framework.js'),('CODE_FILENAME','fixture.wasm'),('COMPANY_NAME','Fixture'),('PRODUCT_NAME','Fixture'),('PRODUCT_VERSION','test')]:
                        content=content.replace('{{{ '+var+' }}}',value)
                    request.fulfill(body=content,content_type='text/html')
                elif path.endswith('fixture.loader.js'):
                    request.fulfill(body='''window.messages=[];window.masks=[];
                    window.createUnityInstance=async()=>{
                      dokdoKeyboard.register(mask=>masks.push(mask));dokdoKeyboard.setEnabled(true);
                      return {Module:{},SendMessage:(o,m,v)=>messages.push({method:m,value:v})};};''',content_type='text/javascript')
                else:
                    name=path.split('/')[-1]; candidate=root/'TemplateData'/name
                    if candidate.is_file(): request.fulfill(path=str(candidate))
                    else: request.abort()
            page.route('**/*',route)
        page.goto(args.url,wait_until='domcontentloaded')
        assert page.locator('#player-name').count()==0
        page.locator('#start-button').click()
        page.wait_for_function('!!window.dokdoUnityInstance || !document.getElementById("error-panel").hidden',timeout=240000)
        assert page.locator('#error-panel').is_hidden(),page.locator('#error-message').inner_text()
        record['checks']['startup']='passed'
        if args.stub:
            canvas=page.locator('#unity-canvas');canvas.focus()
            assert page.evaluate('document.getElementById("unity-canvas").requestFullscreen({navigationUI:"hide"})')=='fullscreen-fixture'
            assert page.evaluate('fullscreenRequest')=={'id':'dokdo-app','options':{'navigationUI':'hide'}}
            record['checks']['canvas-fullscreen-keeps-html-dialogs']='passed'
            page.keyboard.down('KeyW');page.keyboard.down('KeyD')
            assert page.evaluate('masks.slice(-1)[0]')=='9'
            page.keyboard.up('KeyW');page.keyboard.up('KeyD')
            assert page.evaluate('masks.slice(-1)[0]')=='0'
            page.keyboard.down('KeyA');canvas.click(position={'x':300,'y':300})
            assert page.evaluate('masks.slice(-1)[0]')=='0'
            page.keyboard.up('KeyA')
            page.keyboard.down('KeyS');page.evaluate('dispatchEvent(new Event("blur"))')
            assert page.evaluate('masks.slice(-1)[0]')=='0'
            page.keyboard.up('KeyS');page.evaluate('dispatchEvent(new Event("focus"))')
            page.evaluate('dispatchEvent(new CustomEvent("dokdo-manual-paste"))')
            page.evaluate('Object.assign(testViewport,{height:360,offsetTop:40});testViewport.dispatchEvent(new Event("resize"))')
            bounds=page.locator('#clipboard-panel').bounding_box()
            assert bounds['y']>=40 and bounds['y']+bounds['height']<=400
            record['checks']['clipboard-follows-visible-keyboard-area']='passed'
            page.evaluate('Object.assign(testViewport,{height:1000,offsetTop:0});testViewport.dispatchEvent(new Event("resize"))')
            page.locator('#clipboard-text').fill('WASD save text')
            page.keyboard.press('KeyW'); assert page.evaluate('masks.slice(-1)[0]')=='0'
            page.locator('#clipboard-text').fill('save-fixture')
            page.locator('#import-button').click()
            assert page.evaluate('messages.some(x=>x.method==="OnManualPaste"&&x.value==="save-fixture")')
            page.evaluate('dispatchEvent(new CustomEvent("dokdo-manual-paste"))')
            page.locator('#clipboard-close-button').click()
            assert page.evaluate('messages.filter(x=>x.method==="OnManualPaste").slice(-1)[0].value')==''
            record['checks']['keyboard-focus-and-handoff']='passed'
            record['checks']['manual-clipboard-import-and-cancel']='passed'
        else:
            page.wait_for_timeout(10000)
            page.screenshot(path=str(directory/(args.browser+'-startup.png')))
            page.evaluate('document.dispatchEvent(new Event("visibilitychange"))')
            page.reload(wait_until='domcontentloaded');page.locator('#start-button').click()
            page.wait_for_function('!!window.dokdoUnityInstance',timeout=240000)
            page.wait_for_timeout(5000)
            record['checks']['reload-startup']='passed'
        origin=urlparse(args.url)
        external=[r for r in record['requests'] if urlparse(r['url']).netloc != origin.netloc or r['method']!='GET' or '/api/' in r['url']]
        assert not external and not record['websockets'], external
        assert not record['console_errors'],record['console_errors']
        record['checks']['static-only-observed-network']='passed'
        record['ended_utc']=datetime.now(timezone.utc).isoformat()
        record['browser_version']=browser.version
        (directory/(args.browser+('-stub' if args.stub else '-startup')+'.json')).write_text(json.dumps(record,indent=2)+'\n')
        context.close();browser.close()
    print(json.dumps(record['checks']))

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('url');p.add_argument('--browser',choices=['chromium','firefox'],default='chromium')
    p.add_argument('--executable');p.add_argument('--stub',action='store_true')
    p.add_argument('--headed',action='store_true',help='Use a real or virtual display for WebGL')
    p.add_argument('--output',default='.artifacts/browser')
    run(p.parse_args())
