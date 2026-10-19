mergeInto(LibraryManager.library, {
  $DokdoBrowserStorage: {
    lastError: 0,
    classify: function (error) {
      if (!error) return 4;
      if (error.name === "QuotaExceededError" ||
          error.name === "NS_ERROR_DOM_QUOTA_REACHED") return 1;
      if (error.name === "SecurityError") return 2;
      return 4;
    }
  },

  DokdoStorageAvailable__deps: ["$DokdoBrowserStorage"],
  DokdoStorageAvailable: function () {
    DokdoBrowserStorage.lastError = 0;
    try {
      var key = "com.reconstruction.dokdo.web.storage-probe";
      localStorage.setItem(key, "1");
      localStorage.removeItem(key);
      return 1;
    } catch (error) {
      DokdoBrowserStorage.lastError = DokdoBrowserStorage.classify(error);
      return -1;
    }
  },

  DokdoStorageGet__deps: ["$DokdoBrowserStorage"],
  DokdoStorageGet: function (keyPointer) {
    DokdoBrowserStorage.lastError = 0;
    try {
      var value = localStorage.getItem(UTF8ToString(keyPointer));
      if (value === null) return 0;
      var length = lengthBytesUTF8(value) + 1;
      var pointer = _malloc(length);
      stringToUTF8(value, pointer, length);
      return pointer;
    } catch (error) {
      DokdoBrowserStorage.lastError = DokdoBrowserStorage.classify(error);
      return 0;
    }
  },

  DokdoStorageFree: function (pointer) {
    _free(pointer);
  },

  DokdoStorageSet__deps: ["$DokdoBrowserStorage"],
  DokdoStorageSet: function (keyPointer, valuePointer) {
    DokdoBrowserStorage.lastError = 0;
    try {
      localStorage.setItem(
        UTF8ToString(keyPointer), UTF8ToString(valuePointer));
      return 0;
    } catch (error) {
      DokdoBrowserStorage.lastError = DokdoBrowserStorage.classify(error);
      return DokdoBrowserStorage.lastError;
    }
  },

  DokdoStorageRemove__deps: ["$DokdoBrowserStorage"],
  DokdoStorageRemove: function (keyPointer) {
    DokdoBrowserStorage.lastError = 0;
    try {
      localStorage.removeItem(UTF8ToString(keyPointer));
      return 0;
    } catch (error) {
      DokdoBrowserStorage.lastError = DokdoBrowserStorage.classify(error);
      return DokdoBrowserStorage.lastError;
    }
  },

  DokdoStorageLastError__deps: ["$DokdoBrowserStorage"],
  DokdoStorageLastError: function () {
    return DokdoBrowserStorage.lastError;
  },

  DokdoBrowserWarning: function (messagePointer) {
    var message = UTF8ToString(messagePointer);
    window.dispatchEvent(new CustomEvent("dokdo-warning", {
      detail: { message: message }
    }));
  },

  DokdoClipboardWrite: function (valuePointer, receiverPointer) {
    if (window.dokdoKeyboard) window.dokdoKeyboard.clear();
    if (!window.isSecureContext || !navigator.clipboard ||
        !navigator.clipboard.writeText) return 0;
    var value = UTF8ToString(valuePointer);
    var receiver = UTF8ToString(receiverPointer);
    navigator.clipboard.writeText(value).then(function () {
      SendMessage(receiver, "OnClipboardWrite", "ok");
    }).catch(function (error) {
      SendMessage(receiver, "OnClipboardWrite",
        error && error.name ? error.name : "denied");
    });
    return 1;
  },

  DokdoClipboardRead: function (receiverPointer) {
    if (window.dokdoKeyboard) window.dokdoKeyboard.clear();
    if (!window.isSecureContext || !navigator.clipboard ||
        !navigator.clipboard.readText) return 0;
    var receiver = UTF8ToString(receiverPointer);
    navigator.clipboard.readText().then(function (value) {
      SendMessage(receiver, "OnClipboardRead", value);
    }).catch(function (error) {
      SendMessage(receiver, "OnClipboardReadFailed",
        error && error.name ? error.name : "denied");
    });
    return 1;
  },

  DokdoClipboardManualCopy: function (valuePointer) {
    window.dispatchEvent(new CustomEvent("dokdo-manual-copy", {
      detail: { value: UTF8ToString(valuePointer) }
    }));
  },

  DokdoClipboardManualPaste: function () {
    window.dispatchEvent(new CustomEvent("dokdo-manual-paste"));
  },

  DokdoKeyboardRegister: function (receiverPointer) {
    var keyboard = window.dokdoKeyboard;
    if (!keyboard || typeof keyboard.register !== "function" ||
        typeof keyboard.setEnabled !== "function") return 0;
    var receiver = UTF8ToString(receiverPointer);
    return keyboard.register(function (mask) {
      SendMessage(receiver, "OnKeyboardSteering", mask);
    });
  },

  DokdoKeyboardSetEnabled: function (enabled) {
    var keyboard = window.dokdoKeyboard;
    if (keyboard && typeof keyboard.setEnabled === "function") {
      keyboard.setEnabled(!!enabled);
    }
  },

  DokdoLifecycleInstall: function (receiverPointer) {
    if (window.__dokdoLifecycleInstalled) return;
    window.__dokdoLifecycleInstalled = true;
    var receiver = UTF8ToString(receiverPointer);
    document.addEventListener("visibilitychange", function () {
      if (document.visibilityState === "hidden") {
        SendMessage(receiver, "OnBrowserHidden", "visibilitychange");
      } else if (document.visibilityState === "visible") {
        SendMessage(receiver, "OnBrowserVisible", "visibilitychange");
      }
    });
    window.addEventListener("pagehide", function () {
      SendMessage(receiver, "OnBrowserHidden", "pagehide");
    });
    window.addEventListener("pageshow", function (event) {
      if (event.persisted || document.visibilityState === "visible") {
        SendMessage(receiver, "OnBrowserVisible", "pageshow");
      }
    });
  },

  DokdoPerformanceSample: function (fps, maximumFrameMilliseconds,
      allocatedBytes, reservedBytes) {
    var instance = window.dokdoUnityInstance;
    var module = instance && instance.Module;
    var heap = typeof HEAP8 !== "undefined"
      ? HEAP8.buffer
      : (module && module.HEAP8 && module.HEAP8.buffer);
    var sample = {
      framesPerSecond: fps,
      maximumFrameMilliseconds: maximumFrameMilliseconds,
      allocatedBytes: allocatedBytes,
      reservedBytes: reservedBytes,
      wasmHeapBytes: heap ? heap.byteLength : null,
      timestampMilliseconds: performance.now()
    };
    window.dokdoPerformanceSamples = window.dokdoPerformanceSamples || [];
    window.dokdoPerformanceSamples.push(sample);
    if (window.dokdoPerformanceSamples.length > 300) {
      window.dokdoPerformanceSamples.shift();
    }
    window.dispatchEvent(new CustomEvent("dokdo-performance", {
      detail: sample
    }));
  },

  DokdoSceneChanged: function (sceneNamePointer) {
    var sceneName = UTF8ToString(sceneNamePointer);
    window.dokdoSceneHistory = window.dokdoSceneHistory || [];
    window.dokdoSceneHistory.push(sceneName);
    window.dispatchEvent(new CustomEvent("dokdo-scene-changed", {
      detail: { sceneName: sceneName }
    }));
  },

  DokdoWaterPolicyApplied: function (simpleWater, changedWaterComponents) {
    window.dokdoWaterPolicy = {
      simpleWater: !!simpleWater,
      changedWaterComponents: changedWaterComponents
    };
  },

  DokdoResponsivePresentation: function (screenWidth, screenHeight,
      logicalWidth, logicalHeight, projectionHalfWidth,
      projectionHalfHeight, sourceOrthographicSize, orthographicSize,
      scalerCount, activeSafeTargets, visibleSafeTargets,
      offscreenSafeTargetsPointer) {
    window.dokdoResponsivePresentation = {
      screenWidth: screenWidth,
      screenHeight: screenHeight,
      logicalWidth: logicalWidth,
      logicalHeight: logicalHeight,
      projectionHalfWidth: projectionHalfWidth,
      projectionHalfHeight: projectionHalfHeight,
      sourceOrthographicSize: sourceOrthographicSize,
      orthographicSize: orthographicSize,
      scalerCount: scalerCount,
      activeSafeTargets: activeSafeTargets,
      visibleSafeTargets: visibleSafeTargets,
      offscreenSafeTargets: UTF8ToString(offscreenSafeTargetsPointer),
      timestampMilliseconds: performance.now()
    };
  },

  DokdoLifecycleState: function (visibilityBlocked, orientationBlocked,
      blocked, blockTransitions, resumeTransitions, timeScale, audioPaused) {
    window.dokdoLifecycleState = {
      visibilityBlocked: !!visibilityBlocked,
      orientationBlocked: !!orientationBlocked,
      blocked: !!blocked,
      blockTransitions: blockTransitions,
      resumeTransitions: resumeTransitions,
      timeScale: timeScale,
      audioPaused: !!audioPaused,
      timestampMilliseconds: performance.now()
    };
  }
});
