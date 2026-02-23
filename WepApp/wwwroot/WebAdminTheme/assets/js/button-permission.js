/**
 * Buton Yetkilendirme Sistemi
 * Layout'a eklenecek script
 * Tüm sayfalarda butonları otomatik gizler/gösterir
 */

class ButtonPermissionManager {
    constructor() {
        this.permissions = {};
        this.currentUserType = this.getCurrentUserType();
        this.currentPage = this.getCurrentPageName();
        this.initialized = false;
        this.retryCount = 0;
        this.maxRetry = 3;
   
    }

    // Kullanıcı tipini al (cookie'den veya localStorage'dan)
    getCurrentUserType() {
        // Cookie'den almayı dene
        const cookies = document.cookie.split(';');
        for (let cookie of cookies) {
            const [name, value] = cookie.trim().split('=');
            if (name === 'UserType' || name === 'KullaniciTipi' || name === 'kullaniciTipi') {
                return decodeURIComponent(value);
            }
        }

        // localStorage'dan al
        const userType = localStorage.getItem('userType') ||
            localStorage.getItem('KullaniciTipi') ||
            localStorage.getItem('kullaniciTipi');
        if (userType) return userType;

        // URL'den almayı dene (bazı sistemlerde)
        const urlParams = new URLSearchParams(window.location.search);
        const urlUserType = urlParams.get('kullaniciTipi');
        if (urlUserType) return urlUserType;

        // Varsayılan olarak Admin
        return 'Admin';
    }

    // Mevcut sayfa adını al (controller)
    getCurrentPageName() {
        const path = window.location.pathname;
        const segments = path.split('/').filter(segment => segment);

        if (segments.length > 0) {
            // Controller adını al (ilk segment)
            let controller = segments[0];
            // Controller kelimesini temizle
            controller = controller.replace('Controller', '');
            return controller;
        }
        return 'Home';
    }

    // İzinleri backend'den yükle
    async loadPermissions() {
        try {
            const response = await fetch('/AdminButton/TumIzinleriGetirJson');

            if (response.ok) {
                const data = await response.json();
                this.permissions = this.formatPermissions(data);
                this.initialized = true;
                this.retryCount = 0;
                this.applyPermissions();
            } else {
                if (this.retryCount < this.maxRetry) {
                    this.retryCount++;
                    setTimeout(() => this.loadPermissions(), 1000);
                }
            }
        } catch (error) {
            if (this.retryCount < this.maxRetry) {
                this.retryCount++;
                setTimeout(() => this.loadPermissions(), 1000);
            }
        }
    }

    // Gelen veriyi formatla
    formatPermissions(data) {
        const formatted = {};

        // Her kullanıcı tipi için
        for (const userType in data) {
            formatted[userType] = {};

            // Her buton izni için (data objesi olarak geliyor)
            if (data[userType] && typeof data[userType] === 'object') {
                for (const key in data[userType]) {
                    formatted[userType][key] = data[userType][key];
                }
            }
        }

        return formatted;
    }

    // Belirli bir buton için izin kontrolü
    hasPermission(buttonAction, customPage = null) {
        if (!this.initialized) {
            return true; // Henüz yüklenmediyse göster
        }

        const page = customPage || this.currentPage;
        const key = `${page}|${buttonAction}`;

        // İzin kontrolü
        if (this.permissions[this.currentUserType] &&
            this.permissions[this.currentUserType][key] !== undefined) {
            const result = this.permissions[this.currentUserType][key];
            return result;
        }

        // Varsayılan olarak false (yetki yok)
        return false;
    }

    // Sayfadaki tüm butonları kontrol et
    applyPermissions() {
        if (!this.initialized) {
            return;
        }

        // 1. data-permission attribute'u ile tanımlanan butonlar
        document.querySelectorAll('[data-permission]').forEach(element => {
            this.processPermissionElement(element);
        });

        // 2. data-button-permission attribute'u ile tanımlanan butonlar
        document.querySelectorAll('[data-button-permission]').forEach(element => {
            this.processPermissionElement(element);
        });

        // 3. İCON BAZLI KONTROL - ÖNEMLİ!
        this.processIconBasedButtons();
    }

    // İcon bazlı butonları işle (fa-edit, fa-trash, fa-plus, fa-eye)
    processIconBasedButtons() {

        // CREATE - Ekle butonları (fa-plus, fa-hammer, fa-add)
        document.querySelectorAll('.fa-plus, .fa-hammer, .fa-add').forEach(icon => {
            const button = icon.closest('button, a, .btn');
            if (button) {
                const hasPermission = this.hasPermission('create');
                if (!hasPermission) {
                    this.hideElement(button);
                }
            }
        });

        // EDIT - Düzenle butonları (fa-edit, fa-pencil, fa-wrench)
        document.querySelectorAll('.fa-edit, .fa-pencil, .fa-wrench').forEach(icon => {
            const button = icon.closest('button, a, .btn');
            if (button) {
                const hasPermission = this.hasPermission('edit');
                if (!hasPermission) {
                    this.hideElement(button);
                }
            }
        });

        // DELETE - Sil butonları (fa-trash, fa-times, fa-eraser)
        document.querySelectorAll('.fa-trash, .fa-times, .fa-eraser').forEach(icon => {
            const button = icon.closest('button, a, .btn');
            if (button) {
                const hasPermission = this.hasPermission('delete');
                if (!hasPermission) {
                    this.hideElement(button);
                }
            }
        });

        // VIEW - Detay butonları (fa-eye, fa-search, fa-binoculars)
        document.querySelectorAll('.fa-eye, .fa-search, .fa-binoculars').forEach(icon => {
            const button = icon.closest('button, a, .btn');
            if (button) {
                const hasPermission = this.hasPermission('view');
                if (!hasPermission) {
                    this.hideElement(button);
                }
            }
        });

        // Ayrıca buton class'larına göre de kontrol et
        document.querySelectorAll('.btn-warning, .btn-edit').forEach(button => {
            if (!button.closest('.fa-edit')) { // Daha önce işlenmediyse
                const hasPermission = this.hasPermission('edit');
                if (!hasPermission) {
                    this.hideElement(button);
                }
            }
        });

        document.querySelectorAll('.btn-danger, .btn-delete').forEach(button => {
            if (!button.closest('.fa-trash')) { // Daha önce işlenmediyse
                const hasPermission = this.hasPermission('delete');
                if (!hasPermission) {
                    this.hideElement(button);
                }
            }
        });

        document.querySelectorAll('.btn-primary, .btn-success').forEach(button => {
            if (button.textContent.includes('Ekle') || button.textContent.includes('Yeni') ||
                button.innerHTML.includes('plus') || button.innerHTML.includes('ekle')) {
                if (!button.closest('.fa-plus')) { // Daha önce işlenmediyse
                    const hasPermission = this.hasPermission('create');
                    if (!hasPermission) {
                        this.hideElement(button);
                    }
                }
            }
        });

        document.querySelectorAll('.btn-info, .btn-view, .btn-detail').forEach(button => {
            if (button.textContent.includes('Detay') || button.textContent.includes('Görüntüle') ||
                button.innerHTML.includes('eye') || button.innerHTML.includes('detay')) {
                if (!button.closest('.fa-eye')) { // Daha önce işlenmediyse
                    const hasPermission = this.hasPermission('view');
                    if (!hasPermission) {
                        this.hideElement(button);
                    }
                }
            }
        });
    }

    // Tek bir permission elementini işle
    processPermissionElement(element) {
        const permissionAttr = element.getAttribute('data-permission') ||
            element.getAttribute('data-button-permission');

        if (!permissionAttr) return;

        // Format: "Controller|Action" veya sadece "Action"
        const parts = permissionAttr.split('|');
        let action, page;

        if (parts.length === 2) {
            page = parts[0];
            action = parts[1];
        } else {
            action = permissionAttr;
            page = this.currentPage;
        }

        const hasPermission = this.hasPermission(action, page);


        if (!hasPermission) {
            this.hideElement(element);
        }
    }

    // Elementi gizle
    hideElement(element) {
        element.style.display = 'none';
    }

    // Elementi devre dışı bırak (isteğe bağlı)
    disableElement(element) {
        element.disabled = true;
        element.style.opacity = '0.5';
        element.style.cursor = 'not-allowed';
    }

    // Sayfa yüklendiğinde başlat
    initialize() {
        this.loadPermissions();
        this.observeDynamicContent();
    }

    // Dinamik içerik (AJAX, modal vb.) için observer
    observeDynamicContent() {
        const observer = new MutationObserver((mutations) => {
            let shouldRefresh = false;

            mutations.forEach((mutation) => {
                if (mutation.addedNodes.length > 0) {
                    shouldRefresh = true;
                }
            });

            if (shouldRefresh && this.initialized) {
                setTimeout(() => this.applyPermissions(), 100);
            }
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    // Manuel olarak yenile
    refresh() {
        this.currentPage = this.getCurrentPageName();
        this.currentUserType = this.getCurrentUserType();
   
        this.applyPermissions();
    }
}

// Global instance oluştur
window.buttonPermissionManager = new ButtonPermissionManager();

// Sayfa yüklendiğinde başlat
document.addEventListener('DOMContentLoaded', function () {
    window.buttonPermissionManager.initialize();
});

// Sayfa değişikliklerini dinle (SPA için)
window.addEventListener('popstate', function () {
    setTimeout(() => {
        window.buttonPermissionManager.refresh();
    }, 300);
});

// Tab değişimlerini dinle (Bootstrap modallar, tablar)
document.addEventListener('shown.bs.tab', function () {
    setTimeout(() => {
        window.buttonPermissionManager.refresh();
    }, 300);
});

document.addEventListener('shown.bs.modal', function () {
    setTimeout(() => {
        window.buttonPermissionManager.refresh();
    }, 300);
});

// AJAX çağrılarından sonra (jQuery varsa)
if (typeof $ !== 'undefined') {
    $(document).ajaxComplete(function () {
        setTimeout(() => {
            window.buttonPermissionManager.refresh();
        }, 300);
    });
}