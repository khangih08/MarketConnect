/**
 * store-detail.js - Frontend interactions for Store Detail page
 * Separated from Razor CSHTML for clean FE/BE separation.
 */

document.addEventListener('DOMContentLoaded', function () {
    initAddProductModal();
});

function openAddProductModal() {
    const modal = document.getElementById('addProductModal');
    if (modal) {
        modal.classList.remove('hidden');
        modal.classList.add('flex');
        document.body.style.overflow = 'hidden';
    }
}

function closeAddProductModal() {
    const modal = document.getElementById('addProductModal');
    if (modal) {
        modal.classList.add('hidden');
        modal.classList.remove('flex');
        document.body.style.overflow = '';
    }
}

function initAddProductModal() {
    const modal = document.getElementById('addProductModal');
    const btnClose = document.getElementById('btnCloseAddProductModal');
    const btnCancel = document.getElementById('btnCancelAddProductModal');
    const isFreeCheckbox = document.getElementById('modalIsFree');
    const priceInput = document.getElementById('modalPrice');
    const imageInput = document.getElementById('modalImageFile');

    // Attach click listeners to any open buttons
    document.querySelectorAll('#btnOpenAddProductModal, #btnOpenAddProductModalHeader, .btn-open-add-product-modal').forEach(btn => {
        btn.addEventListener('click', openAddProductModal);
    });

    if (btnClose) btnClose.addEventListener('click', closeAddProductModal);
    if (btnCancel) btnCancel.addEventListener('click', closeAddProductModal);

    // Close on backdrop click
    if (modal) {
        modal.addEventListener('click', function (e) {
            if (e.target === modal) {
                closeAddProductModal();
            }
        });
    }

    // Toggle free price input
    if (isFreeCheckbox && priceInput) {
        isFreeCheckbox.addEventListener('change', function () {
            if (this.checked) {
                priceInput.value = '0';
                priceInput.disabled = true;
                priceInput.classList.add('bg-gray-100', 'text-gray-400');
            } else {
                priceInput.disabled = false;
                priceInput.classList.remove('bg-gray-100', 'text-gray-400');
            }
        });
    }

    // Image preview
    if (imageInput) {
        imageInput.addEventListener('change', function () {
            previewStoreProductImage(this);
        });
    }
}

function previewStoreProductImage(input) {
    const previewContainer = document.getElementById('modalImagePreviewContainer');
    const imgPreview = document.getElementById('modalImgPreview');
    if (input.files && input.files[0]) {
        const file = input.files[0];
        const reader = new FileReader();
        reader.onload = function (e) {
            if (imgPreview && previewContainer) {
                imgPreview.src = e.target.result;
                previewContainer.classList.remove('hidden');
            }
        };
        reader.readAsDataURL(file);
    }
}
