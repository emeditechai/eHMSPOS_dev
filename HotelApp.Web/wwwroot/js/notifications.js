(function () {
    const POLL_MS = 60 * 1000; // refresh list every minute
    const SNOOZE_MS = 10 * 60 * 1000; // clear/snooze for 10 minutes
    const STORAGE_KEY = 'luxstay.notifications.snoozedUntil.v1';

    function loadSnoozedUntil() {
        try {
            const raw = localStorage.getItem(STORAGE_KEY);
            return raw ? JSON.parse(raw) : {};
        } catch {
            return {};
        }
    }

    function saveSnoozedUntil(map) {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(map));
        } catch {
            // ignore
        }
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function setBadge(count, badgeEl) {
        if (count > 0) {
            badgeEl.textContent = String(count);
            badgeEl.style.display = '';
        } else {
            badgeEl.textContent = '0';
            badgeEl.style.display = 'none';
        }
    }

    function render(items, listEl) {
        if (!items || items.length === 0) {
            listEl.innerHTML = '<div class="notifications-empty">No notifications</div>';
            return;
        }

        const html = items.map(function (item) {
            const title = escapeHtml(item.title || 'Notification');
            const message = escapeHtml(item.message || '');
            const url = escapeHtml(item.url || '#');
            return (
                '<a class="notifications-item" href="' + url + '">' +
                '<div class="notifications-item-title">' + title + '</div>' +
                '<div class="notifications-item-message">' + message + '</div>' +
                '</a>'
            );
        }).join('');

        listEl.innerHTML = html;
    }

    function applySnooze(items) {
        const snoozedUntil = loadSnoozedUntil();
        const now = Date.now();

        // Cleanup expired snoozes
        for (const k of Object.keys(snoozedUntil)) {
            if (!snoozedUntil[k] || snoozedUntil[k] <= now) delete snoozedUntil[k];
        }
        saveSnoozedUntil(snoozedUntil);

        return (items || []).filter(function (item) {
            const key = item && item.key ? String(item.key) : '';
            if (!key) return false;
            const until = snoozedUntil[key];
            return !until || until <= now;
        });
    }

    async function pollOnce(listEl, badgeEl) {
        const res = await fetch('/Notifications/Poll', { credentials: 'include' });
        if (!res.ok) {
            return;
        }

        let payload;
        try {
            payload = await res.json();
        } catch {
            return;
        }

        const items = Array.isArray(payload.items) ? payload.items : [];
        const visibleItems = applySnooze(items);
        setBadge(visibleItems.length, badgeEl);
        render(visibleItems, listEl);
        return items;
    }

    document.addEventListener('DOMContentLoaded', function () {
        const bell = document.getElementById('notificationsBell');
        const badge = document.getElementById('notificationsBadge');
        const dropdown = document.getElementById('notificationsDropdown');
        const list = document.getElementById('notificationsList');
        const clearBtn = document.getElementById('notificationsClear');
        if (!bell || !badge || !dropdown || !list || !clearBtn) return;

        let lastItems = [];

        function openDropdown() {
            dropdown.style.display = '';
        }

        function closeDropdown() {
            dropdown.style.display = 'none';
        }

        function toggleDropdown() {
            if (dropdown.style.display === 'none') {
                openDropdown();
            } else {
                closeDropdown();
            }
        }

        bell.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            toggleDropdown();
        });

        clearBtn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            const now = Date.now();
            const snoozedUntil = loadSnoozedUntil();
            for (const item of lastItems) {
                if (item && item.key) {
                    snoozedUntil[String(item.key)] = now + SNOOZE_MS;
                }
            }
            saveSnoozedUntil(snoozedUntil);

            // Immediately update UI
            setBadge(0, badge);
            render([], list);
        });

        dropdown.addEventListener('click', function (e) {
            e.stopPropagation();
        });

        document.addEventListener('click', function () {
            closeDropdown();
        });

        closeDropdown();
        pollOnce(list, badge).then(function (items) {
            lastItems = Array.isArray(items) ? items : [];
        }).catch(() => { });

        setInterval(() => {
            pollOnce(list, badge)
                .then(function (items) {
                    lastItems = Array.isArray(items) ? items : [];
                })
                .catch(() => { });
        }, POLL_MS);
    });
})();
