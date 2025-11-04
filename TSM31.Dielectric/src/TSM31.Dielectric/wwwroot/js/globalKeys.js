// Global keyboard handler for Blazor (browser + WebView)
// - Attaches a window keydown listener (capture phase) to reliably catch function keys
// - Selectively preventDefault for problematic keys (F1, F5, etc.)
// - Skips handling when a modal dialog is open to avoid fighting with focused UI

let _handler = null;
let _dotNetRef = null;

const FUNCTION_KEYS = new Set([
    'F1', 'F2', 'F3', 'F4', 'F5', 'F6', 'F7', 'F8', 'F9', 'F10', 'F11', 'F12', 'Escape', 'Esc'
]);

function hasOpenDialog() {
    // FAST <fluent-dialog> uses [open] when visible. Also check generic ARIA patterns.
    try {
        return !!(document.querySelector('fluent-dialog[open]')
            || document.querySelector('[role="dialog"][aria-hidden="false"]')
            || document.querySelector('[role="dialog"][open]'));
    } catch {
        return false;
    }
}

function normalizeKey(key) {
    if (!key) return '';
    if (key === 'Esc') return 'Escape';
    return key;
}

export function registerGlobalKeys(dotNetRef, options) {
    if (_handler) return; // already registered
    _dotNetRef = dotNetRef;

    const opts = {
        preventDefaults: true,
        preventList: ['F1', 'F3', 'F4', 'F5', 'F6', 'F7', 'F8', 'F9', 'F10', 'F11', 'F12', 'Escape'],
        ...options
    };

    _handler = (e) => {
        const key = normalizeKey(e.key);
        if (!FUNCTION_KEYS.has(key)) return; // ignore other keys

        // If a modal dialog is open, let it handle Escape/other keys.
        if (hasOpenDialog()) {
            return;
        }

        // Optionally prevent browser defaults for selected keys
        if (opts.preventDefaults && opts.preventList.includes(key)) {
            try { e.preventDefault(); } catch { }
        }

        // Invoke .NET handler
        try {
            if (_dotNetRef) {
                _dotNetRef.invokeMethodAsync('OnGlobalKey', key);
            }
        } catch (err) {
            // swallow to avoid breaking key handling
            // console.error('OnGlobalKey invoke failed', err);
        }
    };

    // capture=true to catch keys before controls can stop propagation
    window.addEventListener('keydown', _handler, { capture: true });
}

export function unregisterGlobalKeys() {
    if (_handler) {
        window.removeEventListener('keydown', _handler, { capture: true });
    }
    _handler = null;
    _dotNetRef = null;
}
