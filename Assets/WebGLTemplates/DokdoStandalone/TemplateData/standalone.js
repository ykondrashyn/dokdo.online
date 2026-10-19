(function () {
  "use strict";
  const get = id => document.getElementById(id);
  const app = get("dokdo-app"), canvas = get("unity-canvas");
  const clipboard = get("clipboard-panel"), text = get("clipboard-text");
  // Unity can request fullscreen directly on its canvas. Include the HTML
  // save-transfer dialog and preserve the native method's arguments/return.
  for (const method of ["requestFullscreen", "webkitRequestFullscreen",
      "mozRequestFullScreen", "msRequestFullscreen"]) {
    if (typeof canvas[method] !== "function" || typeof app[method] !== "function") continue;
    Object.defineProperty(canvas, method, {configurable: true, value: function (...args) {
      return app[method].apply(app, args);
    }});
  }
  let instance = null, sender = null, enabled = false, mask = 0;
  let composing = false, focused = true, loading = false, blocked = false;
  let landscape = false, pasteMode = false;
  const coarse = matchMedia("(pointer: coarse)").matches;
  const bits = {KeyW: 1, KeyA: 2, KeyS: 4, KeyD: 8};
  const send = (method, value) => instance && instance.SendMessage("DOKDO Web Platform", method, value);
  function setMask(value) { if (value !== mask) { mask = value; if (sender) sender(String(mask)); } }
  function clear() { setMask(0); }
  window.dokdoKeyboard = {
    register(callback) { clear(); sender = callback; enabled = false; return 1; },
    setEnabled(value) { enabled = !!value; if (!enabled) clear(); }, clear
  };
  function typing(node) { return node && (node.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(node.tagName)); }
  document.addEventListener("keydown", event => {
    if (event.ctrlKey || event.altKey || event.metaKey || event.isComposing) { clear(); return; }
    if (event.key === "Escape" && !clipboard.hidden) { closeClipboard(); return; }
    if (event.key === "Tab" && !clipboard.hidden) {
      const nodes = [...clipboard.querySelectorAll("button,textarea")].filter(node => !node.hidden);
      const next = (nodes.indexOf(document.activeElement) + (event.shiftKey ? -1 : 1) + nodes.length) % nodes.length;
      event.preventDefault(); nodes[next].focus(); return;
    }
    const bit = bits[event.code];
    if (!bit || event.repeat || !enabled || !focused || composing || !document.hasFocus() ||
        document.activeElement !== canvas || document.visibilityState !== "visible" ||
        !instance || loading || blocked || !clipboard.hidden || typing(event.target)) return;
    event.preventDefault(); setMask(mask | bit);
  });
  document.addEventListener("keyup", event => { if (bits[event.code]) setMask(mask & ~bits[event.code]); });
  canvas.addEventListener("blur", clear);
  window.addEventListener("blur", () => { focused = false; clear(); });
  window.addEventListener("focus", () => { focused = true; });
  for (const event of ["pointerdown", "mousedown", "touchstart", "pagehide"])
    window.addEventListener(event, clear, true);
  for (const event of ["visibilitychange", "fullscreenchange"])
    document.addEventListener(event, clear);
  document.addEventListener("compositionstart", () => { composing = true; clear(); }, true);
  document.addEventListener("compositionend", () => { composing = false; }, true);
  canvas.addEventListener("pointerdown", () => canvas.focus());
  canvas.addEventListener("wheel", event => {
    if (!instance || !clipboard.hidden) return;
    event.preventDefault(); clear();
    send("OnMouseWheelZoom", String(event.deltaY * (event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? innerHeight : 1)));
  }, {passive: false});
  function warning(value) { get("unity-warning").textContent = String(value); }
  function presentation() {
    const type = screen.orientation && screen.orientation.type;
    if (type && type.startsWith("portrait")) landscape = false;
    else if (type && type.startsWith("landscape")) landscape = true;
    else if (window.orientation === 0 || window.orientation === 180) landscape = false;
    else if (window.orientation === 90 || window.orientation === -90) landscape = true;
    else if (screen.width !== screen.height) landscape = screen.width > screen.height;
    blocked = coarse && landscape;
    get("orientation-panel").hidden = !blocked;
    if (blocked) clear();
    const view = window.visualViewport;
    const shell = get("game-shell");
    shell.style.left = (view ? view.offsetLeft : 0) + "px";
    shell.style.top = (view ? view.offsetTop : 0) + "px";
    shell.style.width = (view ? view.width : innerWidth) + "px";
    shell.style.height = (view ? view.height : innerHeight) + "px";
    const rect = canvas.getBoundingClientRect();
    const dpr = Math.max(.25, Math.min(devicePixelRatio || 1, coarse ? 1.5 : 2,
      Math.sqrt((coarse ? 1500000 : 2500000) / Math.max(1, rect.width * rect.height))));
    if (instance && instance.Module) instance.Module.devicePixelRatio = dpr;
    send("OnOrientationBlocked", blocked ? "1" : "0");
    const safe = getComputedStyle(get("safe-area-probe"));
    if (rect.width && rect.height) send("OnSafeAreaChanged", [safe.paddingTop, safe.paddingRight,
      safe.paddingBottom, safe.paddingLeft].map(parseFloat).concat([rect.width, rect.height]).join(","));
  }
  window.addEventListener("resize", presentation);
  window.addEventListener("orientationchange", presentation);
  document.addEventListener("fullscreenchange", presentation);
  if (window.visualViewport) {
    visualViewport.addEventListener("resize", presentation);
    visualViewport.addEventListener("scroll", presentation);
  }
  get("fullscreen-button").onclick = async () => {
    clear(); try {
      if (document.fullscreenElement) await document.exitFullscreen();
      else await app.requestFullscreen();
    } catch (_) { warning("Full screen is unavailable in this browser."); }
  };
  function showError(error) {
    clear(); loading = false; get("game-shell").hidden = true;
    get("error-panel").hidden = false; get("error-message").textContent = String(error);
  }
  get("retry-button").onclick = () => location.reload();
  get("start-button").onclick = async () => {
    if (loading || instance) return;
    if (!window.WebAssembly || !document.createElement("canvas").getContext("webgl2")) {
      showError("This game needs WebGL 2 and WebAssembly."); return;
    }
    loading = true; get("start-panel").hidden = true; get("game-shell").hidden = false;
    presentation();
    try {
      await new Promise((resolve, reject) => {
        const script = document.createElement("script"); script.src = app.dataset.loader;
        script.onload = resolve; script.onerror = () => reject(new Error("The game loader could not be loaded. Check the extracted files and HTTP server."));
        document.body.appendChild(script);
      });
      instance = await createUnityInstance(canvas, {
        dataUrl: app.dataset.data, frameworkUrl: app.dataset.framework, codeUrl: app.dataset.code,
        streamingAssetsUrl: "StreamingAssets", companyName: app.dataset.company,
        productName: app.dataset.product, productVersion: app.dataset.version,
        devicePixelRatio: Math.min(devicePixelRatio || 1, coarse ? 1.5 : 2),
        showBanner: warning
      }, progress => { get("loading-progress").value = progress; });
      window.dokdoUnityInstance = instance;
      loading = false; get("loading-panel").hidden = true;
      send("OnCapabilities", coarse ? "1" : "0"); presentation(); canvas.focus();
    } catch (error) { showError(error); }
  };
  function openClipboard(paste, value) {
    clear(); pasteMode = paste; clipboard.hidden = false;
    get("clipboard-title").textContent = paste ? "Paste save" : "Copy save";
    get("clipboard-help").textContent = paste ? "Paste the complete save text, then import it." : "Copy the selected text and keep it somewhere safe.";
    text.value = value || ""; text.readOnly = !paste;
    get("copy-button").hidden = paste; get("paste-button").hidden = !paste;
    get("import-button").hidden = !paste; text.focus(); text.select();
  }
  function closeClipboard() {
    if (pasteMode) send("OnManualPaste", "");
    clipboard.hidden = true; text.value = ""; canvas.focus();
  }
  get("clipboard-close-button").onclick = closeClipboard;
  get("copy-button").onclick = async () => {
    try { await navigator.clipboard.writeText(text.value); }
    catch (_) { text.focus(); text.select(); warning("Use your browser's Copy command on the selected text."); }
  };
  get("paste-button").onclick = async () => {
    try { text.value = await navigator.clipboard.readText(); }
    catch (_) { text.focus(); warning("Paste into the text box using your browser's Paste command."); }
  };
  get("import-button").onclick = () => {
    send("OnManualPaste", text.value); pasteMode = false; closeClipboard();
  };
  window.addEventListener("dokdo-warning", event => warning(event.detail.message));
  window.addEventListener("dokdo-manual-copy", event => openClipboard(false, event.detail.value));
  window.addEventListener("dokdo-manual-paste", () => openClipboard(true, ""));
})();
