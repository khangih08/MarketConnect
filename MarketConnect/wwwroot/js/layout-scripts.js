function checkAuthStatus() {
    try {
        const token = sessionStorage.getItem('token');
        const authZone = document.getElementById('topbar_auth_zone');

        if (!authZone) return;

        if (token) {
            // ĐÃ SỬA: Lấy trực tiếp họ tên thật từ sessionStorage do Backend vừa trả về
            const displayName = sessionStorage.getItem('user_name') || 'Tài khoản';

            authZone.innerHTML = `
      <div class="relative group py-2"> <!-- Tăng py-1 thành py-2 để mở rộng vùng hover của thẻ cha -->
        <a class="flex items-center gap-1.5 font-medium hover:text-orange-400 cursor-pointer text-xs text-white">
            <span class="material-symbols-outlined text-[18px]">account_circle</span>
            <span>${displayName}</span>
            <span class="text-[9px] transition-transform duration-200 group-hover:rotate-180">▼</span>
        </a>
        
        <div id="authDropdown" class="absolute right-0 top-full pt-2.5 w-48 hidden group-hover:block z-50">
            <!-- Hộp màu trắng chứa content thực tế nằm bên trong vùng đệm -->
            <div class="bg-white text-gray-800 rounded shadow-xl py-1 border border-gray-100">
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
                <a id="logoutLink" class="flex items-center gap-2 px-4 py-2.5 text-xs text-red-600 hover:bg-red-50 transition-colors cursor-pointer">
                    <span class="material-symbols-outlined text-[16px]">logout</span> Đăng xuất
                </a>
            </div>
        </div>
    </div>`;                  

            document.getElementById('logoutLink').addEventListener('click', function (e) {
                e.preventDefault();
                sessionStorage.removeItem('token');
                sessionStorage.removeItem('user_name');
                sessionStorage.removeItem('user_email');
                sessionStorage.removeItem('user_phone');
                sessionStorage.removeItem('user_address');
                sessionStorage.removeItem('user_dob');
                window.location.href = '/';
            });
        } else {
            authZone.innerHTML = `
                <a class="font-medium hover:text-orange-400" href="/Account/Register">Đăng ký</a>
                <div class="w-[1px] h-3 bg-white/30"></div>
                <a class="font-medium hover:text-orange-400" href="/Account/Login">Đăng nhập</a>
            `;
        }
    } catch (err) {
        console.error('checkAuthStatus error', err);
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', checkAuthStatus);
} else {
    checkAuthStatus();
}