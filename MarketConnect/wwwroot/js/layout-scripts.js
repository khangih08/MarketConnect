function checkAuthStatus() {
    try {
        const token = sessionStorage.getItem('token') || localStorage.getItem('token');
        let userEmail = sessionStorage.getItem('user_email') || localStorage.getItem('user_email') || '';
        let rawName = sessionStorage.getItem('user_name') || localStorage.getItem('user_name') || '';

        let displayName = '';
        if (rawName && rawName !== 'Khôi Nguyễn' && rawName !== 'Tài khoản') {
            displayName = rawName;
        } else if (userEmail && userEmail.includes('@')) {
            displayName = userEmail.split('@')[0];
        } else if (userEmail) {
            displayName = userEmail;
        } else if (token) {
            displayName = 'Người dùng';
        }

        const homeUserDisplay = document.getElementById('home_user_name_display');
        if (homeUserDisplay && displayName) {
            homeUserDisplay.innerText = displayName;
        }

        const layoutUserDisplay = document.getElementById('layout_user_name_display');
        if (layoutUserDisplay && displayName) {
            layoutUserDisplay.innerText = displayName;
        }

        const authZone = document.getElementById('topbar_auth_zone');
        if (!authZone) return;

        const isLoggedIn = (token && token.trim() !== '') || (userEmail && userEmail.includes('@')) || (displayName !== '');

        if (isLoggedIn) {
            authZone.innerHTML = `
      <div class="relative group py-2">
        <a class="flex items-center gap-1.5 font-medium hover:text-orange-400 cursor-pointer text-xs text-white">
            <span class="material-symbols-outlined text-[18px]">account_circle</span>
            <span>${displayName || 'Tài khoản'}</span>
            <span class="text-[9px] transition-transform duration-200 group-hover:rotate-180">▼</span>
        </a>
        
        <div id="authDropdown" class="absolute right-0 top-full pt-2.5 w-52 hidden group-hover:block z-50">
            <div class="bg-white text-gray-800 rounded-2xl shadow-2xl py-1.5 border border-gray-100">
                <a href="/Account/Profile" class="flex items-center gap-2 px-4 py-2.5 text-xs hover:bg-gray-100 hover:text-orange-500 font-semibold transition-colors">
                    <span class="material-symbols-outlined text-[16px] text-emerald-700">account_box</span> Quản lý hồ sơ
                </a>
                <a href="/Stores/Create" class="flex items-center gap-2 px-4 py-2.5 text-xs hover:bg-gray-100 hover:text-orange-500 font-bold text-emerald-800 transition-colors">
                    <span class="material-symbols-outlined text-[16px]">storefront</span> Đăng ký bán hàng
                </a>
                <a href="/Account/Profile" class="flex items-center gap-2 px-4 py-2.5 text-xs hover:bg-gray-100 hover:text-orange-500 transition-colors">
                    <span class="material-symbols-outlined text-[16px]">settings</span> Cài đặt tài khoản
                </a>
                <a href="/Help" class="flex items-center gap-2 px-4 py-2.5 text-xs hover:bg-gray-100 hover:text-orange-500 transition-colors">
                    <span class="material-symbols-outlined text-[16px]">help_center</span> Trợ giúp
                </a>
                <a href="/Feedback" class="flex items-center gap-2 px-4 py-2.5 text-xs hover:bg-gray-100 hover:text-orange-500 transition-colors">
                    <span class="material-symbols-outlined text-[16px]">rate_review</span> Đóng góp ý kiến
                </a>
                <div class="border-t border-gray-100 my-1"></div>
                <a href="/Account/Logout" id="logoutLink" class="flex items-center gap-2 px-4 py-2.5 text-xs text-red-600 hover:bg-red-50 transition-colors cursor-pointer">
                    <span class="material-symbols-outlined text-[16px]">logout</span> Đăng xuất
                </a>
            </div>
        </div>
    </div>`;                  

            const logoutBtn = document.getElementById('logoutLink');
            if (logoutBtn) {
                logoutBtn.addEventListener('click', function (e) {
                    e.preventDefault();
                    sessionStorage.clear();
                    localStorage.removeItem('token');
                    localStorage.removeItem('user_email');
                    localStorage.removeItem('user_name');
                    window.location.href = '/Account/Logout';
                });
            }
        } else {
            authZone.innerHTML = `
                <div class="flex items-center gap-3 text-xs font-semibold text-white">
                    <a class="hover:text-orange-300 transition-colors font-bold" href="/Account/Register">Đăng ký</a>
                    <div class="w-[1px] h-3.5 bg-white/40"></div>
                    <a class="hover:text-orange-300 transition-colors font-bold" href="/Account/Login">Đăng nhập</a>
                </div>
            `;
        }
    } catch (err) {
        console.error('checkAuthStatus error', err);
    }
}

function syncMarketNavigationLinks() {
    try {
        const mId = localStorage.getItem('selected_market_id') || sessionStorage.getItem('selected_market_id');
        if (!mId) return;

        const storeLinks = document.querySelectorAll('a[href="/Stores"], a[href="/Home/Index"], a[href="/"]');
        storeLinks.forEach(link => {
            const href = link.getAttribute('href');
            if (href && !href.includes('marketId=')) {
                const separator = href.includes('?') ? '&' : '?';
                link.setAttribute('href', `${href}${separator}marketId=${mId}`);
            }
        });
    } catch (e) { }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        checkAuthStatus();
        syncMarketNavigationLinks();
    });
} else {
    checkAuthStatus();
    syncMarketNavigationLinks();
}