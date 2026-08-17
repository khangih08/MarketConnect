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
        const storedIsMerchant = sessionStorage.getItem('is_merchant') === 'true' || localStorage.getItem('is_merchant') === 'true';

        if (isLoggedIn) {
            let merchantInitialHtml = storedIsMerchant ? `
                <div id="dynamic_merchant_dropdown_section">
                    <div class="px-3.5 py-1 text-[10px] font-extrabold text-amber-700 uppercase tracking-wider flex items-center gap-1">
                        <span class="material-symbols-outlined text-[14px] text-amber-600">storefront</span>
                        <span>KÊNH TIỂU THƯƠNG / GIAN HÀNG</span>
                    </div>
                    <a href="/CartAndOrders/MerchantRequests" class="flex items-center justify-between px-4 py-2 hover:bg-amber-50 hover:text-amber-900 font-bold text-amber-800 transition-colors">
                        <div class="flex items-center gap-2.5">
                            <span class="material-symbols-outlined text-[18px] text-amber-600">inbox</span> Đơn đặt mua gửi đến quầy
                        </div>
                    </a>
                    <a href="/Account/Profile" class="flex items-center gap-2.5 px-4 py-2 hover:bg-emerald-50 hover:text-emerald-800 transition-colors font-medium">
                        <span class="material-symbols-outlined text-[18px] text-emerald-800">store</span> Quản lý gian hàng của tôi
                    </a>
                    <a href="/Stores/Create" class="flex items-center gap-2.5 px-4 py-2 hover:bg-emerald-50 hover:text-emerald-800 transition-colors text-emerald-800">
                        <span class="material-symbols-outlined text-[18px]">add_business</span> Đăng ký thêm gian hàng
                    </a>
                </div>
            ` : `
                <div id="dynamic_merchant_dropdown_section">
                    <div class="px-3.5 py-1 text-[10px] font-extrabold text-emerald-700 uppercase tracking-wider flex items-center gap-1">
                        <span class="material-symbols-outlined text-[14px] text-emerald-600">storefront</span>
                        <span>MỞ GIAN HÀNG BÁN HÀNG</span>
                    </div>
                    <a href="/Stores/Create" class="flex items-center gap-2.5 px-4 py-2 hover:bg-emerald-50 text-emerald-800 font-bold transition-colors">
                        <span class="material-symbols-outlined text-[18px] text-emerald-700">storefront</span> Đăng ký trở thành tiểu thương
                    </a>
                </div>
            `;

            authZone.innerHTML = `
      <div class="relative group py-2">
        <a class="flex items-center gap-1.5 font-medium hover:text-orange-400 cursor-pointer text-xs text-white">
            <span class="material-symbols-outlined text-[18px]">account_circle</span>
            <span>${displayName || 'Tài khoản'}</span>
            <span class="text-[9px] transition-transform duration-200 group-hover:rotate-180">▼</span>
        </a>
        
        <div id="authDropdown" class="absolute right-0 top-full pt-2.5 w-60 hidden group-hover:block z-50">
            <div class="bg-white text-gray-800 rounded-2xl shadow-2xl py-2 border border-gray-100 text-xs">
                
                <!-- PHẦN 1: CÀI ĐẶT TÀI KHOẢN CÁ NHÂN (Ở TRÊN) -->
                <div class="px-3.5 py-1 text-[10px] font-extrabold text-gray-400 uppercase tracking-wider flex items-center gap-1">
                    <span class="material-symbols-outlined text-[14px] text-gray-400">manage_accounts</span>
                    <span>TÀI KHOẢN CÁ NHÂN</span>
                </div>
                <a href="/Account/Profile" class="flex items-center gap-2.5 px-4 py-2 hover:bg-emerald-50 hover:text-emerald-800 font-semibold transition-colors">
                    <span class="material-symbols-outlined text-[18px] text-emerald-700">account_box</span> Quản lý hồ sơ
                </a>
                <a href="/Account/ChangePassword" class="flex items-center gap-2.5 px-4 py-2 hover:bg-emerald-50 hover:text-emerald-800 transition-colors">
                    <span class="material-symbols-outlined text-[18px] text-emerald-700">lock</span> Cài đặt & Đổi mật khẩu
                </a>
                <a href="/CartAndOrders/BuyerRequests" class="flex items-center gap-2.5 px-4 py-2 hover:bg-emerald-50 hover:text-emerald-800 transition-colors">
                    <span class="material-symbols-outlined text-[18px] text-emerald-700">shopping_bag</span> Lịch sử mua hàng của tôi
                </a>
                <a href="/Help" class="flex items-center gap-2.5 px-4 py-2 hover:bg-emerald-50 hover:text-emerald-800 transition-colors">
                    <span class="material-symbols-outlined text-[18px] text-blue-600">help_center</span> Trợ giúp
                </a>
                <a href="/Feedback" class="flex items-center gap-2.5 px-4 py-2 hover:bg-emerald-50 hover:text-emerald-800 transition-colors">
                    <span class="material-symbols-outlined text-[18px] text-amber-600">rate_review</span> Đóng góp ý kiến
                </a>

                <div class="border-t border-gray-100 my-1.5"></div>

                <!-- PHẦN 2: TRẠNG THÁI GIAN HÀNG & KÊNH TIỂU THƯƠNG -->
                ${merchantInitialHtml}

                <div class="border-t border-gray-100 my-1.5"></div>
                <a href="/Account/Logout" id="logoutLink" class="flex items-center gap-2.5 px-4 py-2 text-red-600 hover:bg-red-50 font-semibold transition-colors cursor-pointer">
                    <span class="material-symbols-outlined text-[18px]">logout</span> Đăng xuất
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
                    localStorage.removeItem('is_merchant');
                    window.location.href = '/Account/Logout';
                });
            }

            // Gọi API kiểm tra xem user có phải tiểu thương hay không để đồng bộ phiên làm việc
            fetch('/Account/GetProfileData')
                .then(res => res.ok ? res.json() : null)
                .then(data => {
                    if (!data) return;
                    const merchantSection = document.getElementById('dynamic_merchant_dropdown_section');

                    if (data.isMerchant || data.role === 'Merchant' || (data.stores && data.stores.length > 0)) {
                        sessionStorage.setItem('is_merchant', 'true');
                        localStorage.setItem('is_merchant', 'true');

                        if (merchantSection) {
                            merchantSection.innerHTML = `
                                <div class="px-3.5 py-1 text-[10px] font-extrabold text-amber-700 uppercase tracking-wider flex items-center gap-1">
                                    <span class="material-symbols-outlined text-[14px] text-amber-600">storefront</span>
                                    <span>KÊNH TIỂU THƯƠNG / GIAN HÀNG</span>
                                </div>
                                <a href="/CartAndOrders/MerchantRequests" class="flex items-center justify-between px-4 py-2 hover:bg-amber-50 hover:text-amber-900 font-bold text-amber-800 transition-colors">
                                    <div class="flex items-center gap-2.5">
                                        <span class="material-symbols-outlined text-[18px] text-amber-600">inbox</span> Đơn đặt mua gửi đến quầy
                                    </div>
                                </a>
                                <a href="/Account/Profile" class="flex items-center gap-2.5 px-4 py-2 hover:bg-emerald-50 hover:text-emerald-800 transition-colors font-medium">
                                    <span class="material-symbols-outlined text-[18px] text-emerald-800">store</span> Quản lý gian hàng của tôi
                                </a>
                                <a href="/Stores/Create" class="flex items-center gap-2.5 px-4 py-2 hover:bg-emerald-50 hover:text-emerald-800 transition-colors text-emerald-800">
                                    <span class="material-symbols-outlined text-[18px]">add_business</span> Đăng ký thêm gian hàng
                                </a>
                            `;
                        }
                    }
                })
                .catch(err => console.error(err));
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