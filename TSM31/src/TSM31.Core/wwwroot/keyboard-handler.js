// Global Function Key Reservation for Blazor Navigation (F1 - F10)
// Prevents browser defaults (Help, Refresh, Focus Address Bar, Caret Mode, Menu activation, etc.)
// while still allowing Blazor components to receive and process the key events.
(function () {
    'use strict';

    const reservedRange = { min: 1, max: 10 }; // Reserve F1-F10 only

    // Helper predicates
    function getFunctionKeyNumber(upperKey) {
        // Match F1 - F12 exactly; reject plain 'F'
        const m = /^F(\d{1,2})$/.exec(upperKey);
        if (!m) return NaN;
        const n = parseInt(m[1], 10);
        return (n >= 1 && n <= 12) ? n : NaN;
    }
    function isEscapeKey(upperKey) {
        return upperKey === 'ESCAPE' || upperKey === 'ESC';
    }

    console.log('🎹 Keyboard handler initialized - Reserving F1-F10');
    console.log('📋 Browser defaults will be suppressed, events will propagate to Blazor');

    document.addEventListener('keydown', function (e) {
        const rawKey = e.key || '';
        const key = rawKey.toUpperCase();

        const fnNumber = getFunctionKeyNumber(key);
        const isFnKey = !Number.isNaN(fnNumber);
        const isEsc = isEscapeKey(key);

        // Handle reload shortcuts (Ctrl+R, F5, Ctrl+Shift+R)
        // In Blazor Hybrid apps with autostart="false", page reloads break the .NET-JS bridge
        // because initialization must come from the C# host, not from JavaScript.
        // Therefore, we completely block reload shortcuts to prevent errors.
        const isReloadKey = (e.ctrlKey && key === 'R') || key === 'F5' || (e.ctrlKey && e.shiftKey && key === 'R');

        if (isReloadKey) {
            console.log('⛔ Reload shortcut blocked (not supported in Blazor Hybrid):', {
                key: rawKey,
                ctrlKey: e.ctrlKey,
                shiftKey: e.shiftKey,
                target: e.target.tagName,
                reason: 'Page reloads break .NET-JS interop bridge',
                timestamp: new Date().toISOString()
            });
            // Prevent reload completely - restart the app if refresh is needed
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            console.log('💡 To refresh the app, please restart it instead of using Ctrl+R');
            return;
        }

        // Log only Escape or true function keys (F1-F12); exclude plain 'F' or other single chars
        if (isFnKey || isEsc) {
            console.log('🔑 Special key detected:', {
                key: rawKey,
                normalized: key,
                fnNumber: isFnKey ? fnNumber : null,
                isEscape: isEsc,
                code: e.code,
                target: e.target.tagName,
                targetId: e.target.id,
                targetClass: e.target.className,
                defaultPrevented: e.defaultPrevented,
                propagationStopped: e.cancelBubble,
                timestamp: new Date().toISOString()
            });
        }

        // Only continue with suppression logic for real function keys (not Escape)
        if (!isFnKey) return;

        if (fnNumber < reservedRange.min || fnNumber > reservedRange.max) {
            // Allow browser defaults for F11/F12 (or outside reserved range if changed)
            console.log('ℹ️ Function key outside reserved range:', key, '- allowing browser default');
            return;
        }

        // Prevent default browser behavior (Help, Refresh, etc.) but keep propagation so Blazor handlers run.
        console.log('✅ Preventing default for:', key, '- Event will propagate to Blazor');
        e.preventDefault();

        // Verify the event is still propagating
        console.log('🔄 Event propagation status:', {
            bubbles: e.bubbles,
            cancelable: e.cancelable,
            defaultPrevented: e.defaultPrevented,
            eventPhase: e.eventPhase,
            isTrusted: e.isTrusted
        });

    }, true); // capture phase for earlier interception

    // Also listen in bubble phase to see if Blazor receives the event
    document.addEventListener('keydown', function (e) {
        const key = (e.key || '').toUpperCase();
        const fnNumber = getFunctionKeyNumber(key);
        if (!Number.isNaN(fnNumber) && fnNumber >= reservedRange.min && fnNumber <= reservedRange.max) {
            console.log('📬 Event reached bubble phase (after Blazor should process):', key);
        }
    }, false); // bubble phase

    // General diagnostic logger (unchanged; still logs everything, but we could limit if too noisy)
    document.addEventListener('keydown', function (e) {
        const key = (e.key || '').toUpperCase();
        console.log('📝 Keydown event:', {
            key: e.key,
            keyUpper: key,
            code: e.code,
            ctrl: e.ctrlKey,
            alt: e.altKey,
            shift: e.shiftKey,
            meta: e.metaKey,
            target: e.target.tagName,
            targetType: e.target.type,
            targetId: e.target.id,
            targetClass: e.target.className,
            isContentEditable: e.target.isContentEditable,
            defaultPrevented: e.defaultPrevented,
            timestamp: new Date().toISOString()
        });
    }, true);

    console.log('✅ Keyboard handler setup complete: Reserved F1-F10 (browser defaults suppressed, events propagate to Blazor).');

    // Handle page reload events
    window.addEventListener('beforeunload', function (e) {
        console.log('📢 Page reload initiated - cleaning up resources...');
        // Perform any cleanup needed before page reload
        // Don't set returnValue to avoid showing confirmation dialog
    });

    window.addEventListener('unload', function (e) {
        console.log('📢 Page unloading - final cleanup...');
    });

    // Signal that the page has loaded/reloaded
    window.addEventListener('load', function (e) {
        console.log('📢 Page load complete - Waiting for Blazor initialization from host...');
        console.log('ℹ️ Note: Blazor is started by the C# host (Program.cs), not from JavaScript');
    });
})();
