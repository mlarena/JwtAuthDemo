document.addEventListener('DOMContentLoaded', () => {
    const savedTheme = localStorage.getItem('theme') || 'light';
    document.documentElement.setAttribute('data-theme', savedTheme);

    const themeToggle = document.getElementById('themeToggle');
    if (themeToggle) {
        themeToggle.addEventListener('click', () => {
            const current = document.documentElement.getAttribute('data-theme');
            const next = current === 'light' ? 'dark' : 'light';
            document.documentElement.setAttribute('data-theme', next);
            localStorage.setItem('theme', next);
        });
    }

    const burgerBtn = document.getElementById('burgerBtn');
    const layout = document.querySelector('.admin-layout');
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebarOverlay');

    function isMobile() {
        return window.innerWidth <= 768;
    }

    function closeMobileMenu() {
        sidebar.classList.remove('show');
        overlay.classList.remove('show');
    }

    if (burgerBtn) {
        burgerBtn.addEventListener('click', () => {
            if (isMobile()) {
                sidebar.classList.toggle('show');
                overlay.classList.toggle('show');
            } else {
                layout.classList.toggle('collapsed');
                localStorage.setItem('sidebarCollapsed', layout.classList.contains('collapsed'));
            }
        });
    }

    if (overlay) {
        overlay.addEventListener('click', closeMobileMenu);
    }

    if (!isMobile()) {
        const sidebarState = localStorage.getItem('sidebarCollapsed');
        if (sidebarState === 'true' && layout) {
            layout.classList.add('collapsed');
        }
    }

    const currentPage = window.location.pathname.toLowerCase();
    document.querySelectorAll('.sidebar-link').forEach(link => {
        const href = link.getAttribute('href') || '';
        const aspHref = link.getAttribute('asp-controller');
        link.classList.remove('active');
        if (currentPage === '/' && href === '/') {
            link.classList.add('active');
        } else if (href !== '/' && currentPage.startsWith(href.toLowerCase())) {
            link.classList.add('active');
        }
    });

    window.addEventListener('resize', () => {
        if (!isMobile()) {
            closeMobileMenu();
        }
    });

    // Collapsible sidebar groups
    const collapsedGroups = JSON.parse(localStorage.getItem('collapsedGroups') || '{}');

    document.querySelectorAll('.sidebar-group-title').forEach(title => {
        const groupId = title.getAttribute('data-group');
        const group = title.closest('.sidebar-group');

        if (collapsedGroups[groupId]) {
            group.classList.add('collapsed');
        }

        title.addEventListener('click', () => {
            group.classList.toggle('collapsed');
            collapsedGroups[groupId] = group.classList.contains('collapsed');
            localStorage.setItem('collapsedGroups', JSON.stringify(collapsedGroups));
        });
    });
});
