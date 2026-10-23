// HabitTracker front-end behaviour.
// Plain JavaScript on purpose: Bootstrap's bundle is the only dependency (plus Chart.js on chart pages).

(function () {
    'use strict';

    const HT = window.HT = window.HT || {};
    const root = document.documentElement;
    const THEME_KEY = 'ht-theme';

    const storage = {
        get(key) {
            try { return localStorage.getItem(key); } catch (e) { return null; }
        },
        set(key, value) {
            try { localStorage.setItem(key, value); } catch (e) { /* private mode */ }
        }
    };

    // ---------------------------------------------------------------------
    // Theme (the initial value is applied by an inline script in <head>)
    // ---------------------------------------------------------------------

    const themeListeners = [];

    HT.onThemeChange = function (fn) {
        themeListeners.push(fn);
    };

    function applyTheme(theme) {
        root.setAttribute('data-bs-theme', theme);
        storage.set(THEME_KEY, theme);
        themeListeners.forEach(fn => fn(theme));
    }

    document.querySelectorAll('[data-theme-toggle]').forEach(btn => {
        btn.addEventListener('click', () => {
            applyTheme(root.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark');
        });
    });

    // Follow the OS setting until a theme has been picked manually
    if (window.matchMedia) {
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
            if (!storage.get(THEME_KEY)) {
                root.setAttribute('data-bs-theme', e.matches ? 'dark' : 'light');
                themeListeners.forEach(fn => fn(e.matches ? 'dark' : 'light'));
            }
        });
    }

    HT.cssVar = function (name) {
        return getComputedStyle(root).getPropertyValue(name).trim();
    };

    // ---------------------------------------------------------------------
    // Toasts
    // ---------------------------------------------------------------------

    const toastIcons = { success: 'bi-check2-circle', error: 'bi-exclamation-triangle', info: 'bi-bell' };

    function toastStack() {
        let stack = document.querySelector('.toast-stack');
        if (!stack) {
            stack = document.createElement('div');
            stack.className = 'toast-stack';
            stack.setAttribute('aria-live', 'polite');
            document.body.appendChild(stack);
        }
        return stack;
    }

    function dismissToast(toast) {
        if (!toast || toast.classList.contains('hide')) return;
        toast.classList.add('hide');
        toast.addEventListener('animationend', () => toast.remove(), { once: true });
    }

    function armToast(toast, delay) {
        toast.querySelector('.btn-close')?.addEventListener('click', () => dismissToast(toast));
        setTimeout(() => dismissToast(toast), delay || 4500);
    }

    HT.toast = function (message, type) {
        type = type || 'success';
        const toast = document.createElement('div');
        toast.className = 'app-toast ' + type;
        toast.setAttribute('role', 'status');

        const icon = document.createElement('span');
        icon.className = 'toast-icon';
        icon.innerHTML = '<i class="bi ' + (toastIcons[type] || toastIcons.info) + '"></i>';

        const text = document.createElement('span');
        text.textContent = message;

        const close = document.createElement('button');
        close.type = 'button';
        close.className = 'btn-close';
        close.setAttribute('aria-label', 'Close');

        toast.append(icon, text, close);
        toastStack().appendChild(toast);
        armToast(toast);
    };

    // Toasts rendered by the server from TempData
    document.querySelectorAll('.toast-stack .app-toast').forEach(t => armToast(t, 5000));

    // ---------------------------------------------------------------------
    // Small helpers
    // ---------------------------------------------------------------------

    function antiForgeryToken() {
        const input = document.querySelector('#af-token input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function todayIso() {
        const d = new Date();
        const pad = n => String(n).padStart(2, '0');
        return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate());
    }

    function formatNumber(value, decimals) {
        return Number(value).toLocaleString(undefined, {
            minimumFractionDigits: decimals,
            maximumFractionDigits: decimals
        });
    }

    // Animated counters: <span data-count-to="42" data-decimals="1" data-suffix="%">
    function animateCount(el, target) {
        const decimals = parseInt(el.dataset.decimals || '0', 10);
        const suffix = el.dataset.suffix || '';
        const start = parseFloat(el.dataset.current || '0');
        const duration = 900;
        const startedAt = performance.now();

        function frame(now) {
            const progress = Math.min((now - startedAt) / duration, 1);
            const eased = 1 - Math.pow(1 - progress, 3);
            el.textContent = formatNumber(start + (target - start) * eased, decimals) + suffix;
            if (progress < 1) requestAnimationFrame(frame);
        }

        el.dataset.current = target;
        requestAnimationFrame(frame);
    }

    HT.animateCount = animateCount;

    document.querySelectorAll('[data-count-to]').forEach(el => {
        animateCount(el, parseFloat(el.dataset.countTo) || 0);
    });

    // Progress rings: <div class="progress-ring" data-ring="65">
    function setRing(ring, percent) {
        const circle = ring.querySelector('.ring-value');
        if (!circle) return;
        const radius = circle.r.baseVal.value;
        const circumference = 2 * Math.PI * radius;
        circle.style.strokeDasharray = circumference;
        circle.style.strokeDashoffset = circumference * (1 - Math.max(0, Math.min(100, percent)) / 100);
        ring.dataset.ring = percent;
    }

    HT.setRing = setRing;

    document.querySelectorAll('[data-ring]').forEach(ring => {
        const circle = ring.querySelector('.ring-value');
        if (circle) {
            const circumference = 2 * Math.PI * circle.r.baseVal.value;
            circle.style.strokeDasharray = circumference;
            circle.style.strokeDashoffset = circumference;
        }
        // Let the browser paint the empty ring first so the fill animates
        requestAnimationFrame(() => requestAnimationFrame(() => setRing(ring, parseFloat(ring.dataset.ring) || 0)));
    });

    // Progress bars: <div class="progress-bar" data-width="65">
    requestAnimationFrame(() => {
        document.querySelectorAll('.progress-bar[data-width]').forEach(bar => {
            bar.style.width = Math.min(100, parseFloat(bar.dataset.width) || 0) + '%';
        });
    });

    // ---------------------------------------------------------------------
    // Habit check-in (buttons, day dots and heatmap cells share the same API)
    // ---------------------------------------------------------------------

    document.addEventListener('click', async e => {
        const trigger = e.target.closest('[data-toggle-habit]');
        if (!trigger) return;

        e.preventDefault();
        if (trigger.classList.contains('is-busy')) return;

        const habitId = trigger.dataset.habitId;
        const date = trigger.dataset.date || todayIso();
        const body = new URLSearchParams({ date: date, __RequestVerificationToken: antiForgeryToken() });

        trigger.classList.add('is-busy');
        try {
            const response = await fetch(trigger.dataset.url, {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: body
            });

            if (!response.ok) {
                let message = 'Something went wrong, please try again.';
                try { message = (await response.json()).message || message; } catch (err) { /* not JSON */ }
                HT.toast(message, 'error');
                return;
            }

            const result = await response.json();
            updateHabitUi(habitId, result);

            if (result.completed) {
                trigger.classList.add('pop');
                trigger.addEventListener('animationend', () => trigger.classList.remove('pop'), { once: true });
            }

            const isToday = result.date === todayIso();
            if (result.completed) {
                const streakText = result.streak > 1 ? ' ' + result.streak + ' days streak 🔥' : '';
                HT.toast('"' + result.name + '" done' + (isToday ? ' for today!' : '.') + streakText, 'success');
            } else {
                HT.toast('"' + result.name + '" unchecked.', 'info');
            }

            if (result.completed && isToday) celebrateIfAllDone();
        } catch (err) {
            HT.toast('Connection problem, the change was not saved.', 'error');
        } finally {
            trigger.classList.remove('is-busy');
        }
    });

    function updateHabitUi(habitId, result) {
        // Every trigger pointing at the same habit + day
        document.querySelectorAll('[data-toggle-habit][data-habit-id="' + habitId + '"][data-date="' + result.date + '"]').forEach(el => {
            el.classList.toggle('is-done', result.completed);
            el.classList.toggle('is-missed', !result.completed && el.matches('.day-dot, .heat'));
            el.setAttribute('aria-pressed', result.completed ? 'true' : 'false');
            if (el.title) {
                el.title = el.title.replace(/ · .*$/, '') + (result.completed ? ' · done' : ' · missed');
            }
        });

        document.querySelectorAll('[data-habit-card="' + habitId + '"]').forEach(card => {
            card.classList.toggle('is-done', result.doneToday);
        });

        document.querySelectorAll('[data-habit-streak="' + habitId + '"]').forEach(el => {
            const value = el.querySelector('[data-value]') || el;
            if (value.textContent !== String(result.streak)) {
                value.textContent = result.streak;
                el.classList.remove('bump');
                void el.offsetWidth;
                el.classList.add('bump');
            }
            el.classList.toggle('is-cold', result.streak === 0);
        });

        document.querySelectorAll('[data-habit-best="' + habitId + '"]').forEach(el => { el.textContent = result.bestStreak; });
        document.querySelectorAll('[data-habit-total="' + habitId + '"]').forEach(el => { el.textContent = result.total; });
        document.querySelectorAll('[data-habit-rate="' + habitId + '"]').forEach(el => {
            el.textContent = formatNumber(result.rate, el.dataset.decimals ? parseInt(el.dataset.decimals, 10) : 0) + '%';
        });
        document.querySelectorAll('[data-habit-rate-bar="' + habitId + '"]').forEach(bar => { bar.style.width = result.rate + '%'; });
        document.querySelectorAll('[data-habit-status="' + habitId + '"]').forEach(el => {
            el.dataset.status = result.doneToday ? 'done' : 'todo';
        });

        refreshTodayProgress();
        applyFilters();
    }

    // Dashboard "today" widgets
    function refreshTodayProgress() {
        const items = document.querySelectorAll('.today-item');
        if (!items.length) return;

        items.forEach(item => {
            const button = item.querySelector('.check-btn');
            item.classList.toggle('is-done', !!button && button.classList.contains('is-done'));
        });

        const done = document.querySelectorAll('.today-item.is-done').length;
        const total = items.length;
        const percent = total === 0 ? 0 : Math.round(done * 100 / total);

        document.querySelectorAll('[data-today-done]').forEach(el => animateCount(el, done));
        document.querySelectorAll('[data-today-percent]').forEach(el => animateCount(el, percent));
        document.querySelectorAll('[data-today-ring]').forEach(ring => setRing(ring, percent));
        document.querySelectorAll('[data-today-message]').forEach(el => {
            el.textContent = done === total
                ? 'Everything is done for today. Impressive!'
                : (total - done) + ' habit' + (total - done > 1 ? 's' : '') + ' left for today. Keep going!';
        });
    }

    function celebrateIfAllDone() {
        const items = document.querySelectorAll('[data-habit-card], .today-item');
        if (!items.length) return;
        const allDone = Array.from(items).every(el => el.classList.contains('is-done'));
        if (allDone) {
            HT.confetti();
            setTimeout(() => HT.toast('All habits done for today. Amazing work! 🎉', 'success'), 400);
        }
    }

    // ---------------------------------------------------------------------
    // Client-side filtering (search box + status pills)
    // ---------------------------------------------------------------------

    const filterState = { text: '', status: 'all' };

    function applyFilters() {
        const items = document.querySelectorAll('[data-filter-item]');
        if (!items.length) return;

        let visible = 0;
        items.forEach(item => {
            const haystack = (item.dataset.filterText || item.textContent).toLowerCase();
            const statusEl = item.matches('[data-status]') ? item : item.querySelector('[data-status]');
            const status = statusEl ? statusEl.dataset.status : 'all';
            const matches = haystack.includes(filterState.text) &&
                (filterState.status === 'all' || filterState.status === status);

            item.classList.toggle('d-none', !matches);
            if (matches) visible++;
        });

        document.querySelectorAll('[data-filter-empty]').forEach(el => el.classList.toggle('d-none', visible > 0));
        document.querySelectorAll('[data-filter-count]').forEach(el => { el.textContent = visible; });
    }

    document.querySelectorAll('[data-filter-input]').forEach(input => {
        input.addEventListener('input', () => {
            filterState.text = input.value.trim().toLowerCase();
            applyFilters();
        });
    });

    document.querySelectorAll('[data-status-filter]').forEach(pill => {
        pill.addEventListener('click', () => {
            document.querySelectorAll('[data-status-filter]').forEach(p => p.classList.remove('active'));
            pill.classList.add('active');
            filterState.status = pill.dataset.statusFilter;
            applyFilters();
        });
    });

    // "/" jumps to the search box, like on most modern apps
    document.addEventListener('keydown', e => {
        if (e.key !== '/' || e.ctrlKey || e.metaKey) return;
        const tag = (document.activeElement && document.activeElement.tagName) || '';
        if (['INPUT', 'TEXTAREA', 'SELECT'].includes(tag)) return;
        const search = document.querySelector('[data-filter-input]');
        if (search) {
            e.preventDefault();
            search.focus();
        }
    });

    // ---------------------------------------------------------------------
    // Forms: password visibility, strength meter, character counter
    // ---------------------------------------------------------------------

    document.querySelectorAll('[data-password-toggle]').forEach(btn => {
        btn.addEventListener('click', () => {
            const input = document.getElementById(btn.dataset.passwordToggle);
            if (!input) return;
            const show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            btn.querySelector('i').className = show ? 'bi bi-eye-slash' : 'bi bi-eye';
            btn.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
        });
    });

    document.querySelectorAll('[data-password-strength]').forEach(meter => {
        const input = document.getElementById(meter.dataset.passwordStrength);
        const label = document.querySelector('[data-strength-label="' + meter.dataset.passwordStrength + '"]');
        if (!input) return;

        const names = ['', 'Weak', 'Fair', 'Good', 'Strong'];
        input.addEventListener('input', () => {
            const value = input.value;
            let score = 0;
            if (value.length >= 6) score++;
            if (value.length >= 10) score++;
            if (/[A-Z]/.test(value) && /[a-z]/.test(value)) score++;
            if (/\d/.test(value) && /[^A-Za-z0-9]/.test(value)) score++;
            if (value.length > 0 && score === 0) score = 1;

            meter.dataset.level = value.length ? score : 0;
            if (label) label.textContent = value.length ? names[score] : '';
        });
    });

    document.querySelectorAll('[data-char-count]').forEach(input => {
        const output = document.querySelector('[data-char-output="' + input.id + '"]');
        const max = parseInt(input.getAttribute('maxlength') || input.dataset.charCount, 10);
        if (!output) return;
        const update = () => { output.textContent = input.value.length + ' / ' + max; };
        input.addEventListener('input', update);
        update();
    });

    // Submit buttons show a spinner so double clicks don't post twice
    document.querySelectorAll('form[data-loading]').forEach(form => {
        form.addEventListener('submit', () => {
            if (window.jQuery && jQuery(form).valid && !jQuery(form).valid()) return;
            const button = form.querySelector('[type="submit"]');
            if (!button) return;
            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>' + (button.dataset.loadingText || 'Saving...');
        });
    });

    // ---------------------------------------------------------------------
    // Relative dates: <time data-relative datetime="2025-05-01T10:00:00">
    // ---------------------------------------------------------------------

    const rtf = window.Intl && Intl.RelativeTimeFormat ? new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' }) : null;

    function relativeLabel(date) {
        const diff = (date.getTime() - Date.now()) / 1000;
        const units = [['year', 31536000], ['month', 2592000], ['week', 604800], ['day', 86400], ['hour', 3600], ['minute', 60]];
        for (const [unit, seconds] of units) {
            if (Math.abs(diff) >= seconds || unit === 'minute') {
                const value = Math.round(diff / seconds);
                return rtf ? rtf.format(value, unit) : date.toLocaleString();
            }
        }
        return '';
    }

    function refreshRelativeTimes() {
        document.querySelectorAll('[data-relative]').forEach(el => {
            const date = new Date(el.getAttribute('datetime'));
            if (!isNaN(date)) el.textContent = relativeLabel(date);
        });
    }

    refreshRelativeTimes();
    setInterval(refreshRelativeTimes, 60000);

    // ---------------------------------------------------------------------
    // Layout bits
    // ---------------------------------------------------------------------

    const topbar = document.querySelector('.app-topbar');
    if (topbar) {
        const onScroll = () => topbar.classList.toggle('scrolled', window.scrollY > 4);
        window.addEventListener('scroll', onScroll, { passive: true });
        onScroll();
    }

    // Mobile sidebar
    document.querySelectorAll('[data-sidebar-toggle]').forEach(btn => {
        btn.addEventListener('click', () => document.body.classList.toggle('sidebar-open'));
    });
    document.querySelector('.sidebar-backdrop')?.addEventListener('click', () => document.body.classList.remove('sidebar-open'));
    document.addEventListener('keydown', e => {
        if (e.key === 'Escape') document.body.classList.remove('sidebar-open');
    });

    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
        if (window.bootstrap) new bootstrap.Tooltip(el);
    });

    // ---------------------------------------------------------------------
    // Reminder notifications while the app is open
    // ---------------------------------------------------------------------

    const remindersUrl = document.body.dataset.remindersUrl;
    const NOTIFIED_KEY = 'ht-notified';

    function notifiedIds() {
        try { return JSON.parse(sessionStorage.getItem(NOTIFIED_KEY) || '[]'); } catch (e) { return []; }
    }

    function markNotified(id) {
        const ids = notifiedIds();
        ids.push(id);
        try { sessionStorage.setItem(NOTIFIED_KEY, JSON.stringify(ids.slice(-100))); } catch (e) { /* ignore */ }
    }

    function fireReminder(reminder) {
        if (notifiedIds().includes(reminder.id)) return;
        markNotified(reminder.id);

        HT.toast('⏰ ' + reminder.habit + ': ' + reminder.message, 'info');
        if ('Notification' in window && Notification.permission === 'granted') {
            new Notification('HabitTracker · ' + reminder.habit, { body: reminder.message });
        }
    }

    const scheduled = new Set();

    async function pollReminders() {
        try {
            const response = await fetch(remindersUrl, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            if (!response.ok) return;
            const reminders = await response.json();
            const now = Date.now();

            reminders.forEach(reminder => {
                const delay = new Date(reminder.time).getTime() - now;
                // Only schedule what falls before the next poll, older ones fire right away
                if (delay < 5 * 60 * 1000 && !scheduled.has(reminder.id)) {
                    scheduled.add(reminder.id);
                    setTimeout(() => fireReminder(reminder), Math.max(0, delay));
                }
            });
        } catch (e) { /* offline, try again later */ }
    }

    if (remindersUrl) {
        pollReminders();
        setInterval(pollReminders, 4 * 60 * 1000);
    }

    document.querySelectorAll('[data-enable-notifications]').forEach(btn => {
        if (!('Notification' in window)) {
            btn.classList.add('d-none');
            return;
        }

        const refresh = () => {
            const granted = Notification.permission === 'granted';
            btn.disabled = granted || Notification.permission === 'denied';
            btn.innerHTML = granted
                ? '<i class="bi bi-bell-fill me-1"></i> Notifications on'
                : Notification.permission === 'denied'
                    ? '<i class="bi bi-bell-slash me-1"></i> Notifications blocked'
                    : '<i class="bi bi-bell me-1"></i> Enable notifications';
        };

        refresh();
        btn.addEventListener('click', async () => {
            await Notification.requestPermission();
            refresh();
            if (Notification.permission === 'granted') HT.toast('Browser notifications enabled for reminders.', 'success');
        });
    });

    // ---------------------------------------------------------------------
    // Confetti (tiny canvas version, no library needed)
    // ---------------------------------------------------------------------

    HT.confetti = function () {
        if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

        const canvas = document.createElement('canvas');
        canvas.className = 'confetti-canvas';
        document.body.appendChild(canvas);

        const ctx = canvas.getContext('2d');
        const ratio = window.devicePixelRatio || 1;
        canvas.width = innerWidth * ratio;
        canvas.height = innerHeight * ratio;
        ctx.scale(ratio, ratio);

        const colors = ['#5b5bf0', '#22c55e', '#f97316', '#ec4899', '#facc15', '#06b6d4'];
        const pieces = Array.from({ length: 160 }, () => ({
            x: innerWidth / 2 + (Math.random() - .5) * 200,
            y: innerHeight * .35,
            vx: (Math.random() - .5) * 14,
            vy: Math.random() * -14 - 4,
            size: Math.random() * 7 + 4,
            rotation: Math.random() * Math.PI,
            spin: (Math.random() - .5) * .3,
            color: colors[Math.floor(Math.random() * colors.length)]
        }));

        const startedAt = performance.now();
        (function frame(now) {
            ctx.clearRect(0, 0, innerWidth, innerHeight);
            pieces.forEach(p => {
                p.vy += .35;
                p.vx *= .99;
                p.x += p.vx;
                p.y += p.vy;
                p.rotation += p.spin;
                ctx.save();
                ctx.translate(p.x, p.y);
                ctx.rotate(p.rotation);
                ctx.fillStyle = p.color;
                ctx.fillRect(-p.size / 2, -p.size / 4, p.size, p.size / 2);
                ctx.restore();
            });

            if (now - startedAt < 3200) {
                requestAnimationFrame(frame);
            } else {
                canvas.remove();
            }
        })(startedAt);
    };

    // ---------------------------------------------------------------------
    // Chart.js defaults that follow the theme
    // ---------------------------------------------------------------------

    HT.chartTheme = function () {
        return {
            text: HT.cssVar('--ht-muted'),
            grid: HT.cssVar('--ht-border'),
            primary: HT.cssVar('--ht-primary'),
            success: HT.cssVar('--ht-success'),
            surface: HT.cssVar('--ht-surface'),
            palette: ['#5b5bf0', '#16a34a', '#ea580c', '#db2777', '#0891b2', '#9333ea', '#ca8a04', '#475569']
        };
    };

    // Charts are rebuilt on theme change so axis and grid colors stay readable
    HT.chart = function (canvas, buildConfig) {
        if (!window.Chart || !canvas) return null;
        let instance = null;

        const render = () => {
            const theme = HT.chartTheme();
            Chart.defaults.font.family = HT.cssVar('--ht-font') || 'sans-serif';
            Chart.defaults.color = theme.text;
            if (instance) instance.destroy();
            instance = new Chart(canvas, buildConfig(theme));
        };

        render();
        HT.onThemeChange(render);
        return instance;
    };
})();
