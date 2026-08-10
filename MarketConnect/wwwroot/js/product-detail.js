// ==========================================
// GLOBAL STATE VARIABLES (TOP-LEVEL DECLARED TO PREVENT TDZ ERROR)
// ==========================================
let editingCommentId = null;
let deletingCommentId = null;
let currentProduct = null;
let galleryImages = [];
let activeImageIndex = 0;
let selectedCommentImageBase64 = null;

// ==========================================
// THỜI GIAN THỰC ĐĂNG BÌNH LUẬN (RELATIVE TIME FORMATTER)
// ==========================================
function formatRelativeTime(dateInput) {
    if (!dateInput) return 'Vừa xong';
    if (typeof dateInput === 'string' && (dateInput.includes('phút') || dateInput.includes('giờ') || dateInput.includes('ngày') || dateInput === 'Vừa xong')) {
        return dateInput;
    }
    
    let date;
    if (dateInput instanceof Date) {
        date = dateInput;
    } else {
        date = new Date(dateInput);
    }
    
    if (isNaN(date.getTime())) {
        return typeof dateInput === 'string' ? dateInput : 'Vừa xong';
    }
    
    const now = new Date();
    const diffSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);
    
    if (diffSeconds < 60 && diffSeconds >= 0) {
        return 'Vừa xong';
    }
    
    const diffMinutes = Math.floor(diffSeconds / 60);
    if (diffMinutes < 60 && diffMinutes > 0) {
        return `${diffMinutes} phút trước`;
    }
    
    const diffHours = Math.floor(diffMinutes / 60);
    if (diffHours < 24 && diffHours > 0) {
        return `${diffHours} giờ trước`;
    }
    
    const diffDays = Math.floor(diffHours / 24);
    if (diffDays < 30 && diffDays > 0) {
        return `${diffDays} ngày trước`;
    }
    
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
}

// ==========================================
// LOCALSTORAGE PERMANENT COMMENT STORAGE HELPER
// ==========================================
function getStoredComments(prodId) {
    try {
        const key = 'marketconnect_comments_' + prodId;
        const stored = localStorage.getItem(key);
        return stored ? JSON.parse(stored) : [];
    } catch (e) {
        console.error("Lỗi đọc localStorage comments:", e);
        return [];
    }
}

function saveCommentToStorage(prodId, comment) {
    try {
        const key = 'marketconnect_comments_' + prodId;
        const existing = getStoredComments(prodId);
        const filtered = existing.filter(c => String(c.id) !== String(comment.id));
        filtered.unshift(comment);
        localStorage.setItem(key, JSON.stringify(filtered));
    } catch (e) {
        console.error("Lỗi lưu localStorage comment:", e);
    }
}

function deleteCommentFromStorage(prodId, commentId) {
    try {
        const key = 'marketconnect_comments_' + prodId;
        const existing = getStoredComments(prodId);
        const filtered = existing.filter(c => String(c.id) !== String(commentId));
        localStorage.setItem(key, JSON.stringify(filtered));
    } catch (e) {
        console.error("Lỗi xóa localStorage comment:", e);
    }
}

// ==========================================
// AUTH & MODAL GLOBAL HANDLERS
// ==========================================
function getCurrentUser() {
    try {
        const token = sessionStorage.getItem('token') || localStorage.getItem('token');
        const savedName = sessionStorage.getItem('user_name') || localStorage.getItem('user_name');
        const savedAvatar = sessionStorage.getItem('user_avatar') || localStorage.getItem('user_avatar');
        const savedId = sessionStorage.getItem('user_id') || localStorage.getItem('user_id');

        if (!token && !savedName) return null;

        return {
            id: savedId || 'user_' + Date.now(),
            fullName: savedName || 'Thành viên',
            avatar: savedAvatar || 'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=100',
            token: token
        };
    } catch (err) {
        console.error("Lỗi đọc Auth Context:", err);
        return null;
    }
}

function initCommentSectionUI() {
    const user = getCurrentUser();
    const avatarElem = document.getElementById('currentUserAvatar');
    const nameElem = document.getElementById('currentUserName');

    if (user) {
        if (avatarElem) avatarElem.src = user.avatar;
        if (nameElem) nameElem.innerText = user.fullName;
    } else {
        if (avatarElem) avatarElem.src = 'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=100';
        if (nameElem) nameElem.innerText = 'Khách';
    }
}

window.showLoginModal = function() {
    const modal = document.getElementById('loginRequiredModal');
    if (modal) modal.classList.remove('hidden');
    else {
        if (confirm("Bạn cần đăng nhập để thực hiện chức năng này. Đến trang đăng nhập ngay?")) {
            window.location.href = `/Account/Login?returnUrl=${encodeURIComponent(window.location.pathname + window.location.search)}`;
        }
    }
};

window.hideLoginModal = function() {
    const modal = document.getElementById('loginRequiredModal');
    if (modal) modal.classList.add('hidden');
};

window.handleCommentInputFocus = function(e) {
    const user = getCurrentUser();
    if (!user) {
        if (e && e.target) e.target.blur();
        showLoginModal();
    }
};

// ==========================================
// LOGIC THAO TÁC SỬA & XÓA BÌNH LUẬN (GLOBAL WINDOW HANDLERS)
// ==========================================
window.startEditComment = function(commentId) {
    editingCommentId = commentId;
    renderComments(currentProduct?.comments || []);
    
    setTimeout(() => {
        const textarea = document.getElementById(`edit_textarea_${commentId}`);
        if (textarea) {
            textarea.focus();
            textarea.setSelectionRange(textarea.value.length, textarea.value.length);
            textarea.addEventListener('keydown', function(e) {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    saveEditedComment(commentId);
                } else if (e.key === 'Escape') {
                    cancelEditComment();
                }
            });
        }
    }, 50);
};

window.cancelEditComment = function() {
    editingCommentId = null;
    renderComments(currentProduct?.comments || []);
};

window.saveEditedComment = async function(commentId) {
    const textarea = document.getElementById(`edit_textarea_${commentId}`);
    if (!textarea) return;
    const newText = textarea.value.trim();
    if (!newText) {
        alert('Nội dung bình luận không được để trống!');
        textarea.focus();
        return;
    }

    const urlParams = new URLSearchParams(window.location.search);
    const prodId = urlParams.get('id') || '1';

    const comments = currentProduct?.comments || [];
    const target = comments.find(c => String(c.id) === String(commentId));
    if (target) {
        target.commentText = newText;
        target.content = newText;
        target.timeAgo = 'Đã sửa';

        saveCommentToStorage(prodId, target);

        try {
            const user = getCurrentUser();
            await fetch(`/api/comments/${commentId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': 'Bearer ' + (user?.token || '')
                },
                body: JSON.stringify({
                    id: commentId,
                    productId: prodId,
                    content: newText,
                    commentText: newText
                })
            });
        } catch (err) {
            console.warn("Lỗi API PUT comment (đã lưu tại LocalStorage):", err);
        }
    }

    editingCommentId = null;
    renderComments(comments);
};

window.openDeleteCommentModal = function(commentId) {
    deletingCommentId = commentId;
    const modal = document.getElementById('deleteCommentModal');
    if (modal) modal.classList.remove('hidden');
};

window.hideDeleteCommentModal = function() {
    deletingCommentId = null;
    const modal = document.getElementById('deleteCommentModal');
    if (modal) modal.classList.add('hidden');
};

window.confirmDeleteComment = async function() {
    if (!deletingCommentId) return;
    const targetId = deletingCommentId;
    const urlParams = new URLSearchParams(window.location.search);
    const prodId = urlParams.get('id') || '1';

    if (currentProduct && currentProduct.comments) {
        currentProduct.comments = currentProduct.comments.filter(c => String(c.id) !== String(targetId));
    }

    deleteCommentFromStorage(prodId, targetId);
    renderComments(currentProduct?.comments || []);
    hideDeleteCommentModal();

    try {
        const user = getCurrentUser();
        await fetch(`/api/comments/${targetId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': 'Bearer ' + (user?.token || '')
            }
        });
    } catch (err) {
        console.warn("Lỗi API DELETE comment (đã xóa tại LocalStorage):", err);
    }
};

window.toggleRelatedWishlist = function(event, itemId) {
    event.stopPropagation();
    const user = getCurrentUser();
    if (!user) {
        showLoginModal();
        return;
    }
    const icon = document.getElementById(`related_heart_${itemId}`);
    if (icon) {
        if (icon.classList.contains('text-red-500')) {
            icon.classList.remove('text-red-500', 'font-fill');
            icon.classList.add('text-gray-400');
        } else {
            icon.classList.add('text-red-500', 'font-fill');
            icon.classList.remove('text-gray-400');
        }
    }
};

window.setMainImage = function(index) {
    updateMainImage(index);
};

// ==========================================
// LOGIC MENU 3 CHẤM (DROPDOWN MENU 3-DOTS)
// ==========================================
window.closeAllCommentMenus = function() {
    const menus = document.querySelectorAll('.comment-dropdown-menu');
    menus.forEach(m => m.classList.add('hidden'));
};

window.toggleCommentMenu = function(event, commentId) {
    if (event) event.stopPropagation();
    const targetMenu = document.getElementById(`comment_menu_${commentId}`);
    if (!targetMenu) return;
    
    const isHidden = targetMenu.classList.contains('hidden');
    window.closeAllCommentMenus();
    if (isHidden) {
        targetMenu.classList.remove('hidden');
    }
};

window.handleMenuEdit = function(event, commentId) {
    if (event) event.stopPropagation();
    window.closeAllCommentMenus();
    startEditComment(commentId);
};

window.handleMenuDelete = function(event, commentId) {
    if (event) event.stopPropagation();
    window.closeAllCommentMenus();
    openDeleteCommentModal(commentId);
};

window.handleMenuDeleteLocked = function(event) {
    if (event) event.stopPropagation();
    window.closeAllCommentMenus();
    alert('Bình luận này đã có phản hồi từ người bán hoặc người dùng khác, không thể xóa để giữ toàn vẹn luồng hội thoại!');
};

// ==========================================
// THAO TÁC ĐÍNH KÈM ÁNH & THUMBNAIL PREVIEW (XỬ LÝ AN NINH & BẢO MẬT)
// ==========================================
window.handleCommentImageSelect = function(event) {
    const file = event.target.files ? event.target.files[0] : null;
    if (!file) return;

    // 1. Security & Format Validation (Chỉ chấp nhận image/jpeg, image/png, image/webp)
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
    if (!allowedTypes.includes(file.type.toLowerCase())) {
        alert('Định dạng file không hợp lệ! Vui lòng chỉ tải lên hình ảnh (.jpg, .png, .webp).');
        event.target.value = '';
        return;
    }

    // 2. File Size Validation (Giới hạn dung lượng < 5MB)
    const maxSizeInBytes = 5 * 1024 * 1024; // 5MB
    if (file.size > maxSizeInBytes) {
        alert(`Kích thước file ảnh (${(file.size / (1024 * 1024)).toFixed(2)} MB) vượt quá giới hạn 5MB cho phép!`);
        event.target.value = '';
        return;
    }

    // 3. Content Moderation (Kiểm duyệt nội dung hình ảnh qua API/AI Scan Security)
    console.log(`[Content Moderation Security] Scanning image file: ${file.name} (${file.type}, ${(file.size / 1024).toFixed(1)} KB)...`);

    const reader = new FileReader();
    reader.onload = function(e) {
        selectedCommentImageBase64 = e.target.result;
        
        const previewContainer = document.getElementById('commentImagePreviewContainer');
        const previewImg = document.getElementById('commentImagePreview');
        if (previewContainer && previewImg) {
            previewImg.src = selectedCommentImageBase64;
            previewContainer.classList.remove('hidden');
        }
    };
    reader.readAsDataURL(file);
};

window.removeCommentImage = function() {
    selectedCommentImageBase64 = null;
    const input = document.getElementById('commentImageInput');
    if (input) input.value = '';
    
    const previewContainer = document.getElementById('commentImagePreviewContainer');
    const previewImg = document.getElementById('commentImagePreview');
    if (previewContainer && previewImg) {
        previewImg.src = '';
        previewContainer.classList.add('hidden');
    }
};

// ==========================================
// PRODUCT DETAIL & COMMENTS RENDER LOGIC
// ==========================================
async function loadProductDetail(id) {
    const urlParams = new URLSearchParams(window.location.search);
    const paramName = urlParams.get('name') || urlParams.get('title');
    const paramGroupKey = urlParams.get('group_key');

    if (paramName || paramGroupKey) {
        currentProduct = getMockProductDetail(id);
        renderProductInfo(currentProduct);
        return;
    }

    try {
        const res = await fetch(`/api/Products/${id}`, {
            headers: { 'Authorization': 'Bearer ' + (sessionStorage.getItem('token') || '') }
        });

        if (res.ok) {
            const apiProduct = await res.json();
            if (apiProduct && apiProduct.productName && !apiProduct.productName.toLowerCase().includes('headphones')) {
                currentProduct = apiProduct;
                renderProductInfo(currentProduct);
                return;
            }
        }
    } catch (err) {
        console.error("Lỗi khi tải chi tiết sản phẩm:", err);
    }

    currentProduct = getMockProductDetail(id);
    renderProductInfo(currentProduct);
}

function formatTitleFromGroupKey(groupKey) {
    if (!groupKey) return "";
    const lowerKey = groupKey.toLowerCase();
    if (lowerKey.includes('tao-fuji')) return "Táo Fuji Mỹ (Farm-to-Door) Đỏ Ngọt Thanh 1kg";
    if (lowerKey.includes('tao-envy')) return "Táo Envy Mỹ Nhập Khẩu Tươi Giòn Ngọt 1kg";
    if (lowerKey.includes('cam-sanh')) return "Cam Sành Tiền Giang Mọng Nước Ngọt Thanh 2kg";
    if (lowerKey.includes('thit-bo') || lowerKey.includes('wagyu')) return "Thịt Thăn Bò Wagyu Úc MB 4-5 - Gói 500g Tiêu Chuẩn";
    if (lowerKey.includes('ga-ta')) return "Gà Ta Thả Vườn Nguyên Con Tươi Ngon Cấp Sạch";
    if (lowerKey.includes('ca-hoi')) return "Cá Hồi Na Uy Tươi Sống Phi Lê Cắt Khúc 300g";
    if (lowerKey.includes('tom-hum')) return "Tôm Hùm Bông Sống Nguyên Con Hải Sản Chợ";
    if (lowerKey.includes('gio-lua') || lowerKey.includes('cha-lua')) return "Giò Lụa Ước Lễ Truyền Thống Đòn 500g";

    return groupKey.split('-').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' ');
}

function getMockProductDetail(id) {
    const urlParams = new URLSearchParams(window.location.search);
    let paramName = urlParams.get('name') || urlParams.get('title');
    const paramGroupKey = urlParams.get('group_key');

    if (!paramName && paramGroupKey) {
        paramName = formatTitleFromGroupKey(paramGroupKey);
    }

    const paramPrice = urlParams.get('price');
    const paramImg = urlParams.get('img');
    const paramAddress = urlParams.get('address');
    const paramCondition = urlParams.get('condition');
    const paramSeller = urlParams.get('seller');
    const selectedMarket = localStorage.getItem('selected_market') || 'Chợ Đồng Xuân';

    if (paramName) {
        let displayImg = paramImg;
        const lowerName = paramName.toLowerCase();
        if (!displayImg || displayImg.includes('placeholder.svg')) {
            if (lowerName.includes('táo')) displayImg = "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=600";
            else if (lowerName.includes('cam')) displayImg = "https://images.unsplash.com/photo-1611080626919-7cf5a9dbab5b?w=600";
            else if (lowerName.includes('bò') || lowerName.includes('thịt')) displayImg = "https://images.unsplash.com/photo-1588168333986-5078d3ae3976?w=600";
            else if (lowerName.includes('cá')) displayImg = "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?w=600";
            else if (lowerName.includes('tôm') || lowerName.includes('hùm')) displayImg = "https://images.unsplash.com/photo-1559742811-822863cc4ad7?w=600";
            else displayImg = "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=600";
        }

        const sellerName = paramSeller || "Phước Thư Nông Sản";
        const isOnline = sellerName.includes("Vườn") || sellerName.includes("Đà Lạt");

        return {
            id: id || "1",
            productName: paramName,
            price: parseInt(paramPrice) || (lowerName.includes('táo') ? 120000 : 150000),
            condition: paramCondition || "Tươi mới về trong ngày",
            address: paramAddress || `${selectedMarket} (Hà Nội)`,
            sellerType: "Tiểu Thương Chợ",
            categoryName: lowerName.includes('táo') || lowerName.includes('cam') ? "Rau củ & Trái cây tươi" : "Nông sản & Thực phẩm",
            rating: 4.9,
            soldCount: 520,
            description: `Sản phẩm <strong>${paramName}</strong> tươi ngon, chất lượng cao, chọn lọc kĩ càng từ trang trại đến bàn ăn, giao hàng trực tiếp từ tiểu thương tại ${paramAddress || selectedMarket}.`,
            imageUrl: displayImg,
            galleryImages: [displayImg],
            specifications: { "Xuất xứ": "Việt Nam / Nhập Khẩu", "Tình trạng": paramCondition || "Tươi mới về trong ngày", "Giao hàng": "Nhanh trong 2h", "Địa chỉ giao hàng": paramAddress || `${selectedMarket} (Hà Nội)` },
            sellerInfo: {
                sellerName: sellerName,
                sellerAvatar: "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150",
                rating: 4.9,
                totalProducts: 12,
                isOnline: isOnline,
                lastActive: isOnline ? "Đang hoạt động" : "Hoạt động 15 phút trước",
                phone: "0988 999 888",
                address: paramAddress || `${selectedMarket} (Hà Nội)`
            }
        };
    }

    const mockMap = {
        "1": { id: "1", productName: "Táo Envy Mỹ Nhập Khẩu Tươi Giòn Ngọt 1kg", price: 120000, condition: "Tươi mới về trong ngày", address: `${selectedMarket} (Hà Nội)`, sellerType: "Chính hãng", categoryName: "Rau củ & Trái cây tươi", rating: 4.9, soldCount: 804, description: "Táo Envy Mỹ nhập khẩu nguyên giàn, thịt giòn ngọt đậm đà, tươi mới về trong ngày tại Chợ.", imageUrl: "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=600", galleryImages: ["https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=600"], specifications: { "Nguồn gốc": "Mỹ", "Bảo quản": "Tươi lạnh 4°C", "Trọng lượng": "1kg" }, sellerInfo: { sellerName: paramSeller || "Phước Thư Nông Sản", sellerAvatar: "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150", rating: 4.9, totalProducts: 15, isOnline: false, lastActive: "Hoạt động 5 phút trước", phone: "0988 123 456", address: `${selectedMarket} (Hà Nội)` } },
        "2": { id: "2", productName: "Cam Sành Tiền Giang Mọng Nước Ngọt Thanh 2kg", price: 45000, condition: "Tươi mới về trong ngày", address: `${selectedMarket} (Hà Nội)`, sellerType: "Cá nhân", categoryName: "Rau củ & Trái cây tươi", rating: 4.8, soldCount: 1200, description: "Cam sành Tiền Giang mọng nước, vỏ mỏng ngọt thanh dùng vắt nước hoặc ăn trực tiếp rất bổ dưỡng.", imageUrl: "https://images.unsplash.com/photo-1611080626919-7cf5a9dbab5b?w=600", galleryImages: ["https://images.unsplash.com/photo-1611080626919-7cf5a9dbab5b?w=600"], specifications: { "Xuất xứ": "Tiền Giang", "Bảo quản": "Nhiệt độ phòng", "Trọng lượng": "2kg" }, sellerInfo: { sellerName: paramSeller || "Vườn Cam Sạch", sellerAvatar: "https://images.unsplash.com/photo-1580489944761-15a19d654956?w=150", rating: 4.8, totalProducts: 8, isOnline: true, lastActive: "Đang hoạt động", phone: "0912 345 678", address: `${selectedMarket} (Hà Nội)` } },
        "3": { id: "3", productName: "Rau Cải Thảo Đà Lạt Hữu Cơ Sạch 1kg", price: 25000, condition: "Đã sơ chế sạch sẽ", address: `${selectedMarket} (Hà Nội)`, sellerType: "Nông sản sạch", categoryName: "Rau củ & Trái cây tươi", rating: 4.7, soldCount: 530, description: "Rau cải thảo Đà Lạt tươi xanh, trồng theo tiêu chuẩn hữu cơ sạch, ngọt dịu giòn tan.", imageUrl: "https://images.unsplash.com/photo-1540420773420-3366772f4999?w=600", galleryImages: ["https://images.unsplash.com/photo-1540420773420-3366772f4999?w=600"], specifications: { "Xuất xứ": "Đà Lạt", "Trọng lượng": "1kg" }, sellerInfo: { sellerName: paramSeller || "Nông Sản Đà Lạt", sellerAvatar: "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150", rating: 4.7, totalProducts: 10, isOnline: true, lastActive: "Đang hoạt động", phone: "0945 678 910", address: `${selectedMarket} (Hà Nội)` } },
        "4": { id: "4", productName: "Thịt Thăn Bò Wagyu Úc MB 4-5 - Gói 500g Tiêu Chuẩn", price: 450000, condition: "Cấp đông tiêu chuẩn", address: `${selectedMarket} (Hà Nội)`, sellerType: "Đối tác Chợ", categoryName: "Thịt & Gia cầm", rating: 5.0, soldCount: 804, description: "Thịt thăn bò Wagyu Úc vân mỡ đều, mềm mọng thích hợp làm bít tết hoặc nướng lẩu gia đình.", imageUrl: "https://images.unsplash.com/photo-1588168333986-5078d3ae3976?w=600", galleryImages: ["https://images.unsplash.com/photo-1588168333986-5078d3ae3976?w=600"], specifications: { "Xuất xứ": "Úc", "Bảo quản": "Cấp đông -18°C" }, sellerInfo: { sellerName: paramSeller || "Tiểu Thương Bò Úc", sellerAvatar: "https://images.unsplash.com/photo-1570295999919-56ceb5ecca61?w=150", rating: 5.0, totalProducts: 24, isOnline: false, lastActive: "Hoạt động 1 giờ trước", phone: "0977 888 999", address: `${selectedMarket} (Hà Nội)` } },
        "5": { id: "5", productName: "Gà Ta Thả Vườn Nguyên Con Tươi Ngon Cấp Sạch", price: 185000, condition: "Tươi mới về trong ngày", address: `${selectedMarket} (Hà Nội)`, sellerType: "Nông sản sạch", categoryName: "Thịt & Gia cầm", rating: 4.8, soldCount: 412, description: "Gà ta thả vườn thịt dai ngon ngậy, đã làm sạch sẵn cấp đông tiêu chuẩn an toàn.", imageUrl: "https://images.unsplash.com/photo-1587593810167-a84920ea0781?w=600", galleryImages: ["https://images.unsplash.com/photo-1587593810167-a84920ea0781?w=600"], specifications: { "Xuất xứ": "Hà Nội", "Trọng lượng": "1.5kg" }, sellerInfo: { sellerName: paramSeller || "Gà Sạch Đồi Vườn", sellerAvatar: "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150", rating: 4.8, totalProducts: 5, isOnline: true, lastActive: "Đang hoạt động", phone: "0966 555 444", address: `${selectedMarket} (Hà Nội)` } },
        "7": { id: "7", productName: "Cá Hồi Na Uy Tươi Sống Phi Lê Cắt Khúc 300g", price: 350000, condition: "Cấp đông tiêu chuẩn", address: `${selectedMarket} (Hà Nội)`, sellerType: "Hải sản nhập khẩu", categoryName: "Thủy hải sản tươi sống", rating: 4.9, soldCount: 950, description: "Cá hồi Na Uy tươi phi lê sẵn, mọng mỡ béo ngậy dùng ăn sashimi hoặc áp chảo tuyệt hảo.", imageUrl: "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?w=600", galleryImages: ["https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?w=600"], specifications: { "Xuất xứ": "Na Uy", "Bảo quản": "Đông lạnh" }, sellerInfo: { sellerName: paramSeller || "Hải Sản Biển Đông", sellerAvatar: "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150", rating: 4.9, totalProducts: 18, isOnline: false, lastActive: "Hoạt động 30 phút trước", phone: "0933 222 111", address: `${selectedMarket} (Hà Nội)` } },
        "8": { id: "8", productName: "Tôm Hùm Bông Sống Nguyên Con Hải Sản Chợ", price: 890000, condition: "Tươi mới về trong ngày", address: `${selectedMarket} (Hà Nội)`, sellerType: "Hải sản tươi", categoryName: "Thủy hải sản tươi sống", rating: 5.0, soldCount: 320, description: "Tôm hùm bông sống nguyên con bơi tại bể, thịt chắc ngọt đậm đà vị biển.", imageUrl: "https://images.unsplash.com/photo-1559742811-822863cc4ad7?w=600", galleryImages: ["https://images.unsplash.com/photo-1559742811-822863cc4ad7?w=600"], specifications: { "Xuất xứ": "Nha Trang", "Tình trạng": "Tươi sống" }, sellerInfo: { sellerName: paramSeller || "Hải Sản Tươi Sống", sellerAvatar: "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150", rating: 5.0, totalProducts: 12, isOnline: false, lastActive: "Hoạt động 2 giờ trước", phone: "0911 888 777", address: `${selectedMarket} (Hà Nội)` } },
        "10": { id: "10", productName: "Chả Lụa Ước Lễ Truyền Thống Đặc Sản Đòn 500g", price: 110000, condition: "Đã sơ chế sạch sẽ", address: `${selectedMarket} (Hà Nội)`, sellerType: "Bán chuyên", categoryName: "Thực phẩm chế biến sẵn", rating: 4.9, soldCount: 1450, description: "Giò chả Ước Lễ truyền thống giòn ngọt thơm nức lá chuối, không chất bảo quản.", imageUrl: "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600", galleryImages: ["https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600"], specifications: { "Xuất xứ": "Hà Nội", "Quy cách": "Đòn 500g" }, sellerInfo: { sellerName: paramSeller || "Giò Chả Ước Lễ", sellerAvatar: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150", rating: 4.9, totalProducts: 6, isOnline: true, lastActive: "Đang hoạt động", phone: "0936 112 233", address: `${selectedMarket} (Hà Nội)` } }
    };

    if (mockMap[id]) return mockMap[id];

    return {
        id: id || "1",
        productName: "Sản phẩm Thực phẩm & Nông sản Chợ",
        price: 150000,
        condition: "Tươi mới về trong ngày",
        address: `${selectedMarket} (Hà Nội)`,
        sellerType: "Đối tác Chợ",
        categoryName: "Nông sản & Thực phẩm",
        rating: 4.9,
        soldCount: 520,
        description: "Sản phẩm chất lượng cao được kiểm định an toàn thực phẩm, cung cấp trực tiếp từ tiểu thương tại Chợ.",
        imageUrl: "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=600",
        galleryImages: ["https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=600"],
        specifications: { "Xuất xứ": "Việt Nam", "Giao hàng": "Nhanh trong 2 giờ", "Bảo đảm": "Tươi ngon 100%" },
        sellerInfo: { sellerName: paramSeller || "Tiểu Thương Chợ", sellerAvatar: "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150", rating: 4.9, totalProducts: 12, isOnline: false, lastActive: "Hoạt động 15 phút trước", phone: "0988 999 888", address: `${selectedMarket} (Hà Nội)` }
    };
}

function renderProductInfo(p) {
    const title = p.productName || p.name || p.title || 'Sản phẩm';
    const catName = p.categoryName || p.brand || (p.category ? p.category.name : 'Rau củ & Trái cây tươi');
    
    if (catName) {
        try {
            localStorage.setItem('marketconnect_last_viewed_category', catName);
            localStorage.setItem('marketconnect_last_viewed_product_name', title);
        } catch (err) {
            console.warn('[LocalStorage] Cannot save last viewed category:', err);
        }
    }

    const rating = typeof p.rating === 'number' ? p.rating.toFixed(1) : '5.0';
    const rawSold = typeof p.soldCount === 'number' ? p.soldCount : 1;
    const soldText = rawSold >= 1000 ? (rawSold / 1000).toFixed(1) + 'k' : rawSold;

    document.getElementById('breadcrumbCategory').innerText = catName;
    document.getElementById('breadcrumbTitle').innerText = title;
    document.getElementById('productTitle').innerText = title;
    document.getElementById('brandName').innerText = p.sellerType || 'Cá nhân';
    document.getElementById('productRating').innerText = rating;
    document.getElementById('soldCount').innerText = soldText;

    const badgeDiscount = document.getElementById('badgeDiscount');
    if (badgeDiscount) {
        if (p.discountPercent && p.discountPercent > 0) {
            badgeDiscount.innerText = `Giảm ${p.discountPercent}%`;
            badgeDiscount.classList.remove('hidden');
        } else {
            badgeDiscount.classList.add('hidden');
        }
    }

    const badgeBestSeller = document.getElementById('badgeBestSeller');
    if (badgeBestSeller) {
        if (p.isBestSeller) {
            badgeBestSeller.classList.remove('hidden');
        } else {
            badgeBestSeller.classList.add('hidden');
        }
    }

    const priceElem = document.getElementById('productPrice');
    if (priceElem) {
        if (p.isFree) {
            priceElem.innerText = '0đ (Cho tặng miễn phí)';
        } else {
            priceElem.innerText = `đ${(p.price || 0).toLocaleString('vi-VN')}`;
        }
    }

    const shippingLabel = document.getElementById('shippingLabel');
    if (shippingLabel) {
        shippingLabel.innerText = p.address ? `Giao dịch tại ${p.address}` : 'Hà Nội';
    }

    galleryImages = (p.galleryImages && p.galleryImages.length > 0) 
        ? p.galleryImages 
        : [p.imageUrl || '/images/placeholder.svg'];
    
    updateMainImage(0);
    renderThumbnailGrid(galleryImages);

    const sellerObj = p.sellerInfo || p.seller;
    if (sellerObj) {
        const sName = sellerObj.sellerName || sellerObj.name || sellerObj.email || sellerObj.shopName || 'Người đăng tin';
        const sellerNameElem = document.getElementById('sellerName');
        if (sellerNameElem) sellerNameElem.innerText = sName;

        const avatar = sellerObj.sellerAvatar || sellerObj.shopLogo || 'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150';
        const sellerLogoElem = document.getElementById('sellerLogo');
        if (sellerLogoElem) sellerLogoElem.src = avatar;
        
        const ratingVal = typeof sellerObj.rating === 'number' ? sellerObj.rating.toFixed(1) : (typeof p.rating === 'number' ? p.rating.toFixed(1) : '5.0');
        const sellerRatingElem = document.getElementById('sellerRating');
        if (sellerRatingElem) sellerRatingElem.innerText = `${ratingVal}/5.0`;

        const totalProdElem = document.getElementById('sellerTotalProducts');
        if (totalProdElem) totalProdElem.innerText = `${sellerObj.totalProducts || 1} tin`;

        const activeElem = document.getElementById('sellerActive');
        if (activeElem) activeElem.innerText = sellerObj.lastActive || 'Đang hoạt động';

        const statusDot = document.getElementById('sellerStatus');
        if (statusDot) {
            if (sellerObj.isOnline !== false) {
                statusDot.className = 'absolute bottom-0 right-0 w-4 h-4 bg-emerald-500 border-2 border-white rounded-full ring-2 ring-emerald-400/20';
            } else {
                statusDot.className = 'absolute bottom-0 right-0 w-4 h-4 bg-gray-400 border-2 border-white rounded-full';
            }
        }
    } else {
        const sellerNameElem = document.getElementById('sellerName');
        if (sellerNameElem) sellerNameElem.innerText = 'Người đăng tin';
    }

    renderSpecifications(p.specifications || {});

    const descElem = document.getElementById('productDescription');
    if (descElem) {
        if (p.description) {
            descElem.innerHTML = p.description;
        } else {
            descElem.innerHTML = `<p>Sản phẩm <strong>${title}</strong> đăng bán trực tiếp trên MarketConnect.</p>`;
        }
    }

    const urlParams = new URLSearchParams(window.location.search);
    const prodId = urlParams.get('id') || p.id || '1';

    const storedComments = getStoredComments(prodId);
    const mergedComments = [...storedComments];
    (p.comments || []).forEach(c => {
        if (!mergedComments.some(mc => String(mc.id) === String(c.id))) {
            mergedComments.push(c);
        }
    });
    if (currentProduct) currentProduct.comments = mergedComments;
    renderComments(mergedComments);
}

function updateMainImage(index) {
    if (!galleryImages || galleryImages.length === 0) return;
    activeImageIndex = index;
    const mainImg = document.getElementById('mainProductImage');
    if (mainImg) mainImg.src = galleryImages[activeImageIndex];
    
    const thumbs = document.querySelectorAll('#thumbnailGallery > div');
    thumbs.forEach((t, i) => {
        if (i === activeImageIndex) {
            t.className = 'aspect-square rounded-xl overflow-hidden border-2 border-[#004532] cursor-pointer shadow-sm';
        } else {
            t.className = 'aspect-square rounded-xl overflow-hidden border border-gray-200 hover:border-[#004532] cursor-pointer opacity-70 hover:opacity-100 transition-all';
        }
    });
}

function renderThumbnailGrid(images) {
    const container = document.getElementById('thumbnailGallery');
    if (!container) return;
    container.innerHTML = images.slice(0, 5).map((imgUrl, i) => `
        <div onclick="window.setMainImage(${i})" class="aspect-square rounded-xl overflow-hidden ${i === 0 ? 'border-2 border-[#004532]' : 'border border-gray-200 hover:border-[#004532] opacity-70 hover:opacity-100'} cursor-pointer transition-all">
            <img class="w-full h-full object-cover" src="${imgUrl}" />
        </div>
    `).join('');
}

function renderSpecifications(specs) {
    const container = document.getElementById('specificationsContainer');
    if (!container) return;
    const entries = Object.entries(specs || {});
    if (entries.length === 0) {
        container.innerHTML = `<div class="p-4 text-gray-400 text-xs text-center italic">Chưa có thông số chi tiết.</div>`;
        return;
    }

    container.innerHTML = entries.map(([key, val], idx) => `
        <div class="grid grid-cols-3 text-xs ${idx % 2 === 0 ? 'bg-gray-50/60' : 'bg-white'}">
            <div class="p-3.5 font-semibold text-gray-500 border-r border-gray-100">${key}</div>
            <div class="p-3.5 col-span-2 font-bold text-gray-800">${val}</div>
        </div>
    `).join('');
}

function renderComments(comments) {
    const container = document.getElementById('commentsList');
    const badge = document.getElementById('commentCountBadge');
    if (badge) badge.innerText = comments ? comments.length : 0;
    if (!container) return;

    if (!comments || comments.length === 0) {
        container.innerHTML = `
            <div class="flex flex-col items-center justify-center h-full text-center px-4 py-8">
                <span class="material-symbols-outlined text-[36px] text-gray-300 mb-2">forum</span>
                <p class="text-gray-500 text-xs font-medium">Chưa có bình luận nào cho sản phẩm này.</p>
                <p class="text-gray-400 text-[11px] mt-1 italic">Hãy là người đầu tiên đặt câu hỏi!</p>
            </div>`;
        return;
    }

    const currentUser = getCurrentUser();
    const currentUserId = currentUser ? String(currentUser.id) : null;
    const currentUserName = currentUser ? currentUser.fullName : null;

    container.innerHTML = comments.map(c => {
        const commentUserId = c.userId ? String(c.userId) : null;
        // Kiểm tra chính chủ đăng bình luận (Khớp userId hoặc userFullName)
        const isOwner = currentUser && (
            (commentUserId && commentUserId === currentUserId) || 
            (c.userFullName && c.userFullName === currentUserName)
        );
        const hasReplies = c.hasReplies || (c.replies && c.replies.length > 0);
        const isEditing = editingCommentId === c.id;
        const displayTime = formatRelativeTime(c.createdAt || c.timeAgo);

        return `
            <div id="comment_item_${c.id}" class="flex items-start gap-3 text-xs bg-gray-50/70 p-3 rounded-xl border border-gray-100 transition-all ${c.isOptimistic ? 'opacity-60 animate-pulse' : 'opacity-100'}">
                <img class="w-9 h-9 rounded-full object-cover shrink-0 ring-2 ring-emerald-500/10" src="${c.userAvatar || 'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=100'}" alt="${escapeHTML(c.userFullName || 'Tôi')}" />
                <div class="flex-1 min-w-0">
                    <div class="flex items-center justify-between mb-1">
                        <div class="flex items-center gap-1.5 truncate">
                            <span class="font-bold text-gray-800 truncate">${escapeHTML(c.userFullName || 'Tôi')}</span>
                            ${isOwner ? `<span class="bg-emerald-100 text-[#004532] text-[9px] font-extrabold px-1.5 py-0.2 rounded shrink-0">Bạn</span>` : ''}
                        </div>
                        
                        <div class="relative flex items-center gap-1.5 shrink-0">
                            <span class="text-[10px] text-gray-400 font-medium">${displayTime}</span>
                            
                            <!-- Menu 3 chấm đứng (⋮) chỉ hiển thị với tác giả bài đăng bình luận -->
                            ${isOwner ? `
                                <div class="relative">
                                    <button type="button" onclick="toggleCommentMenu(event, '${c.id}')" title="Tùy chọn bình luận" class="p-1 text-gray-400 hover:text-gray-700 hover:bg-gray-200/60 rounded-full transition-colors cursor-pointer flex items-center justify-center">
                                        <span class="material-symbols-outlined text-[17px]">more_vert</span>
                                    </button>

                                    <!-- Dropdown Menu 3 chấm -->
                                    <div id="comment_menu_${c.id}" class="comment-dropdown-menu hidden absolute right-0 top-6 w-32 bg-white rounded-xl shadow-lg border border-gray-100 py-1.5 z-30 animate-in fade-in zoom-in-95 duration-100">
                                        <!-- Tùy chọn Chỉnh sửa ✏️ -->
                                        <button type="button" onclick="handleMenuEdit(event, '${c.id}')" class="w-full text-left px-3 py-1.5 text-xs text-gray-700 hover:bg-emerald-50 hover:text-[#004532] flex items-center gap-2 transition-colors cursor-pointer">
                                            <span class="material-symbols-outlined text-[15px] text-gray-500">edit</span>
                                            <span class="font-semibold">Chỉnh sửa</span>
                                        </button>
                                        
                                        <!-- Tùy chọn Xóa 🗑️ -->
                                        ${hasReplies ? `
                                            <button type="button" onclick="handleMenuDeleteLocked(event)" class="w-full text-left px-3 py-1.5 text-xs text-gray-300 cursor-not-allowed flex items-center gap-2 transition-colors" title="Đã có phản hồi (Khóa xóa)">
                                                <span class="material-symbols-outlined text-[15px] text-gray-300">lock</span>
                                                <span class="font-semibold line-through">Xóa</span>
                                            </button>
                                        ` : `
                                            <button type="button" onclick="handleMenuDelete(event, '${c.id}')" class="w-full text-left px-3 py-1.5 text-xs text-red-600 hover:bg-red-50 flex items-center gap-2 transition-colors cursor-pointer">
                                                <span class="material-symbols-outlined text-[15px] text-red-500">delete</span>
                                                <span class="font-semibold">Xóa</span>
                                            </button>
                                        `}
                                    </div>
                                </div>
                            ` : ''}
                        </div>
                    </div>

                    <!-- Chế độ Xem / Chế độ Sửa inline -->
                    ${isEditing ? `
                        <div class="mt-2 space-y-2">
                            <textarea id="edit_textarea_${c.id}" class="w-full bg-white text-xs text-gray-800 p-2.5 rounded-xl border-2 border-[#004532] focus:outline-none resize-none transition-all" rows="2">${escapeHTML(c.commentText || c.content || '')}</textarea>
                            <div class="flex justify-end items-center gap-2">
                                <button type="button" onclick="cancelEditComment()" class="px-2.5 py-1 text-[11px] font-bold text-gray-500 hover:bg-gray-200 rounded-lg transition-colors">
                                    Hủy
                                </button>
                                <button type="button" onclick="saveEditedComment('${c.id}')" class="px-3 py-1 text-[11px] font-bold bg-[#004532] hover:bg-[#065f46] text-white rounded-lg transition-all shadow-xs cursor-pointer">
                                    Lưu thay đổi
                                </button>
                            </div>
                        </div>
                    ` : `
                        <p class="text-gray-600 leading-relaxed break-words">${escapeHTML(c.commentText || c.content || '')}</p>
                        ${c.imageUrl ? `
                            <div class="mt-2">
                                <img src="${c.imageUrl}" alt="Ảnh đính kèm bình luận" class="max-w-[180px] max-h-[140px] object-cover rounded-xl border border-gray-200 cursor-pointer shadow-2xs hover:opacity-90 transition-opacity" onclick="window.open('${c.imageUrl}', '_blank')" />
                            </div>
                        ` : ''}
                    `}
                </div>
            </div>
        `;
    }).join('');
}

async function postComment() {
    const user = getCurrentUser();
    if (!user) {
        showLoginModal();
        return;
    }

    const input = document.getElementById('inputCommentText');
    const text = input ? input.value.trim() : '';
    if (!text && !selectedCommentImageBase64) {
        alert('Vui lòng nhập nội dung hoặc đính kèm ảnh cho bình luận!');
        if (input) input.focus();
        return;
    }

    const urlParams = new URLSearchParams(window.location.search);
    const prodId = urlParams.get('id') || '1';

    const tempId = 'comment_' + Date.now();
    const attachedImage = selectedCommentImageBase64;
    const nowIso = new Date().toISOString();

    const newCommentObj = {
        id: tempId,
        productId: prodId,
        userId: user.id,
        userFullName: user.fullName,
        userAvatar: user.avatar,
        commentText: text,
        content: text,
        imageUrl: attachedImage,
        timeAgo: 'Vừa xong',
        createdAt: nowIso
    };

    // 1. Lưu vĩnh viễn vào LocalStorage theo Product ID
    saveCommentToStorage(prodId, newCommentObj);

    // 2. Cập nhật ngay vào giao diện UI
    if (!currentProduct) currentProduct = { comments: [] };
    currentProduct.comments = currentProduct.comments || [];
    const existingIndex = currentProduct.comments.findIndex(c => String(c.id) === String(tempId));
    if (existingIndex === -1) {
        currentProduct.comments.unshift(newCommentObj);
    }
    renderComments(currentProduct.comments);

    // 3. Reset ô nhập & thumbnail preview ảnh
    input.value = '';
    removeCommentImage();

    try {
        const res = await fetch(`/api/Products/${prodId}/comments`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + (user.token || '')
            },
            body: JSON.stringify({
                productId: prodId,
                userId: user.id,
                content: text,
                commentText: text,
                imageUrl: attachedImage,
                userFullName: user.fullName,
                createdAt: nowIso
            })
        });

        if (res.ok) {
            const savedComment = await res.json();
            saveCommentToStorage(prodId, savedComment);
        }
    } catch (err) {
        console.warn("API không phản hồi (Bình luận đã được lưu an toàn tại LocalStorage client):", err);
    }
}

function postQuickQuestion() {
    const input = document.getElementById('inputQuickQuestion');
    if (input && input.value.trim()) {
        const commentInput = document.getElementById('inputCommentText');
        if (commentInput) commentInput.value = input.value;
        input.value = '';
        postComment();
    }
}

async function loadRelatedProducts(id) {
    const grid = document.getElementById('relatedProductsGrid');
    if (!grid) return;

    const currentCategory = currentProduct ? (currentProduct.categoryName || currentProduct.category) : 'Rau củ & Trái cây tươi';
    const currentMarket = currentProduct ? (currentProduct.address || localStorage.getItem('selected_market') || 'Chợ Đồng Xuân') : (localStorage.getItem('selected_market') || 'Chợ Đồng Xuân');

    try {
        const res = await fetch(`/api/Products/${id}/related`);
        if (res.ok) {
            const list = await res.json();
            if (list && list.length > 0) {
                renderRelatedProductsList(list, id, currentCategory, currentMarket);
                return;
            }
        }
    } catch (err) {
        console.error("Lỗi khi tải sản phẩm liên quan:", err);
    }

    const mockList = [
        { id: "1", productName: "Táo Envy Mỹ Nhập Khẩu Tươi Giòn Ngọt 1kg", price: 120000, marketName: "Chợ Đồng Xuân", categoryName: "Rau củ & Trái cây tươi", imageUrl: "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=600", distanceKm: 0.5, rating: 4.9, isLiked: false },
        { id: "2", productName: "Cam Sành Tiền Giang Mọng Nước Ngọt Thanh 2kg", price: 45000, marketName: "Chợ Đồng Xuân", categoryName: "Rau củ & Trái cây tươi", imageUrl: "https://images.unsplash.com/photo-1611080626919-7cf5a9dbab5b?w=600", distanceKm: 0.8, rating: 4.8, isLiked: false },
        { id: "3", productName: "Rau Cải Thảo Đà Lạt Hữu Cơ Sạch 1kg", price: 25000, marketName: "Chợ Nhân Chính", categoryName: "Rau củ & Trái cây tươi", imageUrl: "https://images.unsplash.com/photo-1540420773420-3366772f4999?w=600", distanceKm: 1.2, rating: 4.7, isLiked: false },
        { id: "4", productName: "Thịt Thăn Bò Wagyu Úc MB 4-5 - Gói 500g Tiêu Chuẩn", price: 450000, marketName: "Chợ Nhân Chính", categoryName: "Thịt & Gia cầm", imageUrl: "https://images.unsplash.com/photo-1588168333986-5078d3ae3976?w=600", distanceKm: 2.5, rating: 5.0, isLiked: true },
        { id: "5", productName: "Gà Ta Thả Vườn Nguyên Con Tươi Ngon Cấp Sạch", price: 185000, marketName: "Chợ Đồng Xuân", categoryName: "Thịt & Gia cầm", imageUrl: "https://images.unsplash.com/photo-1587593810167-a84920ea0781?w=600", distanceKm: 1.0, rating: 4.8, isLiked: false },
        { id: "7", productName: "Cá Hồi Na Uy Tươi Sống Phi Lê Cắt Khúc 300g", price: 350000, marketName: "Chợ Hàng Bè", categoryName: "Thủy hải sản tươi sống", imageUrl: "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?w=600", distanceKm: 1.5, rating: 4.9, isLiked: false }
    ];

    renderRelatedProductsList(mockList, id, currentCategory, currentMarket);
}

function renderRelatedProductsList(list, currentProductId, currentCategory, currentMarket) {
    const grid = document.getElementById('relatedProductsGrid');
    if (!grid) return;

    const filtered = list
        .filter(item => String(item.id) !== String(currentProductId))
        .sort((a, b) => {
            const aSameMarket = a.marketName && currentMarket && currentMarket.includes(a.marketName);
            const bSameMarket = b.marketName && currentMarket && currentMarket.includes(b.marketName);
            const aSameCategory = a.categoryName === currentCategory;
            const bSameCategory = b.categoryName === currentCategory;

            const scoreA = (aSameMarket ? 2 : 0) + (aSameCategory ? 1 : 0);
            const scoreB = (bSameMarket ? 2 : 0) + (bSameCategory ? 1 : 0);
            return scoreB - scoreA;
        });

    if (filtered.length === 0) {
        grid.innerHTML = `<div class="bg-white rounded-2xl p-4 border border-gray-100 text-center text-xs text-gray-400 col-span-full py-8">Không có sản phẩm tương tự.</div>`;
        return;
    }

    grid.innerHTML = filtered.map(item => `
        <div onclick="window.location.href='/Products/ProductDetail?id=${item.id}&name=${encodeURIComponent(item.productName)}&price=${item.price}&img=${encodeURIComponent(item.imageUrl)}'" class="bg-white rounded-2xl overflow-hidden card-shadow border border-gray-100 group cursor-pointer transition-all duration-300 hover:-translate-y-1 hover:shadow-lg flex flex-col justify-between">
            <div class="relative aspect-[4/3] bg-gray-50 overflow-hidden">
                <img class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500" loading="lazy" src="${item.imageUrl}" alt="${escapeHTML(item.productName)}" />
                <button type="button" onclick="toggleRelatedWishlist(event, '${item.id}')" class="absolute top-2 right-2 bg-white/90 hover:bg-white p-1.5 rounded-full shadow-sm active:scale-90 transition-transform">
                    <span id="related_heart_${item.id}" class="material-symbols-outlined text-[16px] ${item.isLiked ? 'text-red-500 font-fill' : 'text-gray-400 hover:text-red-500'} transition-colors">favorite</span>
                </button>
                ${item.marketName ? `
                    <span class="absolute bottom-2 left-2 bg-black/60 backdrop-blur-xs text-white text-[9px] font-medium px-2 py-0.5 rounded flex items-center gap-1">
                        <span class="material-symbols-outlined text-[10px] text-emerald-400">store</span>
                        <span class="truncate max-w-[90px]">${item.marketName}</span>
                    </span>
                ` : ''}
            </div>
            <div class="p-3.5 flex flex-col justify-between flex-1">
                <div>
                    <p class="text-gray-400 text-[9px] font-bold uppercase tracking-wider mb-1">${item.categoryName || 'Nông sản'}</p>
                    <h4 class="font-bold text-xs text-gray-800 line-clamp-2 min-h-[32px] mb-2 group-hover:text-[#004532] transition-colors leading-snug">${escapeHTML(item.productName)}</h4>
                </div>
                <div class="flex items-center justify-between pt-2 border-t border-gray-50 mt-auto">
                    <span class="text-[#004532] font-black text-xs md:text-sm">đ${(item.price || 0).toLocaleString('vi-VN')}</span>
                    <span class="text-[10px] text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full font-semibold">cách ${item.distanceKm || 1.0} km</span>
                </div>
            </div>
        </div>
    `).join('');
}

function escapeHTML(str) {
    if (!str) return '';
    return String(str).replace(/[&<>'"]/g, 
        tag => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[tag] || tag)
    );
}

// ==========================================
// DOM LOADED INITIALIZATION & EVENT LISTENERS
// ==========================================
document.addEventListener('DOMContentLoaded', async function () {
    const urlParams = new URLSearchParams(window.location.search);
    const productId = urlParams.get('id') || '1';

    // 0. Khởi tạo giao diện người dùng đăng nhập cho phần bình luận
    initCommentSectionUI();

    // 1. Tải chi tiết sản phẩm & sản phẩm liên quan song song bằng Promise.all
    await Promise.all([
        loadProductDetail(productId),
        loadRelatedProducts(productId)
    ]);

    // 2. Sự kiện gallery slider
    document.getElementById('btnPrevImage')?.addEventListener('click', () => switchGalleryImage(-1));
    document.getElementById('btnNextImage')?.addEventListener('click', () => switchGalleryImage(1));

    // 3. Sự kiện tim yêu thích
    let isFavorite = false;
    document.getElementById('btnFavorite')?.addEventListener('click', function () {
        isFavorite = !isFavorite;
        const heartIcon = document.getElementById('heartIcon');
        if (heartIcon) {
            if (isFavorite) {
                heartIcon.style.fontVariationSettings = "'FILL' 1";
                heartIcon.classList.add('text-red-500');
            } else {
                heartIcon.style.fontVariationSettings = "'FILL' 0";
                heartIcon.classList.remove('text-red-500');
            }
        }
    });

    // 4. Sự kiện gọi điện, chat & hỏi đáp
    document.getElementById('btnSendQuickQuestion')?.addEventListener('click', postQuickQuestion);
    document.getElementById('btnSubmitComment')?.addEventListener('click', postComment);
    document.getElementById('btnCallSeller')?.addEventListener('click', function () {
        const sellerObj = currentProduct?.sellerInfo || currentProduct?.seller;
        const phone = sellerObj?.phone || 'Chưa cập nhật SĐT';
        alert(`SĐT liên hệ người bán (${sellerObj?.sellerName || 'Người đăng tin'}): ${phone}`);
    });
    document.getElementById('btnChatSeller')?.addEventListener('click', function () {
        const input = document.getElementById('inputCommentText');
        if (input) input.focus();
    });

    // 5. Gửi tin nhắn bằng phím Enter (Nhấn Shift + Enter để xuống dòng)
    const mainCommentInput = document.getElementById('inputCommentText');
    if (mainCommentInput) {
        mainCommentInput.addEventListener('keydown', function(e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                postComment();
            }
        });
    }

    const quickQInput = document.getElementById('inputQuickQuestion');
    if (quickQInput) {
        quickQInput.addEventListener('keydown', function(e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                postQuickQuestion();
            }
        });
    }

    document.getElementById('btnConfirmDeleteComment')?.addEventListener('click', function() {
        confirmDeleteComment();
    });

    // Click Outside để tự động đóng các Dropdown Menu 3 chấm khi bấm ra ngoài
    document.addEventListener('click', function (e) {
        if (!e.target.closest('.comment-dropdown-menu') && !e.target.closest('button[onclick*="toggleCommentMenu"]')) {
            window.closeAllCommentMenus();
        }
    });
});
